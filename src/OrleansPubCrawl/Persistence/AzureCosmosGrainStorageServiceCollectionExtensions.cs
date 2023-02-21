using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

/// <summary>
/// <see cref="IServiceCollection"/> extensions.
/// </summary>
public static class AzureCosmosGrainStorageServiceCollectionExtensions
{
    /// <summary>
    /// Configure silo to use Azure Cosmos DB as the default grain storage.
    /// </summary>
    public static IServiceCollection AddAzureCosmosGrainStorageAsDefault(this IServiceCollection services, Action<AzureCosmosOptions> configureOptions)
    {
        return services.AddAzureCosmosGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    /// Configure silo to use Azure Cosmos DB for grain storage.
    /// </summary>
    public static IServiceCollection AddAzureCosmosGrainStorage(this IServiceCollection services, string name, Action<AzureCosmosOptions> configureOptions)
    {
        return services.AddAzureCosmosGrainStorage(name, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    /// Configure silo to use Azure Cosmos DB as the default grain storage.
    /// </summary>
    public static IServiceCollection AddAzureCosmosGrainStorageAsDefault(this IServiceCollection services, Action<OptionsBuilder<AzureCosmosOptions>> configureOptions = null!)
    {
        return services.AddAzureCosmosGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    /// Configure silo to use Azure Cosmos DB for grain storage.
    /// </summary>
    public static IServiceCollection AddAzureCosmosGrainStorage(this IServiceCollection services, string name,
        Action<OptionsBuilder<AzureCosmosOptions>> configureOptions = null!)
    {
        configureOptions?.Invoke(services.AddOptions<AzureCosmosOptions>(name));
        services.AddTransient<IConfigurationValidator>(sp => new AzureCosmosOptionsValidator(sp.GetRequiredService<IOptionsMonitor<AzureCosmosOptions>>().Get(name), name));
        services.AddTransient<IPostConfigureOptions<AzureCosmosOptions>, DefaultStorageProviderSerializerOptionsConfigurator<AzureCosmosOptions>>();
        services.ConfigureNamedOptionForLogging<AzureCosmosOptions>(name);
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton(sp => sp.GetServiceByName<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        }
        return services.AddSingletonNamedService<IGrainStorage>(name, AzureCosmosGrainStorageFactory.Create)
                        .AddSingletonNamedService<ILifecycleParticipant<ISiloLifecycle>>(name, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
    }
}