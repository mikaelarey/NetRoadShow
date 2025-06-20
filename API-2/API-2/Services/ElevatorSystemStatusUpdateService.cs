using API_2.Dto;

namespace API_2.Services
{
    /// <summary>
    /// Service to manage the status of the elevator system.
    /// For UI Use only.
    /// </summary>
    public class ElevatorSystemStatusUpdateService
    {
        private List<ElevatorPassenger> ElevatorPassenger { get; set; } = new();

        public void AddElevatorPassenger(int elevatorId, int destinationFloor)
        {
            if (ElevatorPassenger.Exists(x => x.ElevatorId == elevatorId))
            {
                var elevatorPassenger = ElevatorPassenger.First(x => x.ElevatorId == elevatorId);

                if (elevatorPassenger.Destinations.Exists(x => x == destinationFloor))
                {
                    elevatorPassenger.Destinations.Add(destinationFloor);
                }
            }
            else
            {
                var elevatorPassenger = new ElevatorPassenger
                {
                    ElevatorId = elevatorId,
                    Destinations = new List<int> { destinationFloor }
                };

                ElevatorPassenger.Add(elevatorPassenger);
            }
        }

        public void RemoveElevatorPassenger(int elevatorId, int currentFloor)
        {
            if (ElevatorPassenger.Exists(x => x.ElevatorId == elevatorId))
            {
                var elevatorPassenger = ElevatorPassenger
                    .Where(x => x.ElevatorId == elevatorId)
                    .First();

                for (int i = elevatorPassenger.Destinations.Count - 1; i >= 0; i--)
                {
                    if (elevatorPassenger.Destinations[i] == currentFloor)
                    {
                        elevatorPassenger.Destinations.RemoveAt(i);
                    }
                }
            }
        }

        public List<ElevatorPassenger> GetElevatorPassenger()
        {
            return ElevatorPassenger;
        }
    }
}
