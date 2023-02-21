using Microsoft.Extensions.Options;
using Orleans.Storage;

public static class AzureCosmosGrainStorageFactory
{
    public static IGrainStorage Create(IServiceProvider services, string name)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<AzureCosmosOptions>>();
        return ActivatorUtilities.CreateInstance<AzureCosmosGrainStorage>(services, name, optionsMonitor.Get(name));
    }
}