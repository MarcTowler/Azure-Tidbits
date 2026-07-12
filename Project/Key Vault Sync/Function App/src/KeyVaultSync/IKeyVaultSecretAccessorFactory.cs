namespace KeyVaultSync;

internal interface IKeyVaultSecretAccessorFactory
{
    IKeyVaultSecretAccessor Create(string vaultName);
}