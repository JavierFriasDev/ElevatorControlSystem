using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Models;

namespace ElevatorControlSystem.Services;

/// <summary>
/// Main controller for the elevator system
/// </summary>
public class ElevatorController
{
    private readonly List<Elevator> _elevators;
    private readonly IElevatorDispatcher _dispatcher;
    private readonly List<Task> _elevatorTasks = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public IReadOnlyList<Elevator> Elevators => _elevators.AsReadOnly();

    public ElevatorController(List<Elevator> elevators, IElevatorDispatcher dispatcher)
    {
        _elevators = elevators ?? throw new ArgumentNullException(nameof(elevators));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        // Subscribe to elevator events for logging
        foreach (var elevator in _elevators)
        {
            elevator.OnFloorChanged += HandleFloorChanged;
            elevator.OnStateChanged += HandleStateChanged;
        }
    }

    /// <summary>
    /// Starts all elevators
    /// </summary>
    public void Start()
    {
        foreach (var elevator in _elevators)
        {
            var task = elevator.RunAsync(_cancellationTokenSource.Token);
            _elevatorTasks.Add(task);
        }
    }

    /// <summary>
    /// Processes an elevator request
    /// </summary>
    public void RequestElevator(ElevatorRequest request)
    {
        Console.WriteLine($"\n[REQUEST] {request.Direction} button pressed on floor {request.Floor}");

        var selectedElevator = _dispatcher.SelectElevator(_elevators, request);

        if (selectedElevator != null)
        {
            selectedElevator.AddDestination(request.Floor, request.Direction);
            Console.WriteLine($"[DISPATCH] Elevator {selectedElevator.Id} assigned to floor {request.Floor}");
        }
        else
        {
            Console.WriteLine($"[ERROR] No elevator available for request on floor {request.Floor}");
        }
    }

    /// <summary>
    /// Stops all elevators
    /// </summary>
    public async Task StopAsync()
    {
        _cancellationTokenSource.Cancel();
        await Task.WhenAll(_elevatorTasks);
    }

    /// <summary>
    /// Displays current status of all elevators
    /// </summary>
    public void DisplayStatus()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("ELEVATOR STATUS");
        Console.WriteLine(new string('=', 60));
        foreach (var elevator in _elevators)
        {
            Console.WriteLine(elevator);
        }
        Console.WriteLine(new string('=', 60) + "\n");
    }

    private void HandleFloorChanged(int elevatorId, int floor, Enums.Direction direction)
    {
        Console.WriteLine($"[MOVE] Elevator {elevatorId} passing floor {floor} ({direction})");
    }

    private void HandleStateChanged(int elevatorId, Enums.ElevatorState state)
    {
        Console.WriteLine($"[STATE] Elevator {elevatorId} state changed to {state}");
    }
}
