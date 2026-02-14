using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Enums;
using ElevatorControlSystem.Models;

namespace ElevatorControlSystem.Services;

/// <summary>
/// Simple elevator dispatcher - selects closest available elevator
/// </summary>
public class SimpleElevatorDispatcher : IElevatorDispatcher
{
    public Elevator? SelectElevator(IEnumerable<Elevator> elevators, ElevatorRequest request)
    {
        var elevatorList = elevators.ToList();

        // Priority 1: Elevator already moving in the same direction and will pass the floor
        var matchingDirectionElevator = elevatorList
            .Where(e => e.CurrentDirection == request.Direction &&
                       e.State == ElevatorState.Moving &&
                       ((request.Direction == Direction.Up && e.CurrentFloor < request.Floor) ||
                        (request.Direction == Direction.Down && e.CurrentFloor > request.Floor)))
            .OrderBy(e => Math.Abs(e.CurrentFloor - request.Floor))
            .FirstOrDefault();

        if (matchingDirectionElevator != null)
            return matchingDirectionElevator;

        // Priority 2: Idle elevator (closest one)
        var idleElevator = elevatorList
            .Where(e => e.State == ElevatorState.Idle)
            .OrderBy(e => Math.Abs(e.CurrentFloor - request.Floor))
            .FirstOrDefault();

        if (idleElevator != null)
            return idleElevator;

        // Priority 3: Any elevator (least busy)
        return elevatorList
            .OrderBy(e => e.GetPendingDestinationCount())
            .ThenBy(e => Math.Abs(e.CurrentFloor - request.Floor))
            .FirstOrDefault();
    }
}
