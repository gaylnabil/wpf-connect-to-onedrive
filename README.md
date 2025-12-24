
# Connect a WPF (.NET) app to OneDrive via Microsoft Graph  
## Azure Portal setup for **Microsoft personal accounts** *and* **work/school (Entra ID) accounts**

This guide walks you through **Azure Portal configuration** and **WPF code** so your app can sign in and call Microsoft Graph to access **OneDrive**. It covers both identity audiences:

- **Personal Microsoft accounts (MSA)** – e.g., Outlook.com/Hotmail/Xbox  
- **Work or school accounts** in **Microsoft Entra ID** (formerly Azure AD)

> **Why two paths?** Microsoft Graph uses the **Microsoft identity platform**. Your app must be registered to support the audience you want (MSA vs. Entra ID), and your code must use the matching **authority** (e.g., `/consumers` for MSA, a tenant or `/organizations` for work/school). [1][2]

---

## Prerequisites

- **Visual Studio** and **.NET 6/7/8** for WPF.  
- NuGet packages:  
  - `Microsoft.Graph` (SDK v5) — Graph client. [3][4]  
  - `Azure.Identity` — authentication with `InteractiveBrowserCredential`. [5][6]  
- A **Microsoft account** (personal) and/or a **work/school account** in Microsoft Entra.

---

## 1) Decide your supported account types

You can support **one** or **both** audiences. Pick at app registration time:

- **MSA only** → *Personal Microsoft accounts only*; authority = `…/consumers`. [1][2]  
- **Work/school only** → *Accounts in this organizational directory* (single tenant) or *Accounts in any organizational directory* (multi-tenant); authority = your **tenant ID/domain** or `…/organizations`. [1][2]  
- **Both** → *Accounts in any organizational directory and personal Microsoft accounts*; authority = `…/common`. [1][2]

> The **authority** you use in code **must match** what you choose in the registration; otherwise you will get errors like *“use the /consumers endpoint”*. [2]

---

## 2) Register your application in Azure Portal

1. Go to **Azure Portal** → **Microsoft Entra ID** → **App registrations** → **New registration**.  
   - **Name**: `YourWpfOneDriveApp`  
   - **Supported account types**: choose from section 1. [1][2]  
   - (Optional) Add your **Publisher domain** later.

2. After creation, note:
   - **Application (client) ID** (GUID).  
   - For work/school scenarios: **Directory (tenant) ID** (GUID). [1]

3. **Authentication** settings:
   - Click **Authentication**.  
   - Under **Platform configurations**, add **Mobile & desktop** (Public client/native).  
   - **Redirect URI**: `http://localhost` *(desktop default)* or `https://login.microsoftonline.com/common/oauth2/nativeclient`. [1][5]  
   - **Advanced settings** → **Allow public client flows** = **Yes** (needed for desktop interactive sign-in). [5]

4. **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**:  
   - `User.Read` (basic sign-in).  
   - `Files.ReadWrite` (OneDrive access).  
   - For org tenants, click **Grant admin consent** after adding permissions. Personal accounts don’t use admin consent. [1][7]

> These settings are the standard MSAL/desktop app configuration for interactive sign-in and delegated Graph permissions. [1][5][7]

---

## 3) (Optional but recommended) Persistent token cache for silent sign-in

To avoid asking the user to sign in on every run, enable **persistent token caching** and store an **AuthenticationRecord** after first login:

- Use `TokenCachePersistenceOptions` (encrypted cache via OS: DPAPI/Keychain/LibSecret).  
- Serialize the `AuthenticationRecord` on first sign-in and reload it next startup. [8][9][10]

---

## 4) WPF C# code — Sign in and call OneDrive

### A) Personal Microsoft accounts **(MSA)**

> Registration must be **MSA only** or **both**. Use authority **`/consumers`** for MSA-only apps. [2]

```csharp
// NuGet: Microsoft.Graph (v5), Azure.Identity
using Azure.Identity;
using Microsoft.Graph;
using Azure.Core;
using System.IO;

// ---- FIRST RUN: login & save AuthenticationRecord ----
var credOptions = new InteractiveBrowserCredentialOptions {
    TenantId = "consumers",                 // MSA authority
    ClientId = "YOUR_CLIENT_ID",
    RedirectUri = new Uri("http://localhost"),
    TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = "com.yourapp.msalcache" }
};
var credential = new InteractiveBrowserCredential(credOptions);

// interactive login once
AuthenticationRecord record = await credential.AuthenticateAsync();

// save record to disk
using (var fs = File.Create("authRecord.bin"))
    await record.SerializeAsync(fs);

// ---- LATER RUNS: silent reuse of the same account ----
using (var fs = File.OpenRead("authRecord.bin"))
    record = await AuthenticationRecord.DeserializeAsync(fs);
credOptions.AuthenticationRecord = record;
credential = new InteractiveBrowserCredential(credOptions);

// Graph v5 client with delegated scopes
var graph = new GraphServiceClient(credential, new[] { "User.Read", "Files.ReadWrite" });

// OneDrive calls
var drive = await graph.Me.Drive.GetAsync();
var root  = await graph.Me.Drive.Root.GetAsync();
var items = await graph.Me.Drive.Root.Children.GetAsync();
```

### B) Work/school (Entra ID) — single or multi‑tenant

Registration must be single‑tenant or multi‑tenant for organizations. Use authority your tenant GUID/domain or /organizations. [1]
```csharp
// NuGet: Microsoft.Graph (v5), Azure.Identity
var credOptions = new InteractiveBrowserCredentialOptions {
    TenantId = "YOUR_TENANT_ID",           // or "organizations" for multi-tenant
    ClientId = "YOUR_CLIENT_ID",
    RedirectUri = new Uri("http://localhost"),
    TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = "com.yourapp.msalcache" }
};
var credential = new InteractiveBrowserCredential(credOptions);

// First run: login & save AuthenticationRecord
AuthenticationRecord record = await credential.AuthenticateAsync();
using (var fs = File.Create("authRecord.bin"))
    await record.SerializeAsync(fs);

// Subsequent runs: load record
using (var fs = File.OpenRead("authRecord.bin"))
    record = await AuthenticationRecord.DeserializeAsync(fs);
credOptions.AuthenticationRecord = record;
credential = new InteractiveBrowserCredential(credOptions);

// Graph client with delegated scopes
var graph = new GraphServiceClient(credential, new[] { "User.Read", "Files.ReadWrite" });

// OneDrive calls
var drive = await graph.Me.Drive.GetAsync();
var root  = await graph.Me.Drive.Root.GetAsync();

```


## 5) Scopes / resources: what to pass

- For Graph delegated calls (OneDrive, profile), pass Graph scopes like User.Read, Files.ReadWrite. Do not mix them with .default. [1][7][11]
- For Azure Resource Manager (if you ever need it), request https://management.azure.com/.default (note: single / before .default), not openid profile offline_access in the same array. [11]

## 6) Troubleshooting


- “Use the /consumers endpoint” error (AADSTS9002331/9002346)
Your app is registered for personal accounts, but the code used an org authority (or vice versa). Fix by aligning Supported account types in the registration with the TenantId/authority you pass in code (consumers, organizations, common, or tenant ID). [2]


- Invalid scope (e.g., https://management.azure.com//.default openid profile)
There’s an extra / and mixed scopes. Use a single resource .default (ARM) or delegated Graph scopes, not both in one request. [11]


- “Tenant does not have a SPO license” when calling /me/drive
For work/school accounts, OneDrive for Business relies on SharePoint Online. Make sure the user has an SPO/OneDrive license assigned and the OneDrive site is provisioned (often after first visit). [12][13]


- Always getting prompted to sign in:
Ensure you serialize AuthenticationRecord and specify the same cache name via TokenCachePersistenceOptions. Keep the same authority and ClientId across runs. [8][9][10]

## 7) References


1) MSAL client application configuration (authorities, redirect URIs, audiences)
https://learn.microsoft.com/en-us/entra/identity-platform/msal-client-application-configuration


2) AADSTS “use /consumers endpoint” guidance (authority must match audience)
https://stackoverflow.com/questions/73102294/aadsts9002331-application-is-configured-for-use-by-microsoft-account-users-only
https://learn.microsoft.com/en-us/answers/questions/1160493/access-issue


3) Graph SDK v5 & authentication (TokenCredential & upgrade guide)
https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/main/docs/upgrade-to-v5.md


4) Graph .NET SDK repo
https://github.com/microsoftgraph/msgraph-sdk-dotnet


5) InteractiveBrowserCredential (desktop interactive sign‑in)
https://learn.microsoft.com/en-us/dotnet/api/azure.identity.interactivebrowsercredential
https://azuresdkdocs.z19.web.core.windows.net/dotnet/Azure.Identity/1.13.2/api/Azure.Identity/Azure.Identity.InteractiveBrowserCredential.html


6) MSAL interactive acquisition basics
https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/acquiring-tokens-interactively


7) Graph delegated permissions reference / adding permissions
https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers
(also see per‑API “Permissions” sections throughout Graph docs)


8) TokenCachePersistenceOptions + AuthenticationRecord sample
https://learn.microsoft.com/en-us/dotnet/api/azure.identity.tokencachepersistenceoptions


9) Azure.Identity token cache sample
https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/samples/TokenCache.md


10) Client-side authentication sample (Azure.Identity)
https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/samples/ClientSideUserAuthentication.md


11) Interactive desktop auth guidance & scope rules
https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-acquire-token-interactive


12) SPO license requirement / OneDrive for Business
https://learn.microsoft.com/en-us/answers/questions/1526573/tenant-does-not-have-a-spo-license
https://stackoverflow.com/questions/46802055/tenant-does-not-have-a-spo-license


13) OneDrive developer resources & samples
https://learn.microsoft.com/en-us/onedrive/developer/sample-code