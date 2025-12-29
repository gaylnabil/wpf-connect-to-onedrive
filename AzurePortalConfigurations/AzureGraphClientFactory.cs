using Azure.Identity;
using Microsoft.Graph;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ConnectToOneDriveAzurePortal.AzurePortalConfigurations
{
    public class AzureGraphClientFactory
    {
        private GraphServiceClient _graphClient;

        public async Task<GraphServiceClient> CreateAsync()
        {
            if (_graphClient != null) return _graphClient;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | ServicePointManager.SecurityProtocol;


            var credentialOptions = new InteractiveBrowserCredentialOptions
            {
                TenantId = AzureGraphAppSettings.TenantId,
                ClientId = AzureGraphAppSettings.ClientId,
                RedirectUri = new Uri("http://localhost"),
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                DisableAutomaticAuthentication = false,

                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = AzureGraphAppSettings.TokenCacheName,
                }
            };

            InteractiveBrowserCredential credential;

            // Use the full per-user path provided by AzureGraphAppSettings.AuthRecordPath
            var filePath = $@"C:\OneDriveAuth\{AzureGraphAppSettings.AuthRecordFileName}";

            // Ensure the directory exists to avoid UnauthorizedAccessException when creating the file
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                // Trigger interactive auth once and capture the AuthenticationRecord
                credential = new InteractiveBrowserCredential(credentialOptions);
                // Request tokens for Microsoft Graph explicitly
                var tokenRequest = new Azure.Core.TokenRequestContext(AzureGraphAppSettings.Scopes);
                var record = await credential.AuthenticateAsync(tokenRequest);

                // Save the record to disk for future runs
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await record.SerializeAsync(fs);
                }
            }
            else
            {
                // Load the record from disk
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    var savedRecord = await AuthenticationRecord.DeserializeAsync(fs);

                    credentialOptions.AuthenticationRecord = savedRecord;

                    credential = new InteractiveBrowserCredential(credentialOptions);
                }
            }

            // Create Graph service client
            _graphClient = new GraphServiceClient(credential, AzureGraphAppSettings.Scopes);
            return _graphClient;
        }
    }
}
