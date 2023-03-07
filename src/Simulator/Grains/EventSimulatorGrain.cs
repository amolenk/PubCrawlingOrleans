public interface IEventSimulatorGrain : IGrainWithIntegerKey
{
    Task StartAsync(int maxCrawlerCount);

    Task<Venue[]> GetVenuesAsync();

    Task<List<string>> GetBeersAsync(string venueId);
}

public class EventSimulatorGrain : Grain, IEventSimulatorGrain
{
    private readonly PubCrawlService _pubCrawlService;
    private readonly ILogger _logger;
    private Venue[] _venues = Array.Empty<Venue>();
    private Dictionary<string, List<string>> _venueBeers = new();
    private int _maxCrawlerCount;
    private int _crawlerCount = 0;
    private object _lock = new();

    public EventSimulatorGrain(PubCrawlService pubCrawlService, ILogger<EventSimulatorGrain> logger)
    {
        _pubCrawlService = pubCrawlService;
        _logger = logger;
    }

    public async Task StartAsync(int maxCrawlerCount)
    {
        _maxCrawlerCount = maxCrawlerCount;
        _venues = await _pubCrawlService.GetVenuesAsync();

        // Cache the venue beers.
        foreach (var venue in _venues)
        {
            var venueDetails = await _pubCrawlService.GetVenueAsync(venue.Id);
            _venueBeers[venue.Id] = venueDetails.Beers;
        }

        this.RegisterTimer(AddCrawler, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(20));
    }

    public Task<Venue[]> GetVenuesAsync() => Task.FromResult(_venues);

    public Task<List<string>> GetBeersAsync(string venueId) => Task.FromResult(_venueBeers[venueId]);

    private async Task AddCrawler(object state)
    {
        if (_crawlerCount < _maxCrawlerCount)
        {
            var crawlerSimulatorGrain = GrainFactory.GetGrain<ICrawlerSimulatorGrain>(_crawlerCount);
            await crawlerSimulatorGrain.StartAsync(this.AsReference<IEventSimulatorGrain>());

            _crawlerCount++;
        }
    }
}