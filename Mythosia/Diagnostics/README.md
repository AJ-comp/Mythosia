# Mythosia Diagnostics Extensions

## Execution Time Measurement
`Mythosia.Diagnostics` provides extensions for measuring execution time of both synchronous and asynchronous methods.

### Basic Usage
First, include the required namespace:
```csharp
using Mythosia.Diagnostics;
```

### Measuring Synchronous Methods
For synchronous methods, you can measure execution time in several ways:

#### Simple Method with Return Value
```csharp
var result = ExecutionTimeExtension.MeasureExecutionTime(
    () => YourMethod(),
    elapsed => Console.WriteLine($"Execution took: {elapsed.TotalMilliseconds}ms")
);
```

#### Simple Method without Return Value
```csharp
ExecutionTimeExtension.MeasureExecutionTime(
    () => YourVoidMethod(),
    elapsed => Console.WriteLine($"Execution took: {elapsed.TotalMilliseconds}ms")
);
```

#### Method with Parameters
```csharp
var result = ExecutionTimeExtension.MeasureExecutionTime(
    param => YourMethodWithParam(param),
    "your parameter",
    elapsed => Console.WriteLine($"Execution took: {elapsed.TotalMilliseconds}ms")
);
```

### Measuring Asynchronous Methods
For async methods, you have several options:

#### Extension Method Style
```csharp
var result = await YourAsyncMethod().MeasureExecutionTimeAsync(
    elapsed => Console.WriteLine($"Async execution took: {elapsed.TotalMilliseconds}ms")
);
```

#### Method with Parameters
```csharp
var result = await ExecutionTimeExtension.MeasureExecutionTimeAsync(
    async param => await YourAsyncMethodWithParam(param),
    "your parameter",
    elapsed => Console.WriteLine($"Async execution took: {elapsed.TotalMilliseconds}ms")
);
```

#### Method with Multiple Parameters
```csharp
var result = await ExecutionTimeExtension.MeasureExecutionTimeAsync(
    async (param1, param2) => await YourAsyncMethodWithTwoParams(param1, param2),
    "first param",
    "second param",
    elapsed => Console.WriteLine($"Async execution took: {elapsed.TotalMilliseconds}ms")
);
```

## Command Execution Extension
`Mythosia.Diagnostics` also provides a string extension method for executing shell commands cross-platform.

### Basic Usage
Include the required namespace:
```csharp
using Mythosia.Diagnostics;
```

### Executing Commands
You can execute shell commands as a string extension:

```csharp
// Simple command execution
var result = await "dir".ExecuteCommandAsync();
if (result.IsSuccess)
{
    Console.WriteLine(result.Result);
}
else
{
    Console.WriteLine(result.Error);
}
```

### Working with Command Results
The `CommandResult` class provides several properties:

```csharp
var commandResult = await "git status".ExecuteCommandAsync();
Console.WriteLine($"Success: {commandResult.IsSuccess}");
Console.WriteLine($"Exit Code: {commandResult.ExitCode}");
Console.WriteLine($"Output: {commandResult.Result}");
Console.WriteLine($"Error (if any): {commandResult.Error}");

// Or use the ToString() method for a formatted output
Console.WriteLine(commandResult.ToString());
```

### Cross-Platform Support
The command execution automatically adapts to the current operating system:
- Windows: Uses `cmd.exe`
- Linux/macOS: Uses `/bin/bash`

```csharp
// Windows
var windowsResult = await "ipconfig".ExecuteCommandAsync();

// Linux/macOS
var unixResult = await "ls -la".ExecuteCommandAsync();
```

### Error Handling
It's recommended to use try-catch blocks when executing commands:

```csharp
try
{
    var result = await "some-command".ExecuteCommandAsync();
    if (!result.IsSuccess)
    {
        Console.WriteLine($"Command failed: {result.Error}");
        Console.WriteLine($"Exit code: {result.ExitCode}");
    }
}
catch (PlatformNotSupportedException ex)
{
    Console.WriteLine("This operating system is not supported.");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
```