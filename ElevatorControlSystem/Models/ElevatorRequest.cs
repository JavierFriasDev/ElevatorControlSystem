using System;
using System.Collections.Generic;
using System.Text;
using ElevatorControlSystem.Enums;

namespace ElevatorControlSystem.Models;

/// <summary>
/// Represents a request for an elevator
/// </summary>
public class ElevatorRequest
{
    public int Floor { get; set; }
    public Direction Direction { get; set; }
    public DateTime RequestTime { get; set; }

    public ElevatorRequest(int floor, Direction direction)
    {
        Floor = floor;
        Direction = direction;
        RequestTime = DateTime.Now;
    }

    public override string ToString()
    {
        return $"Request: Floor {Floor}, Direction {Direction}";
    }
}
