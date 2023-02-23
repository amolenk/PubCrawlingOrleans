using Orleans.Runtime;

public interface IBeerTotalScoreGrain : IGrainWithIntegerCompoundKey
{
    Task<int> GetTotalScoreAsync();

    Task UpdateRatingAsync(string crawlerId, int rating);
}

public class BeerTotalScoreGrain : Grain, IBeerTotalScoreGrain
{
    private readonly IPersistentState<BeerTotalScoreState> _state;
    private readonly ILogger _logger;

    public BeerTotalScoreGrain(
        [PersistentState("state")] IPersistentState<BeerTotalScoreState> state,
        ILogger<BeerTotalScoreGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    // TODO Reentrant?
    public Task<int> GetTotalScoreAsync() => Task.FromResult(_state.State.CurrentScore);

    public async Task UpdateRatingAsync(string crawlerId, int rating)
    {
        if (_state.State.Ratings.ContainsKey(crawlerId) &&
            _state.State.Ratings[crawlerId] == rating)
        {
            return;
        }

        _state.State.Ratings[crawlerId] = rating;
        _state.State.PreviousScore = _state.State.CurrentScore;
        _state.State.CurrentScore = CalculateScore(_state.State.Ratings.Values);

        await _state.WriteStateAsync();
    }

    private int CalculateScore(IEnumerable<int> ratings)
    {
        int likes = 0;
        int dislikes = 0;

        // Count the number of likes and dislikes in the list
        foreach (var rating in ratings)
        {
            if (rating == 1)
            {
                likes++;
            }
            else if (rating == -1)
            {
                dislikes++;
            }
        }

        // Calculate the percentage of likes
        double totalVotes = likes + dislikes;
        double likePercentage = (totalVotes > 0) ? likes / totalVotes : 0;

        // Map the percentage to a 1-5 scale
        int score = (int)Math.Round(likePercentage * 4) + 1;

        return score;
    }
}

public class BeerTotalScoreState
{
    public int CurrentScore { get; set; }
    public int PreviousScore { get; set; }
    public Dictionary<string, int> Ratings { get; set; } = new();
}
