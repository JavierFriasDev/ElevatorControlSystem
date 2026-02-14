using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Models;

namespace ElevatorControlSystem.Services;

/// <summary>
/// Interface for elevator dispatching strategies
/// </summary>
public interface IElevatorDispatcher
{
    /// <summary>
    /// Selects the best elevator for a given request
    /// </summary>
    Elevator? SelectElevator(IEnumerable<Elevator> elevators, ElevatorRequest request);
}
