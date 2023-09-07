public interface ICrawlerSimulatorGrain : IGrainWithIntegerKey
{
    Task StartAsync(IEventSimulatorGrain eventSimulatorGrain);
}

public class CrawlerSimulatorGrain : Grain, ICrawlerSimulatorGrain
{
    private readonly PubCrawlService _pubCrawlService;
    private readonly ILogger _logger;
    private IEventSimulatorGrain _eventSimulatorGrain = null!;
    private Random _random = null!;
    private List<Venue> _venues = new();
    private int _venueIndex = -1;

    public CrawlerSimulatorGrain(PubCrawlService pubCrawlService, ILogger<CrawlerSimulatorGrain> logger)
    {
        _pubCrawlService = pubCrawlService;
        _logger = logger;
    }

    public async Task StartAsync(IEventSimulatorGrain eventSimulatorGrain)
    {
        _eventSimulatorGrain = eventSimulatorGrain;
        _random = new Random((int)this.GetPrimaryKeyLong());
        _venues = (await eventSimulatorGrain.GetVenuesAsync()).ToList();

        Shuffle(_venues);

        ScheduleNextCheckin();
    }

    private async Task CheckInAsync(object state)
    {
        _venueIndex = (_venueIndex + 1) % _venues.Count;
        var venue = _venues[_venueIndex];

        try
        {
            await _pubCrawlService.CheckInAsync(venue.Id, this.GetPrimaryKeyString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to communicate with API: " + ex.Message, ex);
        }

        // Decide on which beers to drink here.
        var availableBeers = await _eventSimulatorGrain.GetBeersAsync(venue.Id);
        var beersToDrink = new Stack<string>(availableBeers.Take(_random.Next(1, availableBeers.Count + 1)));

        ScheduleNextBeer(beersToDrink);
    }

    private async Task DrinkBeerAsync(object state)
    {
        var beersToDrink = (Stack<string>)state;

        var beer = beersToDrink.Pop();

        var like = _random.Next(-1, 5) > 0;

        try
        {
            await _pubCrawlService.RateBeerAsync(beer, like ? 1 : -1, this.GetPrimaryKeyString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to communicate with API: " + ex.Message, ex);
        }

        if (beersToDrink.Count > 0)
        {
            ScheduleNextBeer(beersToDrink);
        }
        else
        {
            ScheduleNextCheckin();
        }
    }

    private void ScheduleNextCheckin() => this.RegisterTimer(
        CheckInAsync,
        null,
        TimeSpan.FromMilliseconds(_random.Next(1000, 3000)),
        TimeSpan.FromMilliseconds(-1));

    private void ScheduleNextBeer(Stack<string> beersToDrink) => this.RegisterTimer(
        DrinkBeerAsync,
        beersToDrink,
        TimeSpan.FromMilliseconds(_random.Next(1000, 3000)),
        TimeSpan.FromMilliseconds(-1));

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}