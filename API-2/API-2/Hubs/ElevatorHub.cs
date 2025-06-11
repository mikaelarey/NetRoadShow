using API_2.Services;
using Microsoft.AspNetCore.SignalR;

namespace API_2.Hubs
{
    public class ElevatorHub : Hub
    {
        private readonly ElevatorSystemService _elevatorSystemService;

        public ElevatorHub(ElevatorSystemService elevatorService)
        {
            _elevatorSystemService = elevatorService;
        }

        public override async Task OnConnectedAsync()
        {
            _elevatorSystemService.TriggerToStartSignal();
            await Clients.Caller.SendAsync("Subscribed", "Simulation started. Subscribed to updates.");
            await base.OnConnectedAsync();

            var elevatorUpdate = _elevatorSystemService.GetElevatorUpdates();
            await Clients.All.SendAsync("ElevatorStatusUpdated", elevatorUpdate);
        }
    }
}
