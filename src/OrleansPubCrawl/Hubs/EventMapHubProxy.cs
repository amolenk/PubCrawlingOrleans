using Microsoft.AspNetCore.SignalR;

public interface IEventMapHubProxy : IGrainObserver
{
    Task OnVenueAttendanceUpdatedAsync(string eventId, string venueId, int count);
}

public class EventMapHubProxy : IEventMapHubProxy
{
    private readonly IHubContext<EventMapHub, IEventMapHub> _hubContext;

    public EventMapHubProxy(IHubContext<EventMapHub, IEventMapHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task OnVenueAttendanceUpdatedAsync(string eventId, string venueId, int attendance)
    {
        // TODO Include event ID in the message.
        await _hubContext.Clients.All.OnVenueAttendanceUpdated(venueId, attendance);
    }
}