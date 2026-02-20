using System;
using System.Collections.Generic;
using System.Text;

namespace ElevatorControlSystem.Constants;

/// <summary>
/// Constants for building configuration
/// </summary>
public static class BuildingConstants
{
    public const int TotalFloors = 10;
    public const int TotalElevators = 4;
    /// <summary>
    /// Time in seconds for elevator to travel between floors.
    /// Defaults to 10 seconds for production, can be set to 0 for fast testing.
    /// </summary>
    public static int FloorTravelTimeSeconds { get; set; } = 10;
    
    /// <summary>
    /// Time in seconds for loading/unloading passengers.
    /// Defaults to 10 seconds for production, can be set to 0 for fast testing.
    /// </summary>
    public static int LoadingTimeSeconds { get; set; } = 10;
}
