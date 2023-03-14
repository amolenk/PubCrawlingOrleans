using Microsoft.AspNetCore.SignalR;

public interface IEventMapObserver : IGrainObserver
{
    Task OnVenueAttendanceUpdatedAsync(string eventId, string venueId, int attendance);
}

public class EventMapObserver : IEventMapObserver
{
    private readonly IHubContext<EventMapHub> _context;

    public EventMapObserver(IHubContext<EventMapHub> context)
    {
        _context = context;
    }

    public Task OnVenueAttendanceUpdatedAsync(string eventId, string venueId, int attendance)
    {
        return _context.Clients.All.SendAsync("OnVenueAttendanceUpdated", eventId, venueId, attendance);
    }
}
