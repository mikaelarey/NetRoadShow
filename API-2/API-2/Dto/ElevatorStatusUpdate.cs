using API_2.Models;

namespace API_2.Dto
{
    public class ElevatorStatusUpdate
    {
        public List<Elevator> Elevators { get; set; } = new List<Elevator>();
        public List<PendingElevatorRequest> Requests { get; set; } = new List<PendingElevatorRequest>();
    }
}
