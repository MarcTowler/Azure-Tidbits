namespace KeyVaultSync;

using System.Net;
using Azure;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Logging;

internal sealed partial class SecretSyncService
{
    private readonly IKeyVaultMetadataProvider _metadataProvider;
    private readonly IKeyVaultSecretAccessorFactory _accessorFactory;
    private readonly ILogger<SecretSyncService> _logger;

    public SecretSyncService(
        IKeyVaultMetadataProvider metadataProvider,
        IKeyVaultSecretAccessorFactory accessorFactory,
        ILogger<SecretSyncService> logger)
    {
        _metadataProvider = metadataProvider;
        _accessorFactory = accessorFactory;
        _logger = logger;
    }

    public async Task SyncAsync(EventGridEvent eventGridEvent)
    {
        ArgumentNullException.ThrowIfNull(eventGridEvent);

        LogReceivedEvent(eventGridEvent.EventType, eventGridEvent.Subject);

        if(eventGridEvent.EventType != EventTypes.SecretNewVersionCreated)
        {
            return;
        }

        try
        {
            var ctx = ParseEventContext(eventGridEvent);

            LogProcessingSecret(ctx.SecretName, ctx.SecretVersion);

            var destinationVaultName = await GetDestinationVaultNameAsync(ctx.VaultResourceId, ctx.SourceVaultName);

            if(destinationVaultName is null)
            {
                return;
            }

            LogProcessingSecretWithDestination(ctx.SecretName, ctx.SourceVaultName, destinationVaultName);

            await TransferSecretAsync(ctx.SecretName, ctx.SecretVersion, ctx.SourceVaultName, destinationVaultName, eventGridEvent.Id);
        }
        catch(Exception ex)
        {
            LogSyncError(ex, eventGridEvent.Id);

            throw;
        }
    }

    private static EventContext ParseEventContext(EventGridEvent eventGridEvent)
    {
        var eventData = eventGridEvent.Data.ToObjectFromJson<KeyVaultSecretNewVersionCreatedEventData>();
        var secretName = eventData?.ObjectName ?? throw new InvalidOperationException("Secret name is missing in the event data.");
        var secretVersion = eventData?.Version ?? throw new InvalidOperationException("Secret version is null");

        var subject = eventGridEvent.Subject ?? throw new InvalidOperationException("Event subject is null");
        var sourceVaultName = VaultSubjectParser.ParseVaultName(subject);
        var vaultResourceId = VaultSubjectParser.ParseVaultResourceId(subject);

        return new EventContext(secretName, secretVersion, sourceVaultName, vaultResourceId);
    }

    private async Task<string?> GetDestinationVaultNameAsync(string vaultResourceId, string sourceVaultName)
    {
        var tags = await _metadataProvider.GetTagsAsync(vaultResourceId);

        if(!tags.TryGetValue(KeyVaultSyncConstants.TagKeys.ReplicaDestination, out var destinationVaultName) || string.IsNullOrWhiteSpace(destinationVaultName))
        {
            LogNoReplicaDestinationTag(sourceVaultName);

            return null;
        }

        return destinationVaultName;
    }

    private async Task TransferSecretAsync(
        string secretName,
        string secretVersion,
        string sourceVaultName,
        string destinationVaultName,
        string eventId)
    {
        var sourceAccessor = _accessorFactory.Create(sourceVaultName);
        var destinationAccessor = _accessorFactory.Create(destinationVaultName);

        string secretValue;

        try
        {
            secretValue = await sourceAccessor.GetSecretValueAsync(secretName, secretVersion);
        }
        catch(RequestFailedException ex) when(ex.Status == (int)HttpStatusCode.NotFound)
        {
            LogSourceSecretNotFound( secretName, secretVersion, sourceVaultName, eventId);

            return;
        }

        try
        {
            await destinationAccessor.SetSecretAsync(secretName, secretValue);
        }
        catch(RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
        {
            LogRecoveringDeletedSecret(secretName, destinationVaultName);

            await destinationAccessor.RecoverDeletedSecretAsync(secretName);

            LogRecoveredDeletedSecret(secretName, destinationVaultName);

            await destinationAccessor.SetSecretAsync(secretName, secretValue);
        }
        
        LogSyncedSuccessfully(secretName, secretVersion, sourceVaultName, destinationVaultName);
    }

    private sealed record EventContext(
        string SecretName,
        string SecretVersion,
        string SourceVaultName,
        string VaultResourceId);
    
    [LoggerMessage(Level = LogLevel.Information, Message = "Received event of type {EventType} for subject {Subject}.")]
    private partial void LogReceivedEvent(string eventType, string subject);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing secret '{SecretName}' with version '{SecretVersion}'.")]
    private partial void LogProcessingSecret(string secretName, string secretVersion);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing secret '{SecretName}' from source vault '{SourceVaultName}' to destination vault '{DestinationVaultName}'.")]
    private partial void LogProcessingSecretWithDestination(string secretName, string sourceVaultName, string destinationVaultName);

    [LoggerMessage(Level = LogLevel.Information, Message = "No replica destination tag found for source vault '{SourceVaultName}'. Skipping secret sync.")]
    private partial void LogNoReplicaDestinationTag(string sourceVaultName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Secret '{SecretName}' with version '{SecretVersion}' not found in source vault '{SourceVaultName}'. Event ID: {EventId}. Skipping sync.")]
    private partial void LogSourceSecretNotFound(string secretName, string secretVersion, string sourceVaultName, string eventId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recovering deleted secret '{SecretName}' in destination vault '{DestinationVaultName}'.")]
    private partial void LogRecoveringDeletedSecret(string secretName, string destinationVaultName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recovered deleted secret '{SecretName}' in destination vault '{DestinationVaultName}'.")]
    private partial void LogRecoveredDeletedSecret(string secretName, string destinationVaultName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Secret '{SecretName}' with version '{SecretVersion}' successfully synced from source vault '{SourceVaultName}' to destination vault '{DestinationVaultName}'.")]
    private partial void LogSyncedSuccessfully(string secretName, string secretVersion, string sourceVaultName, string destinationVaultName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while processing event with ID '{EventId}': {ExceptionMessage}")]
    private partial void LogSyncError(Exception exception, string eventId);
}