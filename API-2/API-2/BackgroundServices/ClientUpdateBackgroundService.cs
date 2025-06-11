using API_2.Dto;
using API_2.Services;

namespace API_2.BackgroundServices
{
    public class ClientUpdateBackgroundService : BackgroundService
    {
        private readonly ElevatorSystemService _elevatorSystemService;
        private const int TRAVEL_AND_LOADING_TIME = 1000;

        public ClientUpdateBackgroundService(ElevatorSystemService elevatorService)
        {
            _elevatorSystemService = elevatorService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TRAVEL_AND_LOADING_TIME);
                await _elevatorSystemService.SendUpdateToClientAsync();
            }
        }
    }
}
