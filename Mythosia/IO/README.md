# Stream Extensions in Mythosia

`Mythosia` provides additional extensions to enhance the capabilities of the Stream class in .NET. These extensions allow developers to implement read and write operations with timeout capabilities more conveniently.

## Using WriteExAsync and ReadExAsync with NetworkStream

To use these extensions, you should first ensure you have the appropriate using directive:

```csharp
using Mythosia;
using Mythosia.IO;
```

### WriteExAsync

This method allows you to write data to a stream asynchronously with a specified timeout.

Example using `NetworkStream`:

```csharp
using var networkStream = new TcpClient("127.0.0.1", 8000).GetStream();

// The operation will timeout if it takes longer than 5 seconds.
await networkStream.WriteExAsync("Hello, World!".ToUTF8Array(), timeout: 5000);  
```

### ReadExAsync

This method provides a mechanism to read data from a stream asynchronously with a specified timeout.

Example using `NetworkStream`:

```csharp
using var stream = new TcpClient("127.0.0.1", 8000).GetStream();

byte[] buffer = new byte[1024];
// The operation will timeout if it takes longer than 5 seconds.
int bytesRead = await stream.ReadExAsync(buffer, timeout: 5000);
```

Using these extension methods, you can easily add timeout capabilities to your read and write operations, making your network communications more resilient to unexpected delays. 
Moreover, they integrate seamlessly with the existing timeout properties of streams, allowing for a consistent programming experience.