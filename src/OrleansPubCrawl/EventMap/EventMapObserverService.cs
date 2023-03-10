using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;

// It would be even nicer if we could let this service participate in the Orleans
// lifecycle (or make it a GrainService).
// Unfortunately, that's not possible yet: https://github.com/dotnet/orleans/issues/7556
internal sealed class EventMapObserverService : BackgroundService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly IEventMapObserver _observer;
    private readonly ILogger _logger;

    public EventMapObserverService(
        IGrainFactory grainFactory,
        ILocalSiloDetails localSiloDetails,
        IHubContext<EventMapHub> hubContext,
        ILogger<EventMapObserverService> logger)
    {
        _grainFactory = grainFactory;
        _localSiloDetails = localSiloDetails;
        _observer = new EventMapObserver(hubContext);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var observerListGrain = _grainFactory.GetGrain<IEventMapObserverListGrain>(Guid.Empty);
        var localSiloAddress = _localSiloDetails.SiloAddress;
        var hubRef = _grainFactory.CreateObjectReference<IEventMapObserver>(_observer);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await observerListGrain.AddObserverAsync(localSiloAddress, hubRef);
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