using Orleans.Concurrency;
using Orleans.Runtime;

public interface IBeerChartGrain : IGrainWithGuidKey
{
    Task UpdateAsync(ICrawlerGrain crawler);

    Task DislikeAsync(ICrawlerGrain crawler);
}

public class BeerGrain : Grain, IBeerGrain
{
    private readonly IPersistentState<BeerState> _state;
    private readonly ILogger _logger;

    public BeerGrain(
        [PersistentState("state")] IPersistentState<BeerState> state,
        ILogger<BeerGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public static string GetKey(Guid eventId, string venueId, string beerId)
    {
        return $"{eventId}/{venueId}/{beerId}";
    }

    public async Task RegisterAsync(IDrinkingVenueGrain venue)
    {
        var primaryKey = venue.GetPrimaryKeyString();

        if (!_state.State.ServedBy.Contains(primaryKey))
        {
            _state.State.ServedBy.Add(primaryKey);
            await _state.WriteStateAsync();
        }
    }

    public async Task LikeAsync(ICrawlerGrain crawler)
    {
        var crawlerId = crawler.GetPrimaryKeyString();
        var venue = await crawler.GetCurrentLocationAsync();

        // TODO Do this implicitly?
        if (!_state.State.ServedBy.Contains(venue.GetPrimaryKeyString()))
        {
            var eventId = venue.GetPrimaryKey(out var venueId);

            throw new BadHttpRequestException($"Specified beer is not served at venue {venueId}.");
        }

        var stateChanged = false;

        if (!_state.State.LikedBy.Contains(crawlerId))
        {
            _state.State.LikedBy.Add(crawlerId);
            stateChanged = true;
        }

        if (_state.State.DislikedBy.Contains(crawlerId))
        {
            _state.State.DislikedBy.Remove(crawlerId);
            stateChanged = true;
        }

        if (stateChanged)
        {
            await _state.WriteStateAsync();
        }

        // Log the number of likes
        _logger.LogInformation("🍺 Beer {Beer} has {Likes} 👍's and {Dislikes} 👎's.",
            this.GetPrimaryKeyString(), _state.State.LikedBy.Count, _state.State.DislikedBy.Count);
    }

    public async Task DislikeAsync(ICrawlerGrain crawler)
    {
        var crawlerId = crawler.GetPrimaryKeyString();
        var venue = await crawler.GetCurrentLocationAsync();

        // TODO Do this implicitly?
        if (!_state.State.ServedBy.Contains(venue.GetPrimaryKeyString()))
        {
            var eventId = venue.GetPrimaryKey(out var venueId);

            throw new BadHttpRequestException($"Specified beer is not served at venue {venueId}.");
        }

        var stateChanged = false;

        if (!_state.State.DislikedBy.Contains(crawlerId))
        {
            _state.State.DislikedBy.Add(crawlerId);
            stateChanged = true;
        }

        if (_state.State.LikedBy.Contains(crawlerId))
        {
            _state.State.LikedBy.Remove(crawlerId);
            stateChanged = true;
        }

        if (stateChanged)
        {
            await _state.WriteStateAsync();
        }

        // Log the number of likes
        _logger.LogInformation("🍺 Beer {Beer} has {Likes} 👍's and {Dislikes} 👎's.",
            this.GetPrimaryKeyString(), _state.State.LikedBy.Count, _state.State.DislikedBy.Count);
    }

    private string GetBeerId => this.GetPrimaryKeyString().Split('/').Last();
}

public class BeerState
{
    public HashSet<string> ServedBy { get; set; } = new();
    public HashSet<string> LikedBy { get; set; } = new();
    public HashSet<string> DislikedBy { get; set; } = new();
}
