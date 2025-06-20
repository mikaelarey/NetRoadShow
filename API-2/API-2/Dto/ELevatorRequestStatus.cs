namespace API_2.Dto
{
    public class ELevatorRequestStatus
    {
        public int ElevatorId { get; set; } = 0;
        public ElevatorRequest ElevatorRequest { get; set; }
        public bool IsAccommodated { get; set; }
    }
}
