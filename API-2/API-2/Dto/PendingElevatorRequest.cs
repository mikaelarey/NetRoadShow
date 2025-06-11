namespace API_2.Dto
{
    public class PendingElevatorRequest
    {
        public int Floor { get; set; }
        public List<int> Destinations { get; set; } = new List<int>();
    }
}
