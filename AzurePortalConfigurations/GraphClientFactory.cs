using Azure.Identity;
using Microsoft.Graph;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ConnectToOneDriveAzurePortal.AzurePortalConfigurations
{
    public class GraphClientFactory
    {
        private GraphServiceClient _graphClient;

        public async Task<GraphServiceClient> CreateAsync()
        {
            if (_graphClient != null) return _graphClient;


            var credentialOptions = new InteractiveBrowserCredentialOptions
            {
                TenantId = GraphAppSettings.TenantId,
                ClientId = GraphAppSettings.ClientId,
                RedirectUri = new Uri("http://localhost"),
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                DisableAutomaticAuthentication = false,

                TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = GraphAppSettings.TokenCacheName,
                }
            };

            InteractiveBrowserCredential credential;

            var systemDriveRoot = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "OneDriveAuth"); // Typically "C:\"

            if (!Directory.Exists(systemDriveRoot))
            {
                Directory.CreateDirectory(systemDriveRoot);
            }

            // Save the record to disk for future runs
            var filePath = Path.Combine(systemDriveRoot, GraphAppSettings.AuthRecordPath);

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                // Trigger interactive auth once and capture the AuthenticationRecord
                credential = new InteractiveBrowserCredential(credentialOptions);
                // Request tokens for Microsoft Graph explicitly
                var tokenRequest = new Azure.Core.TokenRequestContext(GraphAppSettings.Scopes);
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
            _graphClient = new GraphServiceClient(credential, GraphAppSettings.Scopes);
            return _graphClient;
        }
    }
}
