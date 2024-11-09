# Mythosia IO Extensions

## Required Namespaces
```csharp
using Mythosia;
using Mythosia.IO;
```

### WriteExAsync
Applies a timeout to stream writing operations. Prevents infinite waiting and ensures reliable timeout handling, especially in network streams.
```csharp
using var networkStream = new TcpClient("127.0.0.1", 8000).GetStream();
await networkStream.WriteExAsync("Hello, World!".ToUTF8Array(), timeout: 5000);
```

### ReadExAsync
Applies a timeout to stream reading operations. Allows control to be regained even in situations with unstable network conditions or no response.
```csharp
using var stream = new TcpClient("127.0.0.1", 8000).GetStream();
byte[] buffer = new byte[1024];
int bytesRead = await stream.ReadExAsync(buffer, timeout: 5000);
```

### IsValidFilePathAndName
Validates the complete file path. Enables path validation before performing file system operations.
```csharp
string filePath = @"C:\MyFolder\MyFile.txt";
bool isValid = filePath.IsValidFilePathAndName();
```

### WriteBytesAsync
Performs binary data writing operations to a file. Automatically creates necessary directories and supports cancellation tokens.
```csharp
await "C:\MyFolder\MyData.bin".WriteBytesAsync(new byte[] { 0x1, 0x2, 0x3 });
```

### WriteTextAsync
Performs text writing operations to a file. Automatically creates necessary directories and supports cancellation tokens.
```csharp
await "C:\MyFolder\MyFile.txt".WriteTextAsync("Hello World");
```

### ReadAllBytesAsync
Safely reads binary data from a file. Returns an empty array if the file doesn't exist or is inaccessible.
```csharp
byte[] data = await "C:\MyFolder\MyData.bin".ReadAllBytesAsync();
```

### ReadAllTextAsync
Safely reads text from a file. Returns an empty string if the file doesn't exist or is inaccessible.
```csharp
string text = await "C:\MyFolder\MyFile.txt".ReadAllTextAsync();
```

### GetAllFiles
Safely retrieves a list of files from a directory. Returns an empty array if the directory doesn't exist or is inaccessible.
```csharp
// Get all files
string[] files = "C:\MyFolder".GetAllFiles();

// Get specific files
string[] textFiles = "C:\MyFolder".GetAllFiles("*.txt");

// Get files recursively
string[] allFiles = "C:\MyFolder".GetAllFiles("*", SearchOption.AllDirectories);
```