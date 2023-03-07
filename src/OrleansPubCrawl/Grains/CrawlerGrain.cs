using Orleans.Runtime;

public interface ICrawlerGrain : IGrainWithIntegerCompoundKey
{
    Task<string> GetVenueAsync();
    Task SetVenueAsync(string venueId);
    Task ClearVenueAsync();

    Task CheckInAsync(string venueId);
    Task CheckOutAsync();

    Task<IDictionary<string, int>> GetBeerRatingsAsync();
    Task RateBeerAsync(string beerId, int rating);

    Task<CrawlerStatus> GetStatusAsync();
}

public class CrawlerGrain : Grain, ICrawlerGrain
{
    private readonly IPersistentState<CrawlerGrainState> _state;
    private readonly ILogger _logger;

    public CrawlerGrain(
        [PersistentState("state")] IPersistentState<CrawlerGrainState> state,
        ILogger<CrawlerGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public Task<string> GetVenueAsync() => Task.FromResult(_state.State.CurrentVenue);

    public async Task SetVenueAsync(string venueId)
    {
        _state.State.CurrentVenue = venueId;
        await _state.WriteStateAsync();
    }

    public Task ClearVenueAsync() => SetVenueAsync(string.Empty);

    public async Task CheckInAsync(string venueId)
    {
        if (_state.State.CurrentVenue == venueId)
        {
            // Already checked in.
            return;
        }

        var eventId = this.GetPrimaryKeyLong(out var crawlerId);

        if (_state.State.CurrentVenue.Length > 0)
        {
            var currentVenueGrain = GrainFactory.GetGrain<IDrinkingVenueGrain>(eventId, _state.State.CurrentVenue, null);
            await currentVenueGrain.RemoveCrawlerAsync(crawlerId);
        }

        var newVenueGrain = GrainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId, null);
        await newVenueGrain.AddCrawlerAsync(crawlerId);

        _state.State.CurrentVenue = venueId;
        await _state.WriteStateAsync();
    }

    public async Task CheckOutAsync()
    {
        if (_state.State.CurrentVenue.Length > 0)
        {
            var eventId = this.GetPrimaryKeyLong(out var crawlerId);

            var venueGrain = GrainFactory.GetGrain<IDrinkingVenueGrain>(eventId, _state.State.CurrentVenue, null);
            await venueGrain.RemoveCrawlerAsync(crawlerId);
        }

        _state.State.CurrentVenue = string.Empty;
        await _state.WriteStateAsync();
    }

    public Task<IDictionary<string, int>> GetBeerRatingsAsync()
    {
        return Task.FromResult<IDictionary<string, int>>(_state.State.BeerRatings);
    }

    public async Task RateBeerAsync(string beerId, int rating)
    {
        var eventId = this.GetPrimaryKeyLong(out var crawlerId);

        var beerSelectionGrain = GrainFactory.GetGrain<IBeerSelectionGrain>(eventId);
        if (!await beerSelectionGrain.IsAvailableAsync(beerId))
        {
            throw new BeerNotAvailableException(beerId, eventId);
        }

        _state.State.BeerRatings[beerId] = rating;
        await _state.WriteStateAsync();

        var beerTotalScoreGrain = GrainFactory.GetGrain<IBeerScoreGrain>(eventId, beerId, null);
        await beerTotalScoreGrain.UpdateRatingAsync(crawlerId, rating);
    }

    public Task<CrawlerStatus> GetStatusAsync() => Task.FromResult(new CrawlerStatus
    {
        VenueId = _state.State.CurrentVenue,
        BeerRatings = _state.State.BeerRatings
    });
}

public class CrawlerGrainState
{
    public string CurrentVenue { get; set; } = string.Empty;
    public Dictionary<string, int> BeerRatings { get; set; } = new();
}
