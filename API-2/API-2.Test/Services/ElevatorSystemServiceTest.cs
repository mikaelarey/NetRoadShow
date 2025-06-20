
using System.Collections.Concurrent;
using System.Reflection;
using API_2.Dto;
using API_2.Enums;
using API_2.Hubs;
using API_2.Models;
using API_2.Services;
using FakeItEasy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Extensions; // Needed for accessing private fields if mocking internal state


namespace API_2.Test.Services
{
    public class ElevatorSystemServiceTest
    {
        private readonly ElevatorSystemService _elevatorSystemService;
        private readonly IHubContext<ElevatorHub> _hubContext;
        private readonly ElevatorSystemStatusUpdateService _elevatorSystemStatusUpdateService;

        public ElevatorSystemServiceTest()
        {
            _hubContext = A.Fake<IHubContext<ElevatorHub>>();
            _elevatorSystemStatusUpdateService = A.Fake<ElevatorSystemStatusUpdateService>();
            _elevatorSystemService = new ElevatorSystemService(_elevatorSystemStatusUpdateService, _hubContext);
        }

        #region InitializeElevators
        [Fact]
        public void InitializeElevators_Positive_ShouldCreateCorrectNumberOfElevators()
        {
            #region Arrange
            const int numberOfFloors = 10;
            const int numberOfElevators = 4;
            const int intialFloorNumber = 1;
            #endregion

            #region Act
            _elevatorSystemService.InitializeElevators(intialFloorNumber, numberOfFloors, numberOfElevators);
            #endregion

            #region Assert
            var elevators = GetPrivateElevatorsField();
            Assert.Equal(numberOfElevators, elevators.Count);
            Assert.Contains(elevators, e => e.Id == 1);
            Assert.Contains(elevators, e => e.Id == 2);
            Assert.Contains(elevators, e => e.Id == 3);
            Assert.Contains(elevators, e => e.Id == 4);
            Assert.All(elevators, e => Assert.Equal(intialFloorNumber, e.CurrentFloor));
            Assert.All(elevators, e => Assert.Equal(Direction.Idle, e.Direction));
            Assert.All(elevators, e => Assert.Equal(Status.Idle.GetDisplayName(), e.Status));
            #endregion

        }
        #endregion


        #region RequestElevator
        [Fact]
        public void RequestElevator_ShouldAssignRequestToBestElevator_WhenBestElevatorAvailable()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            A.CallTo(() => mockElevator1.CurrentFloor).Returns(1);
            A.CallTo(() => mockElevator1.Direction).Returns(Direction.Idle);

            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));
            A.CallTo(() => mockElevator2.CurrentFloor).Returns(5);
            A.CallTo(() => mockElevator2.Direction).Returns(Direction.Up);

            List<Elevator> elevators = new List<Elevator> { mockElevator1, mockElevator2 };
            SetPrivateElevatorsField(elevators);

            var request = new ElevatorRequest(2, 7);
            #endregion

            #region Act
            _elevatorSystemService.RequestElevator(request);
            #endregion

            #region Assert
            var elevatorRequests = GetPrivateElevatorRequestsField();
            Assert.Contains(elevatorRequests, r => r.ElevatorRequest == request && r.IsAccommodated);
            #endregion
        }

        [Fact]
        public void RequestElevator_ShouldTryIdleElevatorIfBestFailsToAddStops()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            
            A.CallTo(() => mockElevator1.CurrentFloor).Returns(2);
            A.CallTo(() => mockElevator1.Direction).Returns(Direction.Up);

            // Idle elevator
            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));
            
            A.CallTo(() => mockElevator2.CurrentFloor).Returns(1);
            A.CallTo(() => mockElevator2.Direction).Returns(Direction.Idle);

            List<Elevator> elevators = new List<Elevator> { mockElevator1, mockElevator2 };

            var request = new ElevatorRequest(2, 7);
            SetPrivateElevatorsField(elevators);
            #endregion

            #region Act
            _elevatorSystemService.RequestElevator(request);
            #endregion

            #region Assert
            var elevatorRequests = GetPrivateElevatorRequestsField();
            Assert.Contains(elevatorRequests, r => r.ElevatorRequest == request && r.IsAccommodated);
            #endregion
        }

        [Fact]
        public void RequestElevator_ShouldNotAccommodateIfNoElevatorCanAddStops()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            A.CallTo(() => mockElevator1.CurrentFloor).Returns(2);
            A.CallTo(() => mockElevator1.Direction).Returns(Direction.Up);

            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));
            A.CallTo(() => mockElevator2.CurrentFloor).Returns(5);
            A.CallTo(() => mockElevator2.Direction).Returns(Direction.Down);

            List<Elevator> elevators = new List<Elevator> { mockElevator1, mockElevator2 };
            SetPrivateElevatorsField(elevators);

            var request = new ElevatorRequest(1, 7);
            #endregion

            #region Act
            _elevatorSystemService.RequestElevator(request);
            #endregion

            #region Assert
            var elevatorRequests = GetPrivateElevatorRequestsField();
            Assert.Contains(elevatorRequests, r => r.ElevatorRequest == request && !r.IsAccommodated);
            #endregion
        }

        #endregion

        #region StepAllElevatorAsync
        [Fact]
        public async Task StepAllElevatorAsync_ShouldCallStepElevatorAsyncForEachElevator()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));

            mockElevator1.Destinations.Enqueue(10);
            mockElevator2.Destinations.Enqueue(5);

            List<Elevator> elevators = new List<Elevator> { mockElevator1, mockElevator2 };
            SetPrivateElevatorsField(elevators);
            #endregion

            #region Act
            await _elevatorSystemService.StepAllElevatorAsync();
            #endregion

            #region Assert
            A.CallTo(() => mockElevator1.SetCurrentFloor(A<int>._)).MustHaveHappened();
            A.CallTo(() => mockElevator2.SetCurrentFloor(A<int>._)).MustHaveHappened();
            A.CallTo(() => mockElevator1.SetStatus(A<string>._)).MustHaveHappened();
            A.CallTo(() => mockElevator2.SetStatus(A<string>._)).MustHaveHappened();
            #endregion
        }
        #endregion

        
        #region StepElevatorAsync
        [Fact]
        public async Task StepElevatorAsync_ElevatorMovesUp_WhenCurrentFloorLessThanTarget()
        {
            #region Arrange
            var mockElevator = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            mockElevator.Destinations.Enqueue(5);

            A.CallTo(() => mockElevator.CurrentFloor).Returns(1);
            SetPrivateElevatorsField(new[] { mockElevator });
            #endregion

            #region Act
            await _elevatorSystemService.StepAllElevatorAsync();
            #endregion

            #region Assert
            A.CallTo(() => mockElevator.SetCurrentFloor(2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mockElevator.SetDirection(Direction.Up)).MustHaveHappenedOnceExactly();
            #endregion
        }

        [Fact]
        public async Task StepElevatorAsync_ElevatorMovesDown_WhenCurrentFloorGreaterThanTarget()
        {
            #region Arrange
            var mockElevator = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            mockElevator.Destinations.Enqueue(1);

            A.CallTo(() => mockElevator.CurrentFloor).Returns(5);
            SetPrivateElevatorsField(new[] { mockElevator });
            #endregion

            #region Act
            await _elevatorSystemService.StepAllElevatorAsync();
            #endregion

            #region Assert
            A.CallTo(() => mockElevator.SetCurrentFloor(4)).MustHaveHappenedOnceExactly();
            A.CallTo(() => mockElevator.SetDirection(Direction.Down)).MustHaveHappenedOnceExactly();
            #endregion
        }

        [Fact]
        public async Task StepElevatorAsync_ElevatorArrivesAtDestination_SetsLoadingStatusAndDequeues()
        {
            #region Arrange
            var mockElevator = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            mockElevator.Destinations.Enqueue(5);
            mockElevator.Destinations.Enqueue(10);

            A.CallTo(() => mockElevator.CurrentFloor).Returns(5);
            A.CallTo(() => mockElevator.Direction).Returns(Direction.Up);

            var initialQueueCount = mockElevator.Destinations.Count;
            SetPrivateElevatorsField(new[] { mockElevator });
            #endregion

            #region Act
            await _elevatorSystemService.StepAllElevatorAsync();
            #endregion

            #region Assert
            A.CallTo(() => mockElevator.SetDirection(Direction.Up)).MustHaveHappenedOnceExactly();
            #endregion
        }

        [Fact]
        public async Task StepElevatorAsync_ElevatorArrivesAtLastDestination_BecomesIdleAndAssignsUnaccommodated()
        {
            #region Arrange
            var mockElevator = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            mockElevator.Destinations.Enqueue(5);
            A.CallTo(() => mockElevator.CurrentFloor).Returns(5);
            A.CallTo(() => mockElevator.Direction).Returns(Direction.Idle);

            var unaccommodatedRequest = new ElevatorRequest(6, 8); // (2, 8)
            SetPrivateElevatorRequestsField(new[]
            {
                new ELevatorRequestStatus { ElevatorRequest = unaccommodatedRequest, IsAccommodated = false }
            });

            SetPrivateElevatorsField(new[] { mockElevator });
            #endregion

            #region Act
            await _elevatorSystemService.StepAllElevatorAsync();
            #endregion

            #region Assert
            var updatedRequests = GetPrivateElevatorRequestsField();
            Assert.Contains(updatedRequests, r => r.ElevatorRequest == unaccommodatedRequest && r.IsAccommodated);
            #endregion
        }
        #endregion

        #region GetBestElevator
        [Theory]
        [InlineData(2, 10)]
        [InlineData(10, 1)]
        public void GetBestElevator_ShouldReturnSameDirectionElevator_WhenAvailableAndCloser(int currentFloor, int destinationFloor)
        {
            #region Arrange
            var elevator1 = GetMockedElevator(1, 2, Direction.Up);
            var elevator2 = GetMockedElevator(2, 10, Direction.Down);
            var elevator3 = GetMockedElevator(3, 5, Direction.Idle);
            var elevator4 = GetMockedElevator(4, 7, Direction.Down);

            var elevators = new List<Elevator> { elevator1, elevator2, elevator3, elevator4 };
            var request = new ElevatorRequest(currentFloor, destinationFloor);
            var expectedElevator = elevators.First(e => e.CurrentFloor == currentFloor);

            SetPrivateElevatorsField(elevators);
            #endregion

            #region Act
            var result = GetBestElevator(request);
            #endregion

            #region Assert
            Assert.Equal(expectedElevator, result);
            #endregion
        }

        [Fact]
        public void GetBestElevator_ShouldReturnIdleElevator_WhenNoSameDirectionElevator()
        {
            #region Arrange
            var elevator1 = GetMockedElevator(1, 2, Direction.Up);
            var elevator2 = GetMockedElevator(2, 10, Direction.Down);
            var elevator3 = GetMockedElevator(3, 5, Direction.Idle);
            var elevator4 = GetMockedElevator(4, 7, Direction.Down);

            var elevators = new List<Elevator> { elevator1, elevator2, elevator3, elevator4 };
            var request = new ElevatorRequest(1, 10);
            var expectedElevator = elevators.First(e => e.CurrentFloor == 5);

            SetPrivateElevatorsField(elevators);
            #endregion

            #region Act
            var result = GetBestElevator(request);
            #endregion

            #region Assert
            Assert.Equal(elevator3, result);
            #endregion
        }

        [Fact]
        public void GetBestElevator_ShouldReturnElevatorWithLeastDestinationsAndClosest_WhenNoSameDirectionOrIdle()
        {
            #region Arrange
            var elevator1 = GetMockedElevator(1, 2, Direction.Up, new List<int> { 1, 2, 3, 4 });
            var elevator2 = GetMockedElevator(2, 10, Direction.Down, new List<int> { 1, 2, 3 });
            var elevator3 = GetMockedElevator(3, 5, Direction.Down, new List<int> { 1, 2, 3, 4 });
            var elevator4 = GetMockedElevator(4, 7, Direction.Down, new List<int> { 1, 2, 3, 4 });

            var elevators = new List<Elevator> { elevator1, elevator2, elevator3, elevator4 };
            var request = new ElevatorRequest(1, 10);
            var expectedElevator = elevators.First(e => e.CurrentFloor == 2);

            SetPrivateElevatorsField(elevators);
            #endregion

            #region Act
            var result = GetBestElevator(request);
            #endregion

            #region Assert
            Assert.Equal(elevator2, result);
            #endregion
        }

        private Elevator GetMockedElevator(int id, int currentFloor, Direction direction, List<int>? destinations = null) 
        {
            var elevator = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(id)));
            A.CallTo(() => elevator.CurrentFloor).Returns(currentFloor);
            A.CallTo(() => elevator.Direction).Returns(direction);

            destinations = destinations ?? new List<int>();
            elevator.Destinations = new Queue<int>(destinations);
           
            return elevator;
        }
        #endregion

        #region Arrange
        private ConcurrentBag<Elevator> GetPrivateElevatorsField()
        {
            // Use reflection to access the private _elevators field for inspection
            var fieldInfo = typeof(ElevatorSystemService)
                                .GetField("_elevators", BindingFlags.NonPublic | BindingFlags.Instance);
            return (ConcurrentBag<Elevator>)fieldInfo.GetValue(_elevatorSystemService);
        }

        private ConcurrentBag<ELevatorRequestStatus> GetPrivateElevatorRequestsField()
        {
            // Use reflection to access the private _elevatorRequests field for inspection
            var propertyInfo = typeof(ElevatorSystemService)
                                .GetProperty("_elevatorRequests", BindingFlags.NonPublic | BindingFlags.Instance);
            return (ConcurrentBag<ELevatorRequestStatus>)propertyInfo.GetValue(_elevatorSystemService);
        }

        // Helper to set private _elevators field for specific test scenarios
        private void SetPrivateElevatorsField(IEnumerable<Elevator> elevators)
        {
            var fieldInfo = typeof(ElevatorSystemService)
                                .GetField("_elevators", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo?.SetValue(_elevatorSystemService, new ConcurrentBag<Elevator>(elevators));
        }

        // Helper to set private _elevatorRequests field for specific test scenarios
        private void SetPrivateElevatorRequestsField(IEnumerable<ELevatorRequestStatus> requests)
        {
            var propertyInfo = typeof(ElevatorSystemService)
                                .GetProperty("_elevatorRequests", BindingFlags.NonPublic | BindingFlags.Instance);
            propertyInfo.SetValue(_elevatorSystemService, new ConcurrentBag<ELevatorRequestStatus>(requests));
        }

        // Helper to access private GetBestElevator method
        private Elevator? GetBestElevator(ElevatorRequest request)
        {
            var method = typeof(ElevatorSystemService)
                .GetMethod("GetBestElevator", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Elevator?)method.Invoke(_elevatorSystemService, new object[] { request });
        }
        #endregion

    }
}
