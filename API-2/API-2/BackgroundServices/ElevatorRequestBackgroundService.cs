
using API_2.Dto;
using API_2.Services;
namespace API_2.BackgroundServices
{
    public class ElevatorRequestBackgroundService : BackgroundService
    {
        private readonly ElevatorSystemService elevatorSystemService;
        private const int TRAVEL_AND_LOADING_TIME = 5000;

        public ElevatorRequestBackgroundService(ElevatorSystemService elevatorService)
        {
            elevatorSystemService = elevatorService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (elevatorSystemService.StartSignal != null && !elevatorSystemService.StartSignal.IsCompleted)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TRAVEL_AND_LOADING_TIME);

                    Random rnd = new Random();
                    int floorFrom = rnd.Next(1, 10);
                    int floorTo = rnd.Next(1, 10);

                    if (floorFrom != floorTo)
                    {
                        var elevatorRequest = new ElevatorRequest(floorFrom, floorTo);
                        await elevatorSystemService.RequestElevator(elevatorRequest);
                    }
                }
            }

        }
    }
}
