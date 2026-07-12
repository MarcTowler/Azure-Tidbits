$destTag = "replica_destination"

function Get-SecretHash($secureString)
{
    if($null -eq $secureString)
    {
        return $null
    }

    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureString)

    try
    {
        $plainText = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        $hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes($plainText))

        return [System.BitConverter]::ToString($hash) -replace "-", ""
    }
    finally
    {
        if ($bstr -ne [IntPtr]::Zero)
        {
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
}

$sourceVaults = Get-AzResource -ResourceType "Microsoft.KeyVault/vaults" -TagName $destTag -ErrorAction SilentlyContinue

if($sourceVaults -eq $null -or $sourceVaults.Count -eq 0)
{
    Write-Host "No source vaults found with tag '$destTag'."
    exit 0
}

foreach($sourceVault in $sourceVaults)
{
    $sourceVaultName = $sourceVault.Name
    $destinationVaultName = $sourceVault.Tags[$destTag]

    Write-Host "Syncing secrets from source vault '$sourceVaultName' to destination vault '$destinationVaultName'."

    try
    {
        $sourceSecretsMeta = Get-AzKeyVaultSecret -VaultName $sourceVaultName -ErrorAction Stop
    }
    catch
    {
        Write-Host "Error retrieving secrets from source vault '$sourceVaultName': $_"
        continue
    }

    foreach($secretMeta in $sourceSecretsMeta)
    {
        $secretName = $secretMeta.Name
        $needsSync = $false

        $sourceSecret = Get-AzKeyVaultSecret -VaultName $sourceVaultName -Name $secretName -ErrorAction SilentlyContinue
        $sourceHash = Get-SecretHash -SecureString $sourceSecret.SecretValue

        try
        {
            $destinationSecret = Get-AzKeyVaultSecret -VaultName $destinationVaultName -Name $secretName -ErrorAction Stop
            $destinationHash = Get-SecretHash -SecureString $destinationSecret.SecretValue

            if($sourceHash -ne $destinationHash)
            {
                Write-Host "Secret '$secretName' in destination vault '$destinationVaultName' is outdated. Syncing..."
                $needsSync = $true
            }
        }
        catch
        {
            Write-Host "Secret '$secretName' does not exist in destination vault '$destinationVaultName'. Syncing..."
            $needsSync = $true
        }

        if($needsSync)
        {
            try
            {
                Set-AzKeyVaultSecret -VaultName $destinationVaultName -Name $secretName -SecretValue $sourceSecret.SecretValue | Out-Null
                Write-Host "Secret '$secretName' synced successfully to destination vault '$destinationVaultName'."
            }
            catch
            {
                Write-Host "Error syncing secret '$secretName' to destination vault '$destinationVaultName': $_"
            }
        } 
        else
        {
            Write-Host "Secret '$secretName' in destination vault '$destinationVaultName' is up-to-date. No action needed."
        }
    }
}