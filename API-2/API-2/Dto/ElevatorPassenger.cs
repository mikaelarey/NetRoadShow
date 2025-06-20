namespace API_2.Dto
{
    /// <summary>
    /// For UI Use only
    /// </summary>
    public class ElevatorPassenger
    {
        public int ElevatorId { get; set; }
        public List<int> Destinations { get; set; } = new List<int>();
    }
}
