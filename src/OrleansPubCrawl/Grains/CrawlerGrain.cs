// using Orleans.Concurrency;
// using Orleans.Runtime;

// public interface ICrawlerGrain : IGrainWithStringKey
// {
//     // Task JoinEventAsync(Guid eventId);

//     Task CheckInAsync(Guid eventId, string venueId);

//     Task CheckOutAsync(Guid eventId);

// //    [AlwaysInterleave] // See https://dotnet.github.io/orleans/Documentation/grains/interleave.html
//     Task<IDrinkingVenueGrain> GetCurrentLocationAsync();
// }

// public class CrawlerGrain : Grain, ICrawlerGrain
// {
//     private readonly IPersistentState<CrawlerState> _state;
//     private readonly ILogger _logger;

//     public CrawlerGrain(
//         [PersistentState("crawler")] IPersistentState<CrawlerState> state,
//         ILogger<CrawlerGrain> logger)
//     {
//         _state = state;
//         _logger = logger;
//     }

//     // TODO Use something like this?
// //    private Guid GrainKey => this.GetPrimaryKey();

//     // public async Task JoinEventAsync(Guid eventId)
//     // {
//     //     var eventGrain = GrainFactory.GetGrain<IEventGrain>(eventId);
//     //     if (await eventGrain.ExistsAsync())
//     //     {
//     //         _state.State.EventId = eventId;
//     //         await _state.WriteStateAsync();
//     //     }
//     //     else
//     //     {
//     //         throw new ArgumentException($"Event {eventId} does not exist.", nameof(eventId));
//     //     }
//     // }

//     public async Task CheckInAsync(Guid eventId, string venueId)
//     {
//         var venue = GrainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId, null);

//         // TODO Why use AsReference() here?
//         await venue.EnterAsync(this.AsReference<ICrawlerGrain>());

//         _state.State.EventId = eventId;
//         _state.State.CurrentVenueId = venueId;
//         await _state.WriteStateAsync();
//     }

//     public async Task CheckOutAsync(Guid eventId)
//     {
//         if (_state.State.CurrentVenueId.Length > 0)
//         {
//             var venueId = _state.State.CurrentVenueId;
//             var venue = GrainFactory.GetGrain<IDrinkingVenueGrain>(eventId, venueId, null);

//             await venue.LeaveAsync(this.AsReference<ICrawlerGrain>());
//             await _state.ClearStateAsync();
//         }
//     }

//     public Task<IDrinkingVenueGrain> GetCurrentLocationAsync()
//     {
//         if (_state.State.EventId == Guid.Empty)
//         {
//             throw new BadHttpRequestException("Crawler is not currently in an event.");
//         }

//         var venue = GrainFactory.GetGrain<IDrinkingVenueGrain>(_state.State.EventId, _state.State.CurrentVenueId, null);

//         return Task.FromResult(venue);
//     }
// }

// public class CrawlerState
// {
//     public Guid EventId { get; set; } = Guid.Empty;
//     public string CurrentVenueId { get; set; } = string.Empty;
// }