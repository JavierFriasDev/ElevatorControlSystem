using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Constants;
using ElevatorControlSystem.Enums;

namespace ElevatorControlSystem.Models;

/// <summary>
/// Represents an elevator car in the building
/// </summary>
public class Elevator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SortedSet<int> _upDestinations = new();
    private readonly SortedSet<int> _downDestinations = new();

    public int Id { get; }
    public int CurrentFloor { get; private set; }
    public Direction CurrentDirection { get; private set; }
    public ElevatorState State { get; private set; }

    // Events for logging
    public event Action<int, int, Direction>? OnFloorChanged;
    public event Action<int, ElevatorState>? OnStateChanged;

    public Elevator(int id, int startingFloor = 1)
    {
        Id = id;
        CurrentFloor = startingFloor;
        CurrentDirection = Direction.Idle;
        State = ElevatorState.Idle;
    }

    /// <summary>
    /// Adds a destination floor for the elevator
    /// </summary>
    public void AddDestination(int floor, Direction requestDirection)
    {
        if (floor < 1 || floor > BuildingConstants.TotalFloors)
        {
            throw new ArgumentException($"Floor must be between 1 and {BuildingConstants.TotalFloors}");
        }

        lock (_upDestinations)
        {
            // Determine which queue to add to based on request direction and current state
            if (requestDirection == Direction.Up ||
                (CurrentDirection == Direction.Up && floor > CurrentFloor))
            {
                _upDestinations.Add(floor);
            }
            else if (requestDirection == Direction.Down ||
                     (CurrentDirection == Direction.Down && floor < CurrentFloor))
            {
                _downDestinations.Add(floor);
            }
            else
            {
                // If idle, determine direction based on floor relative to current
                if (floor > CurrentFloor)
                    _upDestinations.Add(floor);
                else if (floor < CurrentFloor)
                    _downDestinations.Add(floor);
            }
        }
    }

    /// <summary>
    /// Main elevator operation loop - runs asynchronously
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var nextFloor = GetNextDestination();

                if (nextFloor.HasValue)
                {
                    await MoveToFloorAsync(nextFloor.Value, cancellationToken);
                }
                else
                {
                    // No destinations, go idle
                    UpdateState(ElevatorState.Idle);
                    UpdateDirection(Direction.Idle);
                    await Task.Delay(1000, cancellationToken); // Wait briefly before checking again
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Determines the next floor to visit based on current direction
    /// </summary>
    private int? GetNextDestination()
    {
        lock (_upDestinations)
        {
            // Continue in current direction if possible
            if (CurrentDirection == Direction.Up && _upDestinations.Count > 0)
            {
                return _upDestinations.Min;
            }
            else if (CurrentDirection == Direction.Down && _downDestinations.Count > 0)
            {
                return _downDestinations.Max;
            }
            // Change direction if needed
            else if (_upDestinations.Count > 0)
            {
                UpdateDirection(Direction.Up);
                return _upDestinations.Min;
            }
            else if (_downDestinations.Count > 0)
            {
                UpdateDirection(Direction.Down);
                return _downDestinations.Max;
            }

            return null; // No destinations
        }
    }

    /// <summary>
    /// Moves the elevator to the specified floor
    /// </summary>
    private async Task MoveToFloorAsync(int targetFloor, CancellationToken cancellationToken)
    {
        while (CurrentFloor != targetFloor && !cancellationToken.IsCancellationRequested)
        {
            UpdateState(ElevatorState.Moving);

            // Determine direction
            if (targetFloor > CurrentFloor)
            {
                UpdateDirection(Direction.Up);
                CurrentFloor++;
            }
            else
            {
                UpdateDirection(Direction.Down);
                CurrentFloor--;
            }

            OnFloorChanged?.Invoke(Id, CurrentFloor, CurrentDirection);

            // Simulate travel time between floors
            await Task.Delay(BuildingConstants.FloorTravelTimeSeconds * 1000, cancellationToken);
        }

        // Arrived at destination
        await StopAtFloorAsync(targetFloor, cancellationToken);
    }

    /// <summary>
    /// Handles stopping at a floor (loading/unloading)
    /// </summary>
    private async Task StopAtFloorAsync(int floor, CancellationToken cancellationToken)
    {
        UpdateState(ElevatorState.Loading);

        // Remove from destination queue
        lock (_upDestinations)
        {
            _upDestinations.Remove(floor);
            _downDestinations.Remove(floor);
        }

        Console.WriteLine($"  → Elevator {Id} doors opening on floor {floor}");

        // Simulate loading time
        await Task.Delay(BuildingConstants.LoadingTimeSeconds * 1000, cancellationToken);

        Console.WriteLine($"  → Elevator {Id} doors closing on floor {floor}");
    }

    private void UpdateState(ElevatorState newState)
    {
        if (State != newState)
        {
            State = newState;
            OnStateChanged?.Invoke(Id, newState);
        }
    }

    private void UpdateDirection(Direction newDirection)
    {
        CurrentDirection = newDirection;
    }

    /// <summary>
    /// Gets the count of pending destinations
    /// </summary>
    public int GetPendingDestinationCount()
    {
        lock (_upDestinations)
        {
            return _upDestinations.Count + _downDestinations.Count;
        }
    }

    public override string ToString()
    {
        return $"Elevator {Id}: Floor {CurrentFloor}, {State}, {CurrentDirection}";
    }
}
