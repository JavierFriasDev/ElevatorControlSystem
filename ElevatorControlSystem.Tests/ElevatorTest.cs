using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Constants;
using ElevatorControlSystem.Enums;
using ElevatorControlSystem.Models;
using Xunit;

namespace ElevatorControlSystem.Tests;

public class ElevatorTests
{
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
        await Task.Delay(32000);

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
        await Task.Delay(32000);

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
}