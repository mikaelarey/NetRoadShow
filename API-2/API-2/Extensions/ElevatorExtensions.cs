using API_2.Dto;
using API_2.Enums;
using API_2.Models;

namespace API_2.Extensions
{
    public static class ElevatorExtensions
    {
        #region Add Stops - Adds stops to the elevator based on the elevator direction, and the request's current and destination floors
        public static bool AddStops(this Elevator elevator, ElevatorRequest request, int InitialFloorNumber, int NumberOfFloors)
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
                if (elevator.CurrentFloor < request.CurrentFloor)
                {
                    return false;
                }

                elevator.UpStops.Add(request.CurrentFloor);
                elevator.UpStops.Add(request.DestinationFloor);

            }
            else if (elevator.Direction == Direction.Up && request.Direction == Direction.Down)
            {
                if (elevator.CurrentFloor > request.CurrentFloor)
                {
                    return false;
                }

                elevator.DownStops.Add(request.CurrentFloor);
                elevator.DownStops.Add(request.DestinationFloor);
            }
            else if (elevator.Direction == Direction.Idle && request.Direction == Direction.Up)
            {
                if (elevator.CurrentFloor <= request.CurrentFloor)
                {
                    elevator.UpStops.Add(request.CurrentFloor);
                }
                else
                {
                    elevator.DownStops.Add(request.CurrentFloor);
                }

                elevator.UpStops.Add(request.DestinationFloor);
            }
            else if (elevator.Direction == Direction.Idle && request.Direction == Direction.Down)
            {
                if (elevator.CurrentFloor >= request.CurrentFloor)
                {
                    elevator.DownStops.Add(request.CurrentFloor);
                }
                else
                {
                    elevator.UpStops.Add(request.CurrentFloor);
                }

                elevator.DownStops.Add(request.DestinationFloor);
            }
            else
            {
                // Do Nothing
            }

            elevator.UpStops = elevator.UpStops.OrderBy(x => x).Distinct().ToList();
            elevator.DownStops = elevator.DownStops.OrderByDescending(x => x).Distinct().ToList();

            return true;
        }
        #endregion

        #region Set Destinations - Updates the elevator's destinations based on its current direction and stops
        public static void SetDestinations(this Elevator elevator)
        {
            List<int> destinations = new();

            elevator.UpStops = RemoveAdjacentDuplicates(elevator.UpStops);
            elevator.DownStops = RemoveAdjacentDuplicates(elevator.DownStops);

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

            destinations = RemoveAdjacentDuplicates(destinations);
            elevator.Destinations = new Queue<int>(destinations);
        }
        #endregion

        #region Remove Stops
        public static void RemoveStops(this Elevator elevator, int floor)
        {
            
            if (elevator.Direction == Direction.Up)
            {
                if (elevator.UpStops.Any())
                {
                    elevator.UpStops.Remove(floor);
                }
                else
                {
                    elevator.DownStops.Remove(floor);
                }
                
            }
            else if (elevator.Direction == Direction.Down)
            {
                if (elevator.DownStops.Any())
                {
                    elevator.DownStops.Remove(floor);
                }
                else
                {
                    elevator.UpStops.Remove(floor);
                }
            }
        }
        #endregion

        #region Remove Adjacent Duplicates - Remove consecutive duplicates from a list of integers
        private static List<int> RemoveAdjacentDuplicates(List<int> numbers)
        {
            if (numbers == null || numbers.Count == 0)
            {
                return new List<int>();
            }

            var result = new List<int>();
            result.Add(numbers.First());

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] != numbers[i - 1])
                {
                    result.Add(numbers[i]);
                }
            }

            return result;
        }
        #endregion

    }
}
