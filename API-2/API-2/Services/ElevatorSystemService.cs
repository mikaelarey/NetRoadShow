using System.Collections.Concurrent;
using API_2.Dto;
using API_2.Enums;
using API_2.Extensions;
using API_2.Hubs;
using API_2.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Extensions;

namespace API_2.Services
{
    public class ElevatorSystemService
    {
        private readonly IHubContext<ElevatorHub> _hubContext;
        private ConcurrentBag<Elevator> _elevators;
        private ConcurrentBag<ELevatorRequestStatus> _elevatorRequests { get; set; } = new();
        private const int INITIAL_ELEVATOR_ID = 1;

        private readonly TaskCompletionSource _startSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task StartSignal => _startSignal.Task;


        public ElevatorSystemService(IHubContext<ElevatorHub> hubContext)
        {
            _hubContext = hubContext;
            _elevators = new ConcurrentBag<Elevator>();
        }

        #region TriggerToStartSignal
        public void TriggerToStartSignal()
        {
            _startSignal.TrySetResult();
        }
        #endregion

        #region
        public void InitializeElevators(int count)
        {
            var elevators = Enumerable
                .Range(INITIAL_ELEVATOR_ID, count)
                .Select(id => new Elevator(id));

            _elevators = new ConcurrentBag<Elevator>(elevators);
        }
        #endregion

        #region
        public async Task RequestElevator(ElevatorRequest request)
        {
            var elevator = GetBestElevator(request);

            if (elevator is not null)
            {
                var isAddedToElevator = elevator.AddStops(request);

                if (!isAddedToElevator)
                {
                    elevator = GetIdleElevator(request);

                    if (elevator is not null)
                    {
                        isAddedToElevator = elevator.AddStops(request);
                    }
                }

                var requestStatus = new ELevatorRequestStatus
                {
                    ElevatorRequest = request,
                    IsAccommodated = isAddedToElevator
                };

                _elevatorRequests.Add(requestStatus);

                if (isAddedToElevator)
                {
                    SetDirectionForIdleElevator(elevator, request);

                    elevator.SetDestinations();
                    elevator.SetPassengerDestinations();
                }

                return;
            }

            Console.WriteLine("No available elevators.");
        }
        #endregion

        #region
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
        public ElevatorStatusUpdate GetElevatorUpdates()
        {
            var pendingRequest = _elevatorRequests
                    .Select(x => x.ElevatorRequest)
                    .GroupBy(i => i.CurrentFloor)
                    .Select(x => new PendingElevatorRequest
                    {
                        Floor = x.Key,
                        Destinations = x.Select(i => i.DestinationFloor).Distinct().OrderBy(x => x).ToList()
                    })
                    .ToList();

            return new ElevatorStatusUpdate
            {
                Requests = pendingRequest,
                Elevators = _elevators.ToList()
            };
        }
        #endregion

        #region
        private async Task StepElevatorAsync(Elevator elevator)
        {
            if (elevator.Destinations.Count == 0)
            {
                elevator.SetDirection(Direction.Idle);
                elevator.SetStatus(Status.Idle.GetDisplayName());
                return;
            }

            var targetDestination = elevator.Destinations.Peek();


            if (elevator.CurrentFloor < targetDestination)
            {
                elevator.SetCurrentFloor(elevator.CurrentFloor + 1);
                elevator.SetDirection(Direction.Up);
                elevator.SetStatus(Status.MovingUp.GetDisplayName());
            }
            else if (elevator.CurrentFloor > targetDestination)
            {
                elevator.SetCurrentFloor(elevator.CurrentFloor - 1);
                elevator.SetDirection(Direction.Down);
                elevator.SetStatus(Status.MovingDown.GetDisplayName());
            }
            else // elevator.CurrentFloor == targetDestination =>  Elevator Arrived at the target destination
            {
                elevator.Destinations.Dequeue();
                elevator.RemoveStops(targetDestination);
                elevator.SetStatus(Status.LoadingUnloading.GetDisplayName());

                elevator.SetPassengerDestinations();

                RemoveElevatorRequests(elevator);

                if (elevator.Destinations.Count == 0)
                {
                    // TODO: Check this logic
                    elevator.SetDirection(Direction.Idle);
                    elevator.SetStatus(Status.Idle.GetDisplayName());

                    // Assign Elevator to Request that are not yet accommodated
                    await AssignElevatorToUnAccommodatedRequest(elevator);
                }
                else
                {
                    var nextDestination = elevator.Destinations.Peek();

                    if (elevator.CurrentFloor > nextDestination)
                    {
                        elevator.SetDirection(Direction.Down);
                    }
                    else
                    {
                        elevator.SetDirection(Direction.Up);
                    }
                }
            }
        }

        #endregion

        #region
        private async Task AssignElevatorToUnAccommodatedRequest(Elevator elevator)
        {
            var requestNeedsAccommodation = _elevatorRequests
                .Where(x => !x.IsAccommodated)
                .OrderBy(x => Math.Abs(elevator.CurrentFloor - x.ElevatorRequest.CurrentFloor))
                .FirstOrDefault();


            if (requestNeedsAccommodation is not null)
            {
                var nearestElevatorRequestDirection = requestNeedsAccommodation.ElevatorRequest.Direction;

                var requestsToAccommodate = nearestElevatorRequestDirection == Direction.Up
                    ? _elevatorRequests.Where(x => x.ElevatorRequest.Direction == Direction.Up && x.ElevatorRequest.CurrentFloor >= elevator.CurrentFloor).ToList()
                    : _elevatorRequests.Where(x => x.ElevatorRequest.Direction == Direction.Down && x.ElevatorRequest.CurrentFloor <= elevator.CurrentFloor).ToList();


                var elevatorRequest = _elevatorRequests.ToList();
                var elevatorsToRemove = requestsToAccommodate.Select(x => x.ElevatorRequest).ToList();

                elevatorRequest.RemoveAll(x => elevatorsToRemove.Select(x => x.Id).Contains(x.ElevatorRequest.Id));

                _elevatorRequests = new ConcurrentBag<ELevatorRequestStatus>(elevatorRequest);

                foreach (var requestToAccommodate in requestsToAccommodate)
                {

                    var request = requestToAccommodate.ElevatorRequest;

                    SetDirectionForIdleElevator(elevator, request);

                    var isAccommodated = elevator.AddStops(request);

                    var elevatorRequestStatus = new ELevatorRequestStatus
                    {
                        ElevatorRequest = request,
                        IsAccommodated = isAccommodated,
                    };

                    _elevatorRequests.Add(elevatorRequestStatus);
                }

                elevator.SetDestinations();
                elevator.SetPassengerDestinations();
                elevator.SetStatus(nearestElevatorRequestDirection.GetDisplayName());
            }
        }

        #endregion

        #region
        private Elevator? GetBestElevator(ElevatorRequest request)
        {
            List<Elevator> sameDirectionElevators = new();

            if (request.Direction == Direction.Up)
            {
                sameDirectionElevators = _elevators
                    .Where(x => x.Direction == Direction.Up
                             && x.CurrentFloor <= request.CurrentFloor)
                    .ToList();
            }
            else if (request.Direction == Direction.Down)
            {
                sameDirectionElevators = _elevators
                    .Where(x => x.Direction == Direction.Down
                             && x.CurrentFloor >= request.CurrentFloor)
                    .ToList();
            }

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

        #region
        private Elevator? GetIdleElevator(ElevatorRequest request)
        {
            var idleElevators = _elevators.Where(x => x.Direction == Direction.Idle);

            if (idleElevators is not null && idleElevators.Any())
            {
                return idleElevators
                    .OrderBy(x => Math.Abs(x.CurrentFloor - request.CurrentFloor))
                    .FirstOrDefault();
            }

            return null;
        }

        #endregion

        #region
        public async Task SendUpdateToClientAsync()
        {
            var elevatorUpdate = GetElevatorUpdates();
            await _hubContext.Clients.All.SendAsync("ElevatorStatusUpdated", elevatorUpdate);
        }

        #endregion

        #region
        private void RemoveElevatorRequests(Elevator elevator)
        {
            var requests = _elevatorRequests
                .Where(x => x.IsAccommodated)
                .Select(x => x.ElevatorRequest)
                .Where(x => x.CurrentFloor == elevator.CurrentFloor && x.Direction == elevator.Direction)
                .ToList();

            var elevatorRequest = _elevatorRequests.ToList();
            elevatorRequest.RemoveAll(x => requests.Select(x => x.Id).Contains(x.ElevatorRequest.Id));

            _elevatorRequests = new ConcurrentBag<ELevatorRequestStatus>(elevatorRequest);
        }

        #endregion

        #region
        private void SetDirectionForIdleElevator(Elevator elevator, ElevatorRequest request)
        {
            if (elevator.Direction == Direction.Idle)
            {
                if (request.CurrentFloor > elevator.CurrentFloor)
                {
                    elevator.SetDirection(Direction.Up);
                }
                else if (request.CurrentFloor < elevator.CurrentFloor)
                {
                    elevator.SetDirection(Direction.Down);
                }
                else
                {
                    if (request.DestinationFloor < elevator.CurrentFloor)
                    {
                        elevator.SetDirection(Direction.Down);
                    }
                    else
                    {
                        elevator.SetDirection(Direction.Up);
                    }
                }
            }
        }
        #endregion

    }
}


