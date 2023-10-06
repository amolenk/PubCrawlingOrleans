using Orleans.Runtime;

public interface ICrawlerGrain : IGrainWithIntegerCompoundKey
{
    Task CheckInAsync(IDrinkingVenueGrain venue);
    Task CheckOutAsync();

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

    public async Task CheckInAsync(IDrinkingVenueGrain venue)
    {
        // If we're already checked in, do nothing.
        if (_state.State.CurrentVenue == venue)
        {
            // Already checked in.
            return;
        }

        var self = this.AsReference<ICrawlerGrain>();

        // If we're checking in to a new venue, remove ourselves from the old one.
        if (_state.State.CurrentVenue is {})
        {
            await _state.State.CurrentVenue.RemoveCrawlerAsync(self);
        }

        // Add ourselves to the new venue.
        await venue.AddCrawlerAsync(self);
        _state.State.CurrentVenue = venue;

        await _state.WriteStateAsync();
    }

    public async Task CheckOutAsync()
    {
        if (_state.State.CurrentVenue is null)
        {
            return;
        }

        var self = this.AsReference<ICrawlerGrain>();

        await _state.State.CurrentVenue.RemoveCrawlerAsync(self);

        _state.State.CurrentVenue = null;
        await _state.WriteStateAsync();
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

        var beerTotalScoreGrain = GrainFactory.GetGrain<IBeerScoreGrain>(primaryKey: eventId, keyExtension: beerId);
        await beerTotalScoreGrain.UpdateRatingAsync(crawlerId, rating);
    }

    public Task<CrawlerStatus> GetStatusAsync()
    {
        string venueId;

        if (_state.State.CurrentVenue is null)
        {
            venueId = string.Empty;
        }
        else
        {
            _state.State.CurrentVenue.GetPrimaryKeyLong(out venueId);
        }

        return Task.FromResult(new CrawlerStatus
        {
            VenueId = venueId,
            BeerRatings = _state.State.BeerRatings
        });
    }
}

public class CrawlerGrainState
{
    public IDrinkingVenueGrain? CurrentVenue { get; set; }
    public Dictionary<string, int> BeerRatings { get; set; } = new();
}
