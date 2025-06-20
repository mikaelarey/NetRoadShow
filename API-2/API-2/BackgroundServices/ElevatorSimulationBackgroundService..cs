using API_2.Dto;
using API_2.Services;

namespace API_2.BackgroundServices
{
    public class ElevatorSimulationBackgroundService : BackgroundService
    {
        private readonly ElevatorSystemService _elevatorSystemService;
        private const int TRAVEL_AND_LOADING_TIME = 10000;

        public ElevatorSimulationBackgroundService(ElevatorSystemService elevatorSystemService)
        {
            _elevatorSystemService = elevatorSystemService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _elevatorSystemService.StepAllElevatorAsync();
                await Task.Delay(TRAVEL_AND_LOADING_TIME);
            }
        }
    }
}
