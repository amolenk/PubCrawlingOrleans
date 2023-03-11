using Orleans.Concurrency;
using Orleans.Runtime;

public interface IBeerLeaderboardGrain : IGrainWithIntegerKey
{
    Task<Dictionary<string, int>> GetTopBeersAsync();

    Task UpdateScoreAsync(string beerId, int score);
}

public class BeerLeaderboardGrain : Grain, IBeerLeaderboardGrain
{
    private readonly IPersistentState<BeerLeaderboardState> _state;
    private readonly ILogger _logger;

    public BeerLeaderboardGrain(
        [PersistentState("state")] IPersistentState<BeerLeaderboardState> state,
        ILogger<BeerLeaderboardGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

   public Task<Dictionary<string, int>> GetTopBeersAsync() => Task.FromResult(
        _state.State.Scores
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(10)
            .ToDictionary(x => x.Key, x => x.Value));

    public async Task UpdateScoreAsync(string beerId, int score)
    {
        if (_state.State.Scores.ContainsKey(beerId) &&
            _state.State.Scores[beerId] == score)
        {
            return;
        }

        _state.State.Scores[beerId] = score;

        await _state.WriteStateAsync();
    }
}

public class BeerLeaderboardState
{
    public Dictionary<string, int> Scores { get; set; } = new();
}
