using API_2.Dto;
using API_2.Services;

namespace API_2.BackgroundServices
{
    public class ClientUpdateBackgroundService : BackgroundService
    {
        private readonly ElevatorSystemService _elevatorSystemService;
        private const int UPDATE_FREQUENCY = 1000;

        public ClientUpdateBackgroundService(ElevatorSystemService elevatorSystemService)
        {
            _elevatorSystemService = elevatorSystemService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int numberOfFloors = 10;
            const int numberOfElevators = 4;
            const int intialFloorNumber = 1;

            _elevatorSystemService.InitializeElevators(intialFloorNumber, numberOfFloors, numberOfElevators);
            await _elevatorSystemService.SendUpdateToClient();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(UPDATE_FREQUENCY);
                await _elevatorSystemService.SendUpdateToClient();
            }
        }

    }
}
