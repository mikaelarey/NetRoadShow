using API_2.Dto;
using API_2.Services;

namespace API_2.BackgroundServices
{
    public class ElevatorRequestBackgroundService : BackgroundService
    {
        private readonly ElevatorSystemService _elevatorSystemService;
        private const int REQUEST_FREQUENCY = 5000;

        public ElevatorRequestBackgroundService(ElevatorSystemService elevatorSystemService)
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
                await Task.Delay(REQUEST_FREQUENCY);

                Random rnd = new Random();
                int floorFrom = rnd.Next(intialFloorNumber, numberOfFloors + 1);
                int floorTo = rnd.Next(intialFloorNumber, numberOfFloors + 1);

                if (floorFrom != floorTo && floorFrom <= numberOfFloors && floorTo <= numberOfFloors)
                {
                    var elevatorRequest = new ElevatorRequest(floorFrom, floorTo);
                    _elevatorSystemService.RequestElevator(elevatorRequest);
                }
            }
        }
    }
}
