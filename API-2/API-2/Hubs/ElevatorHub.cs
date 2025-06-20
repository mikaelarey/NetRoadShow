using Microsoft.AspNetCore.SignalR;

namespace API_2.Hubs
{
    public class ElevatorHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Subscribed", "Simulation started. Subscribed to updates.");
            await base.OnConnectedAsync();
        }
    }
}
