using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Storage;

public class AzureCosmosOptions : IStorageProviderSerializerOptions
{

    public string DatabaseName { get; private set; } = DefaultDatabaseName;
    public const string DefaultDatabaseName = "orleans";

    /// <summary>
    /// Container name where grain stage is stored
    /// </summary>
    public string ContainerName { get; private set; } = DefaultContainerName;
    public const string DefaultContainerName = "grainstate";

    /// <summary>
    /// Options to be used when configuring the cosmos client, or <see langword="null"/> to use the default options.
    /// </summary>
    public CosmosClientOptions? ClientOptions { get; set; }

    // /// <summary>
    // /// The optional delegate used to create a <see cref="BlobServiceClient"/> instance.
    // /// </summary>
    // internal Func<Task<BlobServiceClient>> CreateClient { get; private set; }

    /// <summary>
    /// Stage of silo lifecycle where storage should be initialized.  Storage must be initialized prior to use.
    /// </summary>
    public int InitStage { get; set; } = DEFAULT_INIT_STAGE;
    public const int DEFAULT_INIT_STAGE = ServiceLifecycleStage.ApplicationServices;

    /// <summary>
    /// The optional delegate used to create a <see cref="CosmosClient"/> instance.
    /// </summary>
    internal Func<Task<CosmosClient>>? CreateClient { get; private set; }

    /// <inheritdoc/>
    public IGrainStorageSerializer GrainStorageSerializer { get; set;} = null!;

    /// <summary>
    /// Configures the <see cref="BlobServiceClient"/> using a connection string.
    /// </summary>
    public void ConfigureCosmosClient(string connectionString, string databaseName, string containerName)
    {
        CreateClient = () => Task.FromResult(new CosmosClient(connectionString, ClientOptions));
        DatabaseName = databaseName;
        ContainerName = containerName;
    }
}

/// <summary>
/// Configuration validator for AzureBlobStorageOptions
/// </summary>
public class AzureCosmosOptionsValidator : IConfigurationValidator
{
    private readonly AzureCosmosOptions options;
    private readonly string name;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options">The option to be validated.</param>
    /// <param name="name">The option name to be validated.</param>
    public AzureCosmosOptionsValidator(AzureCosmosOptions options, string name)
    {
        this.options = options;
        this.name = name;
    }

    public void ValidateConfiguration()
    {
        if (this.options.CreateClient is null)
        {
            throw new OrleansConfigurationException($"No credentials specified. Use the {options.GetType().Name}.{nameof(AzureCosmosOptions.ConfigureCosmosClient)} method to configure the Azure Cosmos client.");
        }
    }
}
