using Orleans.Runtime;

public interface IBeerScoreGrain : IGrainWithIntegerCompoundKey
{
    Task UpdateRatingAsync(string crawlerId, int rating);
}

public class BeerScoreGrain : Grain, IBeerScoreGrain
{
    private readonly IPersistentState<BeerScoreState> _state;
    private readonly ILogger _logger;

    private Task _reportScoreTask = Task.CompletedTask;
    private int _lastReportedScore;

    public BeerScoreGrain(
        [PersistentState("state")] IPersistentState<BeerScoreState> state,
        ILogger<BeerScoreGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Set up a timer to regularly flush.
        RegisterTimer(
            _ =>
            {
                ReportScoreAsync();
                return Task.CompletedTask;
            },
            null,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(3));

        await base.OnActivateAsync(cancellationToken);
    }

    public async Task UpdateRatingAsync(string crawlerId, int rating)
    {
        if (_state.State.Ratings.ContainsKey(crawlerId))
        {
            if (rating == 0)
            {
                _state.State.Ratings.Remove(crawlerId);
            }
            else if (_state.State.Ratings[crawlerId] == rating)
            {
                return;
            }
        }

        _state.State.Ratings[crawlerId] = rating;

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

        int totalVotes = likes + dislikes;
        if (totalVotes == 0)
        {
            return 0;
        }
        
        double ratio = (double)likes / totalVotes;
        
        // use a sigmoid function to normalize the rating between 1 and 5
        double score = 5 * (1 / (1 + Math.Exp(-10 * (ratio - 0.5))));
        
        return (int)Math.Round(score);
    }

    private Task ReportScoreAsync()
    {
        if (!_reportScoreTask.IsCompleted)
        {
            return Task.CompletedTask;
        }

        _reportScoreTask = ReportScoreInternalAsync();
        return _reportScoreTask;

        async Task ReportScoreInternalAsync()
        {
            var score = CalculateScore(_state.State.Ratings.Values);
            if (score != _lastReportedScore)
            {
                var eventId = this.GetPrimaryKeyLong(out var beerId);

                var leaderboardGrain = GrainFactory.GetGrain<IBeerLeaderboardGrain>(eventId);
                await leaderboardGrain.UpdateScoreAsync(beerId, score);

                _lastReportedScore = score;
            }
        }
    }
}

public class BeerScoreState
{
    public Dictionary<string, int> Ratings { get; set; } = new();
}
