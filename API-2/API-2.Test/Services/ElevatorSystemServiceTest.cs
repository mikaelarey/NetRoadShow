
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
        private readonly IHubContext<ElevatorHub> _fakeHubContext;
        private readonly IHubClients _fakeClients;
        private readonly IClientProxy _fakeClientProxy;
        private readonly ElevatorSystemService _elevatorSystemService;

        public ElevatorSystemServiceTest()
        {
            _fakeHubContext = A.Fake<IHubContext<ElevatorHub>>();
            _fakeClients = A.Fake<IHubClients>();
            _fakeClientProxy = A.Fake<IClientProxy>();

            A.CallTo(() => _fakeHubContext.Clients).Returns(_fakeClients);
            A.CallTo(() => _fakeClients.All).Returns(_fakeClientProxy);

            _elevatorSystemService = new ElevatorSystemService(_fakeHubContext);
        }

        


        #region InitializeElevators
        [Fact]
        public void InitializeElevators_Positive_ShouldCreateCorrectNumberOfElevators()
        {
            #region Arrange
            const int INITIAL_FLOOR = 1;
            const int NUMBER_OF_ELEVATOR = 4;
            #endregion

            #region Act
            _elevatorSystemService.InitializeElevators(NUMBER_OF_ELEVATOR);
            #endregion

            #region Assert
            var elevators = GetPrivateElevatorsField();
            Assert.Equal(NUMBER_OF_ELEVATOR, elevators.Count);
            Assert.Contains(elevators, e => e.Id == 1);
            Assert.Contains(elevators, e => e.Id == 2);
            Assert.Contains(elevators, e => e.Id == 3);
            Assert.Contains(elevators, e => e.Id == 4);
            Assert.All(elevators, e => Assert.Equal(INITIAL_FLOOR, e.CurrentFloor));
            Assert.All(elevators, e => Assert.Equal(Direction.Idle, e.Direction));
            Assert.All(elevators, e => Assert.Equal(Status.Idle.GetDisplayName(), e.Status));
            #endregion

        }
        #endregion

        #region TriggerToStartSignal
        [Fact]
        public void TriggerToStartSignal_Positive_ShouldSetStartSignalResult()
        {
            #region Act
            _elevatorSystemService.TriggerToStartSignal();
            #endregion

            #region Assert
            Assert.True(_elevatorSystemService.StartSignal.IsCompletedSuccessfully);
            #endregion
        }
        #endregion

        #region RequestElevator
        [Fact]
        public async Task RequestElevator_ShouldAssignRequestToBestElevator_WhenBestElevatorAvailable()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            A.CallTo(() => mockElevator1.Id).Returns(1);
            A.CallTo(() => mockElevator1.CurrentFloor).Returns(1);
            A.CallTo(() => mockElevator1.Direction).Returns(Direction.Idle);

            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));
            A.CallTo(() => mockElevator2.Id).Returns(2);
            A.CallTo(() => mockElevator2.CurrentFloor).Returns(5);
            A.CallTo(() => mockElevator2.Direction).Returns(Direction.Up);

            List<Elevator> elevators = new List<Elevator> { mockElevator1, mockElevator2 };
            SetPrivateElevatorsField(elevators);

            var request = new ElevatorRequest(2, 7);
            #endregion

            #region Act
            await _elevatorSystemService.RequestElevator(request);
            #endregion

            #region Assert
            var elevatorRequests = GetPrivateElevatorRequestsField();
            Assert.Contains(elevatorRequests, r => r.ElevatorRequest == request && r.IsAccommodated);
            #endregion
        }

        [Fact]
        public async Task RequestElevator_ShouldTryIdleElevatorIfBestFailsToAddStops()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            A.CallTo(() => mockElevator1.Id).Returns(1);
            A.CallTo(() => mockElevator1.CurrentFloor).Returns(2);
            A.CallTo(() => mockElevator1.Direction).Returns(Direction.Up);

            // Idle elevator
            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));
            A.CallTo(() => mockElevator2.Id).Returns(2);
            A.CallTo(() => mockElevator2.CurrentFloor).Returns(1);
            A.CallTo(() => mockElevator2.Direction).Returns(Direction.Idle);

            List<Elevator> elevators = new List<Elevator> { mockElevator1, mockElevator2 };

            var request = new ElevatorRequest(2, 7);
            SetPrivateElevatorsField(elevators);
            #endregion

            #region Act
            await _elevatorSystemService.RequestElevator(request);
            #endregion

            #region Assert
            var elevatorRequests = GetPrivateElevatorRequestsField();
            Assert.Contains(elevatorRequests, r => r.ElevatorRequest == request && r.IsAccommodated);
            #endregion
        }

        [Fact]
        public async Task RequestElevator_ShouldNotAccommodateIfNoElevatorCanAddStops()
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
            await _elevatorSystemService.RequestElevator(request);
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

        #region GetElevatorUpdates
        [Fact]
        public void GetElevatorUpdates_ShouldReturnCorrectStatusUpdate()
        {
            #region Arrange
            _elevatorSystemService.InitializeElevators(2);
            var elevators = GetPrivateElevatorsField().ToList(); // Get the actual created elevators

            elevators[0].SetCurrentFloor(5);
            elevators[0].SetDirection(Direction.Up);
            elevators[0].Destinations.Enqueue(10);
            elevators[0].Destinations.Enqueue(12);

            elevators[1].SetCurrentFloor(2);
            elevators[1].SetDirection(Direction.Idle);

            var request1 = new ElevatorRequest(3, 8);
            var request2 = new ElevatorRequest(3, 6);
            var request3 = new ElevatorRequest(1, 0);

            SetPrivateElevatorRequestsField(new[]
            {
                new ELevatorRequestStatus { ElevatorRequest = request1, IsAccommodated = false },
                new ELevatorRequestStatus { ElevatorRequest = request2, IsAccommodated = false },
                new ELevatorRequestStatus { ElevatorRequest = request3, IsAccommodated = true }
            });

            #endregion

            #region Act
            var update = _elevatorSystemService.GetElevatorUpdates();
            #endregion

            #region Assert
            Assert.NotNull(update);
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
            A.CallTo(() => mockElevator.SetStatus(Status.MovingUp.GetDisplayName())).MustHaveHappenedOnceExactly();
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
            A.CallTo(() => mockElevator.SetStatus(Status.MovingDown.GetDisplayName())).MustHaveHappenedOnceExactly();
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
            A.CallTo(() => mockElevator.Direction).Returns(Direction.Up);

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
        [Fact]
        public void GetBestElevator_ShouldReturnSameDirectionElevator_WhenAvailableAndCloser()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            A.CallTo(() => mockElevator1.CurrentFloor).Returns(2);
            A.CallTo(() => mockElevator1.Direction).Returns(Direction.Up);

            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));
            A.CallTo(() => mockElevator2.CurrentFloor).Returns(1);
            A.CallTo(() => mockElevator2.Direction).Returns(Direction.Up); // Same direction, but further

            var mockElevator3 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(3)));
            A.CallTo(() => mockElevator3.CurrentFloor).Returns(5);
            A.CallTo(() => mockElevator3.Direction).Returns(Direction.Idle);

            SetPrivateElevatorsField(new[] { mockElevator1, mockElevator2, mockElevator3 });

            var request = new ElevatorRequest(3, 7);
            #endregion

            #region Act
            var result = GetBestElevator(request);
            #endregion

            #region Assert
            Assert.Equal(mockElevator1, result);
            #endregion
        }

        [Fact]
        public void GetBestElevator_ShouldReturnIdleElevator_WhenNoSameDirectionElevator()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            A.CallTo(() => mockElevator1.CurrentFloor).Returns(2);
            A.CallTo(() => mockElevator1.Direction).Returns(Direction.Down); // Wrong direction

            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));
            A.CallTo(() => mockElevator2.CurrentFloor).Returns(5);
            A.CallTo(() => mockElevator2.Direction).Returns(Direction.Idle); // Idle and closer

            var mockElevator3 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(3)));
            A.CallTo(() => mockElevator3.CurrentFloor).Returns(10);
            A.CallTo(() => mockElevator3.Direction).Returns(Direction.Idle); // Idle but further

            SetPrivateElevatorsField(new[] { mockElevator1, mockElevator2, mockElevator3 });

            var request = new ElevatorRequest(3, 7);
            #endregion

            #region Act
            var result = GetBestElevator(request);
            #endregion

            #region Assert
            Assert.Equal(mockElevator2, result);
            #endregion
        }

        [Fact]
        public void GetBestElevator_ShouldReturnElevatorWithLeastDestinationsAndClosest_WhenNoSameDirectionOrIdle()
        {
            #region Arrange
            var mockElevator1 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(1)));
            mockElevator1.Destinations.Enqueue(5);
            mockElevator1.Destinations.Enqueue(10);

            A.CallTo(() => mockElevator1.CurrentFloor).Returns(1);
            A.CallTo(() => mockElevator1.Direction).Returns(Direction.Down);

            var mockElevator2 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(2)));
            mockElevator1.Destinations.Enqueue(15);
            A.CallTo(() => mockElevator2.CurrentFloor).Returns(10);
            A.CallTo(() => mockElevator2.Direction).Returns(Direction.Up);

            var mockElevator3 = A.Fake<Elevator>(options => options.WithArgumentsForConstructor(() => new Elevator(3)));
            mockElevator1.Destinations.Enqueue(2);

            A.CallTo(() => mockElevator3.CurrentFloor).Returns(7);
            A.CallTo(() => mockElevator3.Direction).Returns(Direction.Down);

            SetPrivateElevatorsField(new[] { mockElevator1, mockElevator2, mockElevator3 });

            var request = new ElevatorRequest(3, 7);
            #endregion

            #region Act
            var result = GetBestElevator(request);
            #endregion

            #region Assert
            Assert.Equal(mockElevator3, result);
            #endregion
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
