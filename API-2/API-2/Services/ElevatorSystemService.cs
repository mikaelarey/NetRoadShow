using System.Collections.Concurrent;
using API_2.Dto;
using API_2.Enums;
using API_2.Extensions;
using API_2.Hubs;
using API_2.Models;
using Microsoft.AspNetCore.SignalR;

namespace API_2.Services
{
    public class ElevatorSystemService
    {
        private readonly ElevatorSystemStatusUpdateService _elevatorSystemStatusUpdateService;
        private readonly IHubContext<ElevatorHub> _hubContext;
        private ConcurrentBag<ELevatorRequestStatus> _elevatorRequests { get; set; } = new();
        private ConcurrentBag<Elevator> _elevators;
       
        private const int INITIAL_ELEVATOR_ID = 1;

        private int NumberOfFloors { get; set; }
        private int NumberOfElevators { get; set; }
        private int InitialFloorNumber { get; set; }

        public ElevatorSystemService(
            ElevatorSystemStatusUpdateService elevatorSystemStatusUpdateService,
            IHubContext<ElevatorHub> hubContext)
        {
            _elevators = new ConcurrentBag<Elevator>();
            _elevatorSystemStatusUpdateService = elevatorSystemStatusUpdateService;
            _hubContext = hubContext;
        }

        #region Initialize Elevators
        public void InitializeElevators(int intialFloorNumber, int numberOfFloors, int numberOfElevators)
        {
            NumberOfFloors = numberOfFloors;
            NumberOfElevators = numberOfElevators;
            InitialFloorNumber = intialFloorNumber;

            var elevators = Enumerable
                .Range(INITIAL_ELEVATOR_ID, NumberOfElevators)
                .Select(id => new Elevator(id));

            _elevators = new ConcurrentBag<Elevator>(elevators);
        }
        #endregion

        #region Request Elevator
        public void RequestElevator(ElevatorRequest request)
        {
            var elevator = GetBestElevator(request);

            if (elevator is not null)
            {
                var isAccommodated = elevator.AddStops(request, InitialFloorNumber, NumberOfFloors);

                var requestStatus = new ELevatorRequestStatus
                {
                    ElevatorRequest = request,
                    IsAccommodated = isAccommodated,
                    ElevatorId = isAccommodated ? elevator.Id : 0
                };

                _elevatorRequests.Add(requestStatus);

                if (isAccommodated)
                {
                    if (elevator.Direction == Direction.Idle)
                    {
                        elevator.SetDirection(request.Direction);
                    }

                    elevator.SetDestinations();
                }

                return;
            }

            Console.WriteLine("No available elevators.");
        }
        #endregion

        #region StepAllElevatorAsync
        public async Task StepAllElevatorAsync()
        {
            var tasks = new List<Task>();

            foreach (var elevator in _elevators)
            {
                var task = StepElevatorAsync(elevator);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

        }
        #endregion

        #region
        private async Task StepElevatorAsync(Elevator elevator)
        {
            if (elevator.Destinations.Count == 0)
            {
                elevator.SetDirection(Direction.Idle);
                elevator.SetStatus(Status.Idle.GetDescription());
                return;
            }

            var targetDestination = elevator.Destinations.Peek();

            if (elevator.CurrentFloor < targetDestination)
            {
                elevator.SetCurrentFloor(elevator.CurrentFloor + 1);
                elevator.SetDirection(Direction.Up);
                elevator.SetStatus(Status.MovingUp.GetDescription());
            }
            else if (elevator.CurrentFloor > targetDestination)
            {
                elevator.SetCurrentFloor(elevator.CurrentFloor - 1);
                elevator.SetDirection(Direction.Down);
                elevator.SetStatus(Status.MovingDown.GetDescription());
            }
            else // Elevator Arrived at the target destination
            {
                elevator.Destinations.Dequeue();

                elevator.RemoveStops(targetDestination);
                elevator.SetStatus(Status.DoorsOpen.GetDescription());

                RemoveElevatorRequests(elevator);
                
                if (elevator.Destinations.Count == 0)
                {
                    elevator.SetDirection(Direction.Idle);
                    elevator.SetStatus(Status.Idle.GetDescription());

                    // Assign Elevator to Request that are not yet accommodated
                   
                    AssignElevatorToUnAccommodatedRequest(elevator);
                }
                else if (elevator.CurrentFloor == InitialFloorNumber || elevator.CurrentFloor == NumberOfFloors)
                {
                    AssignElevatorToUnAccommodatedRequest(elevator);
                }
                else
                {
                    var nextDestination = elevator.Destinations.Peek();

                    if (elevator.CurrentFloor > nextDestination)
                    {
                        elevator.SetDirection(Direction.Down);
                    }
                    else if (elevator.CurrentFloor < nextDestination)
                    {
                        elevator.SetDirection(Direction.Up);
                    }
                    else
                    {
                        while (nextDestination == elevator.CurrentFloor)
                        {
                            elevator.RemoveStops(nextDestination);
                            nextDestination = elevator.Destinations.Peek();
                        }

                        var direction = elevator.CurrentFloor > nextDestination ? Direction.Down : Direction.Up;
                        elevator.SetDirection(direction);
                    }
                }

            }

            await Task.Delay(100);
        }
        #endregion

        #region Assign Elevator To UnAccommodated Request
        private void AssignElevatorToUnAccommodatedRequest(Elevator elevator)
        {
            if (elevator.Direction == Direction.Idle)
            {
                var requestNeedsAccommodation = _elevatorRequests
                .Where(x => !x.IsAccommodated)
                .OrderBy(x => Math.Abs(elevator.CurrentFloor - x.ElevatorRequest.CurrentFloor))
                .FirstOrDefault();

                if (requestNeedsAccommodation is not null)
                {
                    var nearestElevatorRequestDirection = requestNeedsAccommodation.ElevatorRequest.Direction;

                    var requestsToAccommodate = nearestElevatorRequestDirection == Direction.Up
                        ? _elevatorRequests.Where(x => x.ElevatorRequest.Direction == Direction.Up
                                                    && x.ElevatorRequest.CurrentFloor >= elevator.CurrentFloor
                                                    && !x.IsAccommodated).ToList()
                        : _elevatorRequests.Where(x => x.ElevatorRequest.Direction == Direction.Down
                                                    && x.ElevatorRequest.CurrentFloor <= elevator.CurrentFloor
                                                    && !x.IsAccommodated).ToList();

                    var elevatorRequest = _elevatorRequests.ToList();
                    var elevatorsToRemove = requestsToAccommodate.Select(x => x.ElevatorRequest).ToList();

                    elevatorRequest.RemoveAll(x => elevatorsToRemove.Select(x => x.Id).Contains(x.ElevatorRequest.Id));
                    _elevatorRequests = new ConcurrentBag<ELevatorRequestStatus>(elevatorRequest);

                    elevator.SetDirection(nearestElevatorRequestDirection);
                    foreach (var requestToAccommodate in requestsToAccommodate)
                    {
                        var request = requestToAccommodate.ElevatorRequest;
                        var isAccommodated = elevator.AddStops(request, InitialFloorNumber, NumberOfFloors);

                        var elevatorRequestStatus = new ELevatorRequestStatus
                        {
                            ElevatorRequest = request,
                            IsAccommodated = isAccommodated,
                            ElevatorId = isAccommodated ? elevator.Id : 0
                        };

                        _elevatorRequests.Add(elevatorRequestStatus);
                    }

                    elevator.SetDestinations();
                }
            }

            else
            {
                var direction = elevator.CurrentFloor == InitialFloorNumber ? Direction.Up : Direction.Down;
                var requestsToAccommodate = direction == Direction.Up
                        ? _elevatorRequests.Where(x => x.ElevatorRequest.Direction == Direction.Up
                                                    && x.ElevatorRequest.CurrentFloor >= elevator.CurrentFloor
                                                    && !x.IsAccommodated).ToList()
                        : _elevatorRequests.Where(x => x.ElevatorRequest.Direction == Direction.Down
                                                    && x.ElevatorRequest.CurrentFloor <= elevator.CurrentFloor
                                                    && !x.IsAccommodated).ToList();

                var elevatorRequest = _elevatorRequests.ToList();
                var elevatorsToRemove = requestsToAccommodate.Select(x => x.ElevatorRequest).ToList();

                elevatorRequest.RemoveAll(x => elevatorsToRemove.Select(x => x.Id).Contains(x.ElevatorRequest.Id));
                _elevatorRequests = new ConcurrentBag<ELevatorRequestStatus>(elevatorRequest);

                elevator.SetDirection(direction);
                foreach (var requestToAccommodate in requestsToAccommodate)
                {
                    var request = requestToAccommodate.ElevatorRequest;
                    var isAccommodated = elevator.AddStops(request, InitialFloorNumber, NumberOfFloors);

                    var elevatorRequestStatus = new ELevatorRequestStatus
                    {
                        ElevatorRequest = request,
                        IsAccommodated = isAccommodated,
                        ElevatorId = isAccommodated ? elevator.Id : 0
                    };

                    _elevatorRequests.Add(elevatorRequestStatus);
                }

                elevator.SetDestinations();
            }

            
        }
        #endregion

        #region Get Best Elevator
        private Elevator? GetBestElevator(ElevatorRequest request)
        {
            var idleElevatorsOntheSameFloorOfRequest = _elevators
                .Where(x => x.Direction == Direction.Idle && x.CurrentFloor == request.CurrentFloor)
                .ToList();

            if (idleElevatorsOntheSameFloorOfRequest.Any())
            {
                return idleElevatorsOntheSameFloorOfRequest.First();
            }

            var sameDirectionElevators = request.Direction == Direction.Up
            ? _elevators.Where(x => x.Direction == Direction.Up
                    && x.CurrentFloor <= request.CurrentFloor
                    && x.CurrentFloor < request.DestinationFloor)
              .ToList()
            : _elevators.Where(x => x.Direction == Direction.Down
                    && x.CurrentFloor >= request.CurrentFloor
                    && x.CurrentFloor > request.DestinationFloor)
              .ToList();

            if (sameDirectionElevators.Any())
            {
                return sameDirectionElevators
                    .OrderBy(x => Math.Abs(x.CurrentFloor - request.CurrentFloor))
                    .FirstOrDefault();
            }

            var idleElevators = _elevators.Where(x => x.Direction == Direction.Idle);

            if (idleElevators is not null && idleElevators.Any())
            {
                return idleElevators
                    .OrderBy(x => Math.Abs(x.CurrentFloor - request.CurrentFloor))
                    .FirstOrDefault();
            }

            return _elevators
                .OrderBy(e => e.Destinations.Count)
                .ThenBy(e => Math.Abs(e.CurrentFloor - request.CurrentFloor))
                .FirstOrDefault();
        }
        #endregion

        #region Remove Elevator Requests
        private void RemoveElevatorRequests(Elevator elevator)
        {
            var requests = GetElevatorRequestsToRemove(elevator);
            var elevatorRequest = _elevatorRequests.ToList();

            elevatorRequest.RemoveAll(x => requests.Select(x => x.Id).Contains(x.ElevatorRequest.Id));

            _elevatorRequests = new ConcurrentBag<ELevatorRequestStatus>(elevatorRequest);
        }
        #endregion

        private List<ElevatorRequest> GetElevatorRequestsToRemove(Elevator elevator)
        {
            var query = _elevatorRequests
                .Where(x => x.ElevatorId == elevator.Id) // x.IsAccommodated && 
                .Select(x => x.ElevatorRequest)
                .Where(x => x.CurrentFloor == elevator.CurrentFloor && elevator.Destinations.ToList().Contains(x.DestinationFloor)) // && x.Direction == elevator.Direction
                .AsQueryable();
            
            if (elevator.Destinations.Count > 0)
            {
                var nextDestination = elevator.Destinations.Peek();
                Direction? nextDirection = null;

                if (nextDestination > elevator.CurrentFloor)
                {
                    nextDirection = Direction.Up;
                }
                else if (nextDestination < elevator.CurrentFloor)
                {
                    nextDirection = Direction.Down;
                }
                else
                {
                    return new List<ElevatorRequest>();
                }

                return query.Where(x => x.Direction == nextDirection).ToList();
            }

            return query.ToList();
        }
        
        public IEnumerable<Elevator> GetElevators()
        {
            return _elevators.ToList();
        }

        public IEnumerable<ELevatorRequestStatus> GetElevatorRequests()
        {
            return _elevatorRequests.ToList();
        }

        /// <summary>
        /// For UI use only.
        /// </summary>
        /// <returns></returns>
        public async Task SendUpdateToClient()
        {
            var elevatorUpdate = GetElevatorUpdates();
            await _hubContext.Clients.All.SendAsync("ElevatorStatusUpdated", elevatorUpdate);
        }

        private ElevatorStatusUpdate GetElevatorUpdates()
        {
            var elevators = GetElevators();
            var requests = GetElevatorRequests();
            var passengers = _elevatorSystemStatusUpdateService.GetElevatorPassenger();

            var pendingRequest = requests
                    .Select(x => x)
                    .GroupBy(i => i.ElevatorRequest.CurrentFloor)
                    .Select(x => new PendingElevatorRequest
                    {
                        Floor = x.Key,
                        Destinations = x.Select(i => new RequestDestination
                        {
                            Destination = i.ElevatorRequest.DestinationFloor,
                            ElevatorId = i.ElevatorId,
                        })
                        .Distinct()
                        .OrderBy(x => x.ElevatorId)
                        .ThenBy(x => x.Destination)
                        .ToList()
                    })
                    .ToList();

            return new ElevatorStatusUpdate
            {
                Requests = pendingRequest,
                Elevators = elevators.ToList(),
                Passengers = passengers.ToList(),
            };
        }

    }
}


