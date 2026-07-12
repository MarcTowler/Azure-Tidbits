namespace KeyVaultSync;

internal interface IKeyVaultMetadataProvider
{
    Task<IReadOnlyDictionary<string, string>> GetTagsAsync(string vaultResourceId, CancellationToken cancellationToken = default);
}