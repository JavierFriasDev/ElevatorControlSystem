using ElevatorControlSystem.Constants;
using ElevatorControlSystem.Enums;
using ElevatorControlSystem.Models;
using ElevatorControlSystem.Services;
using Xunit;

namespace ElevatorControlSystem.Tests;

public class DispatcherTests
{
    public DispatcherTests()
    {
        // Set to 0 for fast test execution (if needed for any async tests)
        BuildingConstants.FloorTravelTimeSeconds = 0;
        BuildingConstants.LoadingTimeSeconds = 0;
    }

    [Fact]
    public void SelectElevator_PrefersIdleElevator()
    {
        // Arrange
        var elevators = new List<Elevator>
        {
            new Elevator(1, 1), // Idle at floor 1
            new Elevator(2, 10) // Idle at floor 10
        };

        var dispatcher = new SimpleElevatorDispatcher();
        var request = new ElevatorRequest(5, Direction.Up);

        // Act
        var selected = dispatcher.SelectElevator(elevators, request);

        // Assert
        Assert.NotNull(selected);
        Assert.Equal(1, selected.Id); // Elevator 1 is closer to floor 5
    }

    [Fact]
    public void SelectElevator_ReturnsNull_WhenNoElevatorsAvailable()
    {
        // Arrange
        var elevators = new List<Elevator>();
        var dispatcher = new SimpleElevatorDispatcher();
        var request = new ElevatorRequest(5, Direction.Up);

        // Act
        var selected = dispatcher.SelectElevator(elevators, request);

        // Assert
        Assert.Null(selected);
    }

    [Fact]
    public void SelectElevator_SelectsClosestIdleElevator()
    {
        // Arrange
        var elevators = new List<Elevator>
        {
            new Elevator(1, 1),
            new Elevator(2, 8),
            new Elevator(3, 3)
        };

        var dispatcher = new SimpleElevatorDispatcher();
        var request = new ElevatorRequest(4, Direction.Up);

        // Act
        var selected = dispatcher.SelectElevator(elevators, request);

        // Assert
        Assert.NotNull(selected);
        Assert.Equal(3, selected.Id); // Elevator 3 at floor 3 is closest to floor 4
    }
}
