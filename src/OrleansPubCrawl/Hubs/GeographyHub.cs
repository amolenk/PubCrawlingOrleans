using Microsoft.AspNetCore.SignalR;

public interface IGeographyHub
{
    Task ReceiveMessage(string user, string message);
}

public class GeographyHub : Hub<IGeographyHub>
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.ReceiveMessage(user, message);
    }
}
