using System;
using System.Collections.Generic;
using System.Text;

namespace ElevatorControlSystem.Enums;

/// <summary>
/// Represents the current state of an elevator
/// </summary>
public enum ElevatorState
{
    // Waiting for requests
    Idle,
    // Moving between floors
    Moving,
    // Doors open, loading/unloading passengers
    Loading
}
