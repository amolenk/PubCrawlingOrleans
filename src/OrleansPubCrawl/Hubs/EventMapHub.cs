using Microsoft.AspNetCore.SignalR;

public interface IEventMapHub
{
    Task OnVenueAttendanceUpdated(string venueId, int attendance);
}

public class EventMapHub : Hub<IEventMapHub>
{
}
