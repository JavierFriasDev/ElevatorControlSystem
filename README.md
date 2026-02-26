# ElevatorControlSystem

A production-like elevator control system simulation built with C# and .NET 10.0, demonstrating clean architecture, SOLID principles, and asynchronous programming patterns.

## Overview

This project implements an elementary elevator control system that manages 4 elevators across a 10-floor building. The system handles random passenger requests, dispatches elevators intelligently, and tracks elevator movements in real-time.

### Key Features

- **4 Elevators** managing **10 floors**
- **Smart Dispatching Algorithm** with 3-tier priority system
- **Asynchronous Operations** using async/await patterns
- **Thread-Safe** implementation with locks and semaphores
- **Event-Driven Architecture** for real-time notifications
- **Comprehensive Unit Tests** with xUnit
- **Clean Code** following SOLID principles

### Running Tests

```bash
dotnet test
```

Expected output:
```
Test summary: total: 9, failed: 0, succeeded: 7, skipped: 0
```