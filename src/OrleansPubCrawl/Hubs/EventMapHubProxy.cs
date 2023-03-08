using Microsoft.AspNetCore.SignalR;

public interface IEventMapHubProxy : IGrainObserver
{
    Task SendVenueAttendanceUpdatedAsync(string eventId, string venueId, int attendance);
}

public class EventMapHubProxy : IEventMapHubProxy
{
    private readonly IHubContext<EventMapHub> _hubContext;

    public EventMapHubProxy(IHubContext<EventMapHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendVenueAttendanceUpdatedAsync(string eventId, string venueId, int attendance)
    {
        // TODO Include event ID in the message.
        return _hubContext.Clients.All.SendAsync("OnVenueAttendanceUpdated", venueId, attendance);
    }
}
