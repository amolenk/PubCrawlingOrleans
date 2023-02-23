using Orleans.Runtime;

public interface IBeerHighScoresGrain : IGrainWithIntegerKey
{
    Task<Dictionary<string, int>> GetTopBeersAsync();

    Task UpdateScoreAsync(string beerId, int score);
}

public class BeerHighScoresGrain : Grain, IBeerHighScoresGrain
{
    private readonly IPersistentState<BeerHighScoreState> _state;
    private readonly ILogger _logger;

    public BeerHighScoresGrain(
        [PersistentState("state")] IPersistentState<BeerHighScoreState> state,
        ILogger<BeerHighScoresGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    // TODO Reentrant?
   public Task<Dictionary<string, int>> GetTopBeersAsync() => Task.FromResult(
        _state.State.Scores
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(10)
            .ToDictionary(x => x.Key, x => x.Value));

    public async Task UpdateScoreAsync(string beerId, int score)
    {
        _logger.LogInformation("Updating score for beer {BeerId} to {Score}", beerId, score);

        if (_state.State.Scores.ContainsKey(beerId) &&
            _state.State.Scores[beerId] == score)
        {
            return;
        }

        _state.State.Scores[beerId] = score;

        await _state.WriteStateAsync();
    }
}

public class BeerHighScoreState
{
    public Dictionary<string, int> Scores { get; set; } = new();
}
