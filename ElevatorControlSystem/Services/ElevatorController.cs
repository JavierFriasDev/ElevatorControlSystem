using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Models;
using ElevatorControlSystem.Constants;
using ElevatorControlSystem.Enums;

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
    private readonly List<ElevatorRequest> _activeRequests = new();

    public IReadOnlyList<Elevator> Elevators => _elevators.AsReadOnly();
    public IReadOnlyList<ElevatorRequest> ActiveRequests => _activeRequests.AsReadOnly();

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

        // Track active request for UI
        lock (_activeRequests)
        {
            _activeRequests.Add(request);
        }

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
        var status = GetStatusString();
        Console.WriteLine(status);
    }

    /// <summary>
    /// Builds a visual, testable representation of all elevators and floors.
    /// Returns a multi-line string that can be asserted in tests.
    /// </summary>
    public string GetStatusString()
    {
        var sb = new StringBuilder();
        int floors = BuildingConstants.TotalFloors;

        sb.AppendLine();
        sb.AppendLine(new string('=', 60));
        sb.AppendLine("ELEVATOR STATUS");
        sb.AppendLine(new string('=', 60));

        // Header with elevator IDs
        sb.Append("Floor ".PadRight(8));
        foreach (var e in _elevators)
        {
            sb.Append($"E{e.Id}".PadRight(8));
        }
        sb.AppendLine();

        // Draw shafts from top floor to bottom
        for (int f = floors; f >= 1; f--)
        {
            sb.Append(f.ToString().PadLeft(3) + " ".PadRight(5));
            foreach (var e in _elevators)
            {
                if (e.CurrentFloor == f)
                    sb.Append($"[E{e.Id}:{e.CurrentDirection.ToString()[0]}]".PadRight(8));
                else
                    sb.Append("[     ]".PadRight(8));
            }
            sb.AppendLine();
        }

        sb.AppendLine(new string('-', 60));
        foreach (var e in _elevators)
        {
            sb.AppendLine(e.ToString());
        }
        sb.AppendLine(new string('=', 60));

        return sb.ToString();
    }

    private void HandleFloorChanged(int elevatorId, int floor, Enums.Direction direction)
    {
        Console.WriteLine($"[MOVE] Elevator {elevatorId} passing floor {floor} ({direction})");
    }

    private void HandleStateChanged(int elevatorId, Enums.ElevatorState state)
    {
        Console.WriteLine($"[STATE] Elevator {elevatorId} state changed to {state}");

        // When elevator starts loading, remove matching active requests for that floor
        if (state == ElevatorState.Loading)
        {
            var elevator = _elevators.FirstOrDefault(e => e.Id == elevatorId);
            if (elevator != null)
            {
                lock (_activeRequests)
                {
                    _activeRequests.RemoveAll(r => r.Floor == elevator.CurrentFloor);
                }
            }
        }
    }
}
