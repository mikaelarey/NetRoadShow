namespace API_2.Dto
{
    public class PendingElevatorRequest
    {
        public int Floor { get; set; }
        public List<RequestDestination> Destinations { get; set; } = new List<RequestDestination>();
    }

    public class RequestDestination
    {
        public int ElevatorId { get; set; }
        public int Destination { get; set; }
    }
}