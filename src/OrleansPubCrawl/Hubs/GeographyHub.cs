using Microsoft.AspNetCore.SignalR;

public interface IGeographyHub
{
    Task OnVenueAttendanceUpdated(string venueId, int attendance);
}

public class GeographyHub : Hub<IGeographyHub>
{
}
