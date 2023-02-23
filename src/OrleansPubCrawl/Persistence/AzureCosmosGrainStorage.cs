using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Cosmos;
using Orleans.Runtime;
using Orleans.Storage;

public class AzureCosmosGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly string _name;
    private readonly AzureCosmosOptions _options;
    private readonly IGrainStorageSerializer _serializer;
    private readonly ILogger _logger;

    private Container _container = null!;

    public AzureCosmosGrainStorage(
        string name,
        AzureCosmosOptions options,
        IGrainStorageSerializer serializer,
        ILogger<AzureCosmosGrainStorage> logger)
    {
        _name = name;
        _options = options;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task ReadStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
    {
        try
        {
            var documentId = GetDocumentId(grainId);
            var partitionKey = new PartitionKey(documentId);
            string? loadedState = null;

            try
            {
                var response = await _container.ReadItemAsync<GrainDocument>(
                    documentId,
                    partitionKey)
                    .ConfigureAwait(false);

                grainState.ETag = response.ETag;
                loadedState = response.Resource.State;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
            }  

            if (loadedState is null)
            {
                grainState.RecordExists = false;
                return;
            }

            grainState.RecordExists = true;
            grainState.State = ConvertFromStorageFormat<T>(loadedState);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error reading: GrainType={GrainType} Grainid={GrainId} ETag={ETag} from Azure Cosmos DB",
                grainType,
                grainId,
                grainState.ETag);

            throw;
        }
    }

    public async Task WriteStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
    {
        Console.WriteLine("WRITING STATE: " + grainId.ToString() + " (" + grainType + ")");

        try
        {
            var documentId = GetDocumentId(grainId);
            var partitionKey = new PartitionKey(documentId);
            var state = ConvertToStorageFormat(grainState.State);

            // If the grain has en ETag, then we are doing a conditional write
            ItemRequestOptions? itemRequestOptions = !string.IsNullOrEmpty(grainState.ETag)
                ? new ItemRequestOptions { IfNoneMatchEtag = grainState.ETag } : null;

            var grainDocument = new GrainDocument
            {
                Id = documentId,
                GrainId = grainId.ToString(),
                State = state
            };

            var response = await _container.UpsertItemAsync(grainDocument, partitionKey, itemRequestOptions)
                .ConfigureAwait(false);

            grainState.ETag = response.ETag;
            grainState.RecordExists = true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            throw new InconsistentStateException("Unknown", grainState.ETag, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error writing: GrainType={GrainType} GrainId={GrainId} ETag={ETag} to Azure Cosmos DB",
                grainType,
                grainId,
                grainState.ETag);

            throw;
        }
    }

    public async Task ClearStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
    {
        try
        {
            var documentId = GetDocumentId(grainId);
            var partitionKey = new PartitionKey(documentId);

            // If the grain has en ETag, then we are doing a conditional delete
            ItemRequestOptions? itemRequestOptions = !string.IsNullOrEmpty(grainState.ETag)
                ? new ItemRequestOptions { IfNoneMatchEtag = grainState.ETag } : null;

            var response = await _container.DeleteItemAsync<GrainDocument>(
                documentId,
                partitionKey,
                itemRequestOptions)
                .ConfigureAwait(false);

            grainState.ETag = null;
            grainState.RecordExists = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error clearing: GrainType={GrainType} GrainId={GrainId} ETag={ETag} in Azure Cosmos DB",
                grainType,
                grainId,
                grainState.ETag);

            throw;
        }
    }
        
    public void Participate(ISiloLifecycle observer)
    {
        observer.Subscribe(OptionFormattingUtilities.Name<AzureCosmosGrainStorage>(_name), _options.InitStage, Init);
    }

    private async Task Init(CancellationToken ct)
    {
        var stopWatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("AzureCosmosGrainStorage initializing: {Options}", _options.ToString());

            if (_options.CreateClient is not { } createClient)
            {
                throw new OrleansConfigurationException($"No credentials specified. Use the {_options.GetType().Name}.{nameof(AzureCosmosOptions.ConfigureCosmosClient)} method to configure the Azure Cosmos client.");
            }

            var client = await createClient();

            _container = client.GetContainer(_options.DatabaseName, _options.ContainerName);
            
            stopWatch.Stop();

            _logger.LogInformation(
                "Initializing provider {ProviderName} of type {ProviderType} in stage {Stage} took {ElapsedMilliseconds} Milliseconds.",
                _name,
                this.GetType().Name,
                _options.InitStage,
                stopWatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopWatch.Stop();
            
            _logger.LogError(
                ex,
                "Initialization failed for provider {ProviderName} of type {ProviderType} in stage {Stage} in {ElapsedMilliseconds} Milliseconds.",
                _name,
                this.GetType().Name,
                _options.InitStage,
                stopWatch.ElapsedMilliseconds);
            
            throw;
        }
    }

    private string GetDocumentId(GrainId grainId) => grainId.ToString().Replace('/', '_');

    private string ConvertToStorageFormat<T>(T grainState) => Convert.ToBase64String(_serializer.Serialize(grainState));

    private T ConvertFromStorageFormat<T>(string contents) => _serializer.Deserialize<T>(Convert.FromBase64String(contents));
}
