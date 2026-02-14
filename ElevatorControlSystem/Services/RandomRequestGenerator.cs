using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Constants;
using ElevatorControlSystem.Enums;
using ElevatorControlSystem.Models;

namespace ElevatorControlSystem.Services;

/// <summary>
/// Generates random elevator requests for simulation
/// </summary>
public class RandomRequestGenerator
{
    private readonly Random _random = new();
    private readonly ElevatorController _controller;

    public RandomRequestGenerator(ElevatorController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    /// <summary>
    /// Starts generating random requests
    /// </summary>
    public async Task StartGeneratingRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Wait random time between requests (5-15 seconds)
            await Task.Delay(_random.Next(5000, 15000), cancellationToken);

            var floor = _random.Next(1, BuildingConstants.TotalFloors + 1);
            var direction = _random.Next(0, 2) == 0 ? Direction.Up : Direction.Down;

            // Avoid invalid combinations (can't go up from top floor or down from ground floor)
            if (floor == BuildingConstants.TotalFloors)
                direction = Direction.Down;
            else if (floor == 1)
                direction = Direction.Up;

            var request = new ElevatorRequest(floor, direction);
            _controller.RequestElevator(request);
        }
    }
}
