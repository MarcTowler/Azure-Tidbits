namespace KeyVaultSync;

using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;

internal sealed class AzureKeyVaultMetadataProvider: IKeyVaultMetadataProvider
{
    private readonly ArmClient _armClient;

    public AzureKeyVaultMetadataProvider(TokenCredential credential)
    {
        _armClient = new ArmClient(credential);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetTagsAsync(string vaultResourceId, CancellationToken cancellationToken = default)
    {
        var vaultResource = await _armClient.GetKeyVaultResource(new ResourceIdentifier(vaultResourceId)).GetAsync(cancellationToken);

        return (IReadOnlyDictionary<string, string>)vaultResource.Value.Data.Tags;
    }
}