using ElevatorControlSystem.Constants;
using ElevatorControlSystem.Enums;
using ElevatorControlSystem.Models;
using Xunit;

namespace ElevatorControlSystem.Tests;

public class ElevatorTests
{
    // Store original values to restore after tests
    private readonly int _originalFloorTravelTime;
    private readonly int _originalLoadingTime;

    public ElevatorTests()
    {
        // Save original constants
        _originalFloorTravelTime = BuildingConstants.FloorTravelTimeSeconds;
        _originalLoadingTime = BuildingConstants.LoadingTimeSeconds;

        // Set to 0 for fast test execution
        BuildingConstants.FloorTravelTimeSeconds = 0;
        BuildingConstants.LoadingTimeSeconds = 0;
    }

    [Fact]
    public void Elevator_InitializesWithCorrectDefaults()
    {
        // Arrange & Act
        var elevator = new Elevator(1, startingFloor: 5);

        // Assert
        Assert.Equal(1, elevator.Id);
        Assert.Equal(5, elevator.CurrentFloor);
        Assert.Equal(Direction.Idle, elevator.CurrentDirection);
        Assert.Equal(ElevatorState.Idle, elevator.State);
    }

    [Fact]
    public void AddDestination_ThrowsException_ForInvalidFloor()
    {
        // Arrange
        var elevator = new Elevator(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            elevator.AddDestination(0, Direction.Up));
        Assert.Throws<ArgumentException>(() =>
            elevator.AddDestination(BuildingConstants.TotalFloors + 1, Direction.Down));
    }

    [Fact]
    public void AddDestination_AddsToCorrectQueue_BasedOnDirection()
    {
        // Arrange
        var elevator = new Elevator(1, startingFloor: 5);

        // Act
        elevator.AddDestination(8, Direction.Up);
        elevator.AddDestination(3, Direction.Down);

        // Assert
        Assert.Equal(2, elevator.GetPendingDestinationCount());
    }

    [Fact]
    public async Task Elevator_MovesUpCorrectly()
    {
        // Arrange
        var elevator = new Elevator(1, startingFloor: 1);
        var cts = new CancellationTokenSource();
        var runTask = elevator.RunAsync(cts.Token);

        // Act
        elevator.AddDestination(3, Direction.Up);

        // Wait until elevator reaches destination (with 2 second timeout)
        var timeout = DateTime.Now.AddSeconds(2);
        while (elevator.CurrentFloor != 3 && DateTime.Now < timeout)
        {
            await Task.Delay(50); // Check every 50ms
        }

        // Assert
        Assert.Equal(3, elevator.CurrentFloor);

        // Cleanup
        cts.Cancel();
        try
        {
            await runTask;
        }
        catch (TaskCanceledException)
        {
            // Expected when cancelling
        }
    }

    [Fact]
    public async Task Elevator_MovesDownCorrectly()
    {
        // Arrange
        var elevator = new Elevator(1, startingFloor: 5);
        var cts = new CancellationTokenSource();
        var runTask = elevator.RunAsync(cts.Token);

        // Act
        elevator.AddDestination(3, Direction.Down);

        // Wait until elevator reaches destination (with 2 second timeout)
        var timeout = DateTime.Now.AddSeconds(2);
        while (elevator.CurrentFloor != 3 && DateTime.Now < timeout)
        {
            await Task.Delay(50); // Check every 50ms
        }

        // Assert
        Assert.Equal(3, elevator.CurrentFloor);

        // Cleanup
        cts.Cancel();
        try
        {
            await runTask;
        }
        catch (TaskCanceledException)
        {
            // Expected when cancelling
        }
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var elevator = new Elevator(2, startingFloor: 7);

        // Act
        var result = elevator.ToString();

        // Assert
        Assert.Contains("Elevator 2", result);
        Assert.Contains("Floor 7", result);
    }
}
