namespace KeyVaultSync;

using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;

internal sealed class SecretSyncFunction
{
    private readonly SecretSyncService _service;

    public SecretSyncFunction(SecretSyncService service)
    {
        _service = service;
    }

    [Function(KeyVaultSyncConstants.FunctionNames.SyncSecretToDestination)]
    public async Task RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        await _service.SyncAsync(eventGridEvent);
    }
}