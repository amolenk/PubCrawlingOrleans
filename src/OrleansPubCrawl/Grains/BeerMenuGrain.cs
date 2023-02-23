// using Orleans.Runtime;
// using Orleans.Utilities;

// // TODO Rename to IDrinkMenuGrain
// public interface IBeerMenuGrain : IGrainWithStringKey
// {
//     Task<IEnumerable<Beer>> GetAsync();

//     Task<bool> IsAvailable(string beerId);

//     Task UpdateAsync(IEnumerable<Beer> beers);
// }

// public class BeerMenuGrain : Grain, IBeerMenuGrain
// {
//     private readonly IPersistentState<BeerMenuState> _state;
//     private readonly ILogger _logger;

//     public BeerMenuGrain(
//         [PersistentState("state")] IPersistentState<BeerMenuState> state,
//         ILogger<BeerMenuGrain> logger)
//     {
//         _state = state;
//         _logger = logger;
//     }

//     public Task<IEnumerable<Beer>> GetAsync()
//     {
//         var beers = _state.State.Beers.Values.ToList();

//         // If there are no beers, deactivate the grain.
//         if (beers.Count == 0)
//         {
//             DeactivateOnIdle();
//         }

//         return Task.FromResult<IEnumerable<Beer>>(beers);
//     }

//     public Task<bool> IsAvailable(string beerId)
//     {
//         var isAvailable = _state.State.Beers.ContainsKey(beerId);

//         if (!isAvailable)
//         {
//             DeactivateOnIdle();
//         }

//         return Task.FromResult(isAvailable);
//     }

//     public async Task UpdateAsync(IEnumerable<Beer> beers)
//     {
//         _state.State.Beers = beers.ToDictionary(b => b.Id);

//         await _state.WriteStateAsync();
//     }

//     // public async Task RegisterBeerAsync(string beerId)
//     // {
//     //     // var eventId = this.GetPrimaryKey(out var venueId);

//     //     // if (!_state.State.IsRegistered)
//     //     // {
//     //     //     throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
//     //     // }

//     //     // var beerKey = BeerGrain.GetKey(eventId, venueId, beerId);

//     //     // var beerGrain = GrainFactory.GetGrain<IBeerGrain>(beerKey);
//     //     // await beerGrain.RegisterAsync(this);
//     // }

//     // public Task ObserveAsync(IHandleVenueEvents observer)
//     // {
//     //     // TODO check what the parameters mean
//     //     _observerManager.Subscribe(observer, observer);

//     //     return Task.CompletedTask;
//     // }

//     // public async Task EnterAsync(ICrawlerGrain crawler)
//     // {
//     //     var eventId = this.GetPrimaryKey(out var venueId);

//     //     if (!_state.State.IsRegistered)
//     //     {
//     //         throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
//     //     }

//     //     var crawlerId = crawler.GetPrimaryKeyString();

//     //     if (!_state.State.Crawlers.Contains(crawlerId))
//     //     {
//     //         _state.State.Crawlers.Add(crawlerId);
//     //         await _state.WriteStateAsync();
//     //     }

//     //     await OnNumberOfCrawlersChangedAsync();
//     // }

//     // public async Task LeaveAsync(ICrawlerGrain crawler)
//     // {
//     //     var eventId = this.GetPrimaryKey(out var venueId);

//     //     if (!_state.State.IsRegistered)
//     //     {
//     //         throw new BadHttpRequestException($"Venue {venueId} does not participate in event {eventId}.");
//     //     }

//     //     var crawlerId = crawler.GetPrimaryKeyString();

//     //     if (_state.State.Crawlers.Contains(crawlerId))
//     //     {
//     //         _state.State.Crawlers.Remove(crawlerId);
//     //         await _state.WriteStateAsync();
//     //     }

//     //     await OnNumberOfCrawlersChangedAsync();
//     // }

//     // private async Task OnNumberOfCrawlersChangedAsync()
//     // {
//     //     this.GetPrimaryKey(out var venueId);

//     //     _logger.LogInformation("🍻 There are now {Count} crawler(s) in {Venue}",
//     //         _state.State.Crawlers.Count,
//     //         venueId);

//     //     await _observerManager.Notify(obs => obs.OnNumberOfCrawlersChangedAsync(
//     //         venueId, _state.State.Crawlers.Count));
//     // }
// }

// public class BeerMenuState
// {
//     public Dictionary<string, Beer> Beers { get; set; } = new();
// }
