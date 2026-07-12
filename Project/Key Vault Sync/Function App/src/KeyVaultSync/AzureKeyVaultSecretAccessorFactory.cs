namespace KeyVaultSync;

using System.Globalization;
using System.Text;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;

internal sealed class AzureKeyVaultSecretAccessorFactory : IKeyVaultSecretAccessorFactory
{
    private static readonly CompositeFormat _uriFormat = CompositeFormat.Parse(KeyVaultSyncConstants.KeyVaultUri.UriFormat);

    private readonly TokenCredential _credential;

    public AzureKeyVaultSecretAccessorFactory(TokenCredential credential)
    {
        _credential = credential;
    }

    public IKeyVaultSecretAccessor Create(string vaultName)
    {
        var client = new SecretClient(new Uri(string.Format(CultureInfo.InvariantCulture, _uriFormat, vaultName)), _credential);
        
        return new AzureKeyVaultSecretAccessor(client);
    }
}