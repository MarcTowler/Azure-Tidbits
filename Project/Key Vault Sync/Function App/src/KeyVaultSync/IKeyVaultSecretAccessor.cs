namespace KeyVaultSync;

internal interface IKeyVaultSecretAccessor
{
    Task<string> GetSecretValueAsync(string name, string version, CancellationToken cancellationToken = default);
    Task SetSecretAsync(string name, string value, CancellationToken cancellationToken = default);
    Task RecoverDeletedSecretAsync(string name, CancellationToken cancellationToken = default);
}