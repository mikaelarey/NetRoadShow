using API_2.Enums;

namespace API_2.Dto
{
    public class ElevatorRequest
    {
        public Guid Id { get; private set; }
        public int CurrentFloor { get; private set; }
        public int DestinationFloor { get; private set; }
        public Direction Direction { get; private set; }

        public ElevatorRequest(int currentFloor, int destinationFloor)
        {
            Id = Guid.NewGuid();
            CurrentFloor = currentFloor;
            DestinationFloor = destinationFloor;
            Direction = currentFloor < destinationFloor ? Direction.Up : Direction.Down;
        }
    }
}
