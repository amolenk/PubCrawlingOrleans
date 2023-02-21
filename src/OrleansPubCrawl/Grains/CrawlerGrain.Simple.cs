// using Orleans.Runtime;

// public interface ICrawlerGrain : IGrainWithStringKey
// {
//     Task CheckInAsync(string venueId);

//     Task CheckOutAsync();

//     Task<string> GetCurrentVenueAsync();
// }

// public class CrawlerGrain : Grain, ICrawlerGrain
// {
//     private readonly IPersistentState<CrawlerState> _state;
//     private readonly ILogger _logger;

//     public CrawlerGrain(
//         [PersistentState("default")] IPersistentState<CrawlerState> state,
//         ILogger<CrawlerGrain> logger)
//     {
//         _state = state;
//         _logger = logger;
//     }

//     public async Task CheckInAsync(string venueId)
//     {
//         _state.State.CurrentVenueId = venueId;
//         await _state.WriteStateAsync();
//     }

//     public async Task CheckOutAsync()
//     {
//         await _state.ClearStateAsync();
//     }

//     public Task<string> GetCurrentVenueAsync()
//     {
//         return Task.FromResult(_state.State.CurrentVenueId);
//     }
// }

// public class CrawlerState
// {
//     public string CurrentVenueId { get; set; } = string.Empty;
// }