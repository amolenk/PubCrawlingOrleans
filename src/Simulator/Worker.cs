public class Worker : BackgroundService
{
    private const string DefaultEventId = "1";
    private const string DefaultMaxCrawlerCount = "10";

    private readonly IGrainFactory _grainFactory;
    private readonly int _eventId;
    private readonly int _maxCrawlerCount;
    private readonly ILogger _logger;

    public Worker(IGrainFactory grainFactory, IConfiguration configuration, ILogger<Worker> logger)
    {
        _grainFactory = grainFactory;
        _eventId = int.Parse(configuration["eventId"] ?? "1");
        _maxCrawlerCount = int.Parse(configuration["maxCrawlerCount"] ?? DefaultMaxCrawlerCount);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var eventSimulatorGrain = _grainFactory.GetGrain<IEventSimulatorGrain>(_eventId);

        await eventSimulatorGrain.StartAsync(_maxCrawlerCount);

        await Task.Delay(-1, stoppingToken);
    }
}
