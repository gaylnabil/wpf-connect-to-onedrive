namespace ConnectToOneDriveAzurePortal.AzurePortalConfigurations
{
    internal static class GraphAppSettings
    {
        internal const string ClientId = "3c66c8d5-bdc6-464e-b7a7-bacc99c142c7";     // from app registration
        internal const string TenantId = "consumers";     // from app registration
        internal const string TokenCacheName = "CacheToken";     // from app registration";

        internal const string AuthRecordPath = "authRecord.bin";

        // Minimal delegated scopes for OneDrive and profile
        internal static readonly string[] Scopes = { "offline_access", "Files.ReadWrite", "User.Read" };
    }

}
