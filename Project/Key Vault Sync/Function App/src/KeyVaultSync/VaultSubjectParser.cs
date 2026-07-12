namespace KeyVaultSync;

internal static class VaultSubjectParser
{
    internal static string ParseVaultResourceId(string subject)
    {
        const string secretSuffix = "/secrets/";
        var secretsIndex = subject.IndexOf(secretSuffix, StringComparison.OrdinalIgnoreCase);

        return secretsIndex >= 0
            ? subject[..secretsIndex]
            : subject;
    }

    internal static string ParseVaultName(string subject)
    {
        const string vaultsSegment = "/providers/Microsoft.KeyVault/vaults/";
        var segmentIndex = subject.IndexOf(vaultsSegment, StringComparison.OrdinalIgnoreCase);

        if(segmentIndex < 0)
        {
            throw new ArgumentException($"Subject '{subject}' does not contain the expected segment '{vaultsSegment}'.");
        }

        var nameStart = segmentIndex + vaultsSegment.Length;
        var nameEnd = subject.IndexOf('/', nameStart);

        return nameEnd >= 0
            ? subject[nameStart..nameEnd]
            : subject[nameStart..];
    }
}