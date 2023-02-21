using Microsoft.AspNetCore.SignalR;

public interface IGeographyHub
{
    Task OnVenueUpdated(VenueAttendance venueAttendance);
}

public class GeographyHub : Hub<IGeographyHub>
{
    // public async Task SendMessage(string user, string message)
    // {
    //     await Clients.All.ReceiveMessage(user, message);
    // }
}
