namespace KeyVaultSync;

using Azure.Security.KeyVault.Secrets;

internal sealed class AzureKeyVaultSecretAccessor : IKeyVaultSecretAccessor
{
    private readonly SecretClient _client;

    public AzureKeyVaultSecretAccessor(SecretClient client)
    {
        _client = client;
    }

    public async Task<string> GetSecretValueAsync(string secretName, string version, CancellationToken cancellationToken = default)
    {
        var secret = await _client.GetSecretAsync(secretName, version, cancellationToken);
     
        return secret.Value.Value;
    }

    public async Task SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        await _client.SetSecretAsync(name, value, cancellationToken);
    }

    public async Task RecoverDeletedSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var operation = await _client.StartRecoverDeletedSecretAsync(secretName, cancellationToken);

        await operation.WaitForCompletionAsync(cancellationToken);
    }
}