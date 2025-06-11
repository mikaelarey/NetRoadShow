using API_2.Services;

namespace API_2.BackgroundServices
{
    public class ElevatorSimulationBackgroundService : BackgroundService
    {
        private readonly ElevatorSystemService _elevatorSystemService;
        private const int TRAVEL_AND_LOADING_TIME = 10000;

        public ElevatorSimulationBackgroundService(ElevatorSystemService elevatorService)
        {
            _elevatorSystemService = elevatorService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _elevatorSystemService.StartSignal;       // Wait for SignalR readiness
            _elevatorSystemService.InitializeElevators(4);

            while (!stoppingToken.IsCancellationRequested)
            {
                await _elevatorSystemService.StepAllElevatorAsync();
                await Task.Delay(TRAVEL_AND_LOADING_TIME);
            }
        }
    }
}
