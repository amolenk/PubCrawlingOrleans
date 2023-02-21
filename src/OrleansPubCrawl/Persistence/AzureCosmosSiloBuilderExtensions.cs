using Microsoft.Extensions.Options;
using Orleans.Providers;

public static class AzureBlobSiloBuilderExtensions
{
    /// <summary>
    /// Configure silo to use Azure Cosmos DB as the default grain storage.
    /// </summary>
    public static ISiloBuilder AddAzureCosmosGrainStorageAsDefault(this ISiloBuilder builder, Action<AzureCosmosOptions> configureOptions)
    {
        return builder.AddAzureCosmosGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    /// Configure silo to use Azure Cosmos DB for grain storage.
    /// </summary>
    public static ISiloBuilder AddAzureCosmosGrainStorage(this ISiloBuilder builder, string name, Action<AzureCosmosOptions> configureOptions)
    {
        return builder.ConfigureServices(services => services.AddAzureCosmosGrainStorage(name, configureOptions));
    }

    /// <summary>
    /// Configure silo to use Azure Cosmos DB as the default grain storage.
    /// </summary>
    public static ISiloBuilder AddAzureCosmosGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<AzureCosmosOptions>> configureOptions = null!)
    {
        return builder.AddAzureCosmosGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    /// Configure silo to use Azure Cosmos DB for grain storage.
    /// </summary>
    public static ISiloBuilder AddAzureCosmosGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<AzureCosmosOptions>> configureOptions = null!)
    {
        return builder.ConfigureServices(services => services.AddAzureCosmosGrainStorage(name, configureOptions));
    }
}