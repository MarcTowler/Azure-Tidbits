namespace KeyVaultSync;

internal static class KeyVaultSyncConstants
{
    internal static class EventGridEventTypes
    {
        public const string SecretNewVersionCreated = "Microsoft.KeyVault.SecretNewVersionCreated";
        public const string SecretNearExpiry = "Microsoft.KeyVault.SecretNearExpiry";
    }

    internal static class TagKeys
    {
        internal const string ReplicaDestination = "replica_destination";
    }

    internal static class KeyVaultUri
    {
        internal const string UriFormat = "https://{0}.vault.azure.net/";
    }

    internal static class FunctionNames
    {
        internal const string SyncSecretToDestination = "SyncSecretToDestination";
    }
}