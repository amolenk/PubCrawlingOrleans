using Microsoft.AspNetCore.SignalR;
using Orleans.Concurrency;
using Orleans.Runtime;

[Reentrant]
internal sealed class EventMapHubListUpdater : BackgroundService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly EventMapHubProxy _hubProxy;
    private readonly ILogger _logger;

    public EventMapHubListUpdater(
        IGrainFactory grainFactory,
        ILocalSiloDetails localSiloDetails,
        IHubContext<EventMapHub, IEventMapHub> hubContext,
        ILogger<EventMapHubListUpdater> logger)
    {
        _grainFactory = grainFactory;
        _localSiloDetails = localSiloDetails;
        _hubProxy = new EventMapHubProxy(hubContext);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hubListGrain = _grainFactory.GetGrain<IEventMapHubListGrain>(Guid.Empty);
        var localSiloAddress = _localSiloDetails.SiloAddress;
        var hubRef = _grainFactory.CreateObjectReference<IEventMapHubProxy>(_hubProxy);

        // This runs in a loop because the HubListGrain does not use any form of persistence, so if the
        // host which it is activated on stops, then it will lose any internal state.
        // If HubListGrain was changed to use persistence, then this loop could be safely removed.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await hubListGrain.AddHubAsync(localSiloAddress, hubRef);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Error polling location hub list");
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch
                {
                    // Ignore cancellation exceptions, since cancellation is handled by the outer loop.
                }
            }
        }
    }
}