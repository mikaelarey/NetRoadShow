using System.Runtime.CompilerServices;
using API_2.Dto;
using API_2.Enums;
using API_2.Models;

namespace API_2.Extensions
{
    public static class ElevatorExtensions
    {
        public static bool AddStops(this Elevator elevator, ElevatorRequest request)
        {
            if (elevator.Direction == Direction.Up && request.Direction == Direction.Up)
            {
                if (request.CurrentFloor >= elevator.CurrentFloor)
                {
                    elevator.UpStops.Add(request.CurrentFloor);
                    elevator.UpStops.Add(request.DestinationFloor);
                }
                else
                {
                    return false;
                }
            }
            else if (elevator.Direction == Direction.Down && request.Direction == Direction.Down)
            {
                if (request.CurrentFloor <= elevator.CurrentFloor)
                {
                    elevator.DownStops.Add(request.CurrentFloor);
                    elevator.DownStops.Add(request.DestinationFloor);
                }
                else
                {
                    return false;
                }
            }
            else if (elevator.Direction == Direction.Down && request.Direction == Direction.Up)
            {
                elevator.UpStops.Add(request.CurrentFloor);
                elevator.UpStops.Add(request.DestinationFloor);
            }
            else if (elevator.Direction == Direction.Up && request.Direction == Direction.Down)
            {
                elevator.DownStops.Add(request.CurrentFloor);
                elevator.DownStops.Add(request.DestinationFloor);
            }
            else if (elevator.Direction == Direction.Idle && request.Direction == Direction.Up)
            {
                if (elevator.CurrentFloor <= request.CurrentFloor)
                {
                    elevator.UpStops.Add(request.CurrentFloor);
                    elevator.UpStops.Add(request.DestinationFloor);
                }
                else
                {
                    elevator.DownStops.Add(request.CurrentFloor);
                    elevator.UpStops.Add(request.DestinationFloor);
                }
            }
            else if (elevator.Direction == Direction.Idle && request.Direction == Direction.Down)
            {
                if (elevator.CurrentFloor >= request.CurrentFloor)
                {
                    if (elevator.Destinations.Count() == 0)
                    {
                        elevator.DownStops.Add(request.CurrentFloor);
                        elevator.DownStops.Add(request.DestinationFloor);
                    }
                }
                else
                {
                    if (elevator.Destinations.Count() == 0)
                    {
                        elevator.UpStops.Add(request.CurrentFloor);
                        elevator.DownStops.Add(request.DestinationFloor);
                    }
                }
            }
            else
            {
                // TODO: Handle as needed
            }

            elevator.UpStops = elevator.UpStops.OrderBy(x => x).Distinct().ToList();
            elevator.DownStops = elevator.DownStops.OrderByDescending(x => x).Distinct().ToList();

            return true;
        }

        public static void SetDestinations(this Elevator elevator)
        {
            List<int> destinations = new();

            if (elevator.Direction == Direction.Up)
            {
                destinations.AddRange(elevator.UpStops);
                destinations.AddRange(elevator.DownStops);
            }
            else if (elevator.Direction == Direction.Down)
            {
                destinations.AddRange(elevator.DownStops);
                destinations.AddRange(elevator.UpStops);
            }

            //destinations.AddRange(OtherDestinations);
            destinations = RemoveAdjacentDuplicates(destinations);

            elevator.Destinations = new Queue<int>(destinations);
        }
        public static void RemoveStops(this Elevator elevator, int floor)
        {
            if (elevator.Direction == Direction.Up)
            {
                elevator.UpStops.Remove(floor);
            }
            else if (elevator.Direction == Direction.Down)
            {
                elevator.DownStops.Remove(floor);
            }
            else
            {
                // TODO: Idle. Handle as needed
            }
        }

        private static List<int> RemoveAdjacentDuplicates(List<int> numbers)
        {
            if (numbers == null || numbers.Count <= 1)
            {
                return numbers; // No duplicates possible or nothing to do
            }

            List<int> result = new List<int>();
            result.Add(numbers[0]); // Always add the first element

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] != numbers[i - 1])
                {
                    result.Add(numbers[i]);
                }
            }
            return result;
        }
    }
}
