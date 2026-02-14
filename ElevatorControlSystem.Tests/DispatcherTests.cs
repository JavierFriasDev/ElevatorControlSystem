using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Enums;
using ElevatorControlSystem.Models;
using ElevatorControlSystem.Services;
using Xunit;

namespace ElevatorControlSystem.Tests;

public class DispatcherTests
{
    [Fact]
    public void SelectElevator_PrefersIdleElevator()
    {
        // Arrange
        var elevators = new List<Elevator>
        {
            new Elevator(1, 1) { }, // Idle
            new Elevator(2, 10) { } // Idle
        };

        var dispatcher = new SimpleElevatorDispatcher();
        var request = new ElevatorRequest(5, Direction.Up);

        // Act
        var selected = dispatcher.SelectElevator(elevators, request);

        // Assert
        Assert.NotNull(selected);
        Assert.Equal(1, selected.Id); // Closer to floor 5
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
}
