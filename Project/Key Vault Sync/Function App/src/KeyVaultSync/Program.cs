using Azure.Identity;
using KeyVaultSync;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder
    .ConfigureFunctionsWebApplication()
    .Services
    .AddApplicationInsightsTelemetryWorkerServices()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton(_ => new DefaultAzureCredential())
    .AddSingleton<IKeyVaultMetadataProvider, AzureKeyVaultMetadataProvider>(sp =>
        new AzureKeyVaultMetadataProvider(sp.GetRequiredService<DefaultAzureCredential>()))
    .AddSingleton<IKeyVaultSecretAccessorFactory, AzureKeyVaultSecretAccessorFactory>(sp =>
        new AzureKeyVaultSecretAccessorFactory(sp.GetRequiredService<DefaultAzureCredential>()))
    .AddSingleton<SecretSyncService>();

var host = builder.Build();

await host.RunAsync();