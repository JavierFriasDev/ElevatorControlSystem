using ElevatorControlSystem.Constants;
using ElevatorControlSystem.Models;
using ElevatorControlSystem.Services;

namespace ElevatorControlSystem;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("**********************************************************");
        Console.WriteLine("*     ELEVATOR CONTROL SYSTEM - SIMULATION START        *");
        Console.WriteLine("**********************************************************\n");

        // Create elevators
        var elevators = new List<Elevator>();
        for (int i = 1; i <= BuildingConstants.TotalElevators; i++)
        {
            elevators.Add(new Elevator(i, startingFloor: 1));
        }

        // Create dispatcher and controller
        var dispatcher = new SimpleElevatorDispatcher();
        var controller = new ElevatorController(elevators, dispatcher);

        // Start elevator operation
        controller.Start();

        // Display initial status
        controller.DisplayStatus();

        // Create request generator
        var requestGenerator = new RandomRequestGenerator(controller);

        // Setup cancellation
        var cts = new CancellationTokenSource();

        Console.WriteLine("Press 'Q' to quit, 'S' to show status\n");

        // Start request generation in background
        var generatorTask = requestGenerator.StartGeneratingRequestsAsync(cts.Token);

        // Handle user input
        var inputTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("\n\nShutting down...\n");
                        cts.Cancel();
                        break;
                    }
                    else if (key.Key == ConsoleKey.S)
                    {
                        controller.DisplayStatus();
                    }
                }
                await Task.Delay(100);
            }
        });

        // Wait for either task to complete
        await Task.WhenAny(generatorTask, inputTask);

        // Cleanup
        cts.Cancel();
        await controller.StopAsync();

        Console.WriteLine("Simulation ended.");
    }
}