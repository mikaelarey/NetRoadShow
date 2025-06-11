using System.Drawing;
using API_2.Dto;
using API_2.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Extensions;

namespace API_2.Models
{
    public class Elevator
    {
        public virtual int Id { get; private set; }
        public virtual int CurrentFloor { get; private set; } = 1;
        public virtual Direction Direction { get; private set; } = Direction.Idle;
        public Queue<int> Destinations { get; set; } = new();
        public string? Status { get; private set; } = Enums.Status.Idle.GetDisplayName();
        public List<int> PassengerDestinations { get; private set; } = new();

        public List<int> UpStops = new();
        public List<int> DownStops = new();

        public Elevator(int id) => Id = id;

        public virtual void SetPassengerDestinations() => PassengerDestinations = Destinations.ToList();
        
        public virtual void SetDirection(Direction direction) => Direction = direction;
        public virtual void SetStatus(string status) => Status = status;
        public virtual void SetCurrentFloor(int currentFloor) => CurrentFloor = currentFloor;
    }
}
