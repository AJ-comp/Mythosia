using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

// Basic remains a supported public compatibility alias.
#pragma warning disable CS0618

[Collection(IntegrityTestCollection.Name)]
public sealed class CrcInitializationTests
{
    private const int ColdStartRounds = 4;
    private const int WorkerCount = 32;
    private static readonly TimeSpan WorkerTimeout = TimeSpan.FromSeconds(30);
    private static readonly Algorithm[] Algorithms =
    {
        new(16, (int)CRC16Type.Basic, "CRC16/Basic"),
        new(16, (int)CRC16Type.Classic, "CRC16/Classic"),
        new(16, (int)CRC16Type.Modbus, "CRC16/Modbus"),
        new(16, (int)CRC16Type.CCITTxModem, "CRC16/CCITTxModem"),
        new(16, (int)CRC16Type.DNP, "CRC16/DNP"),
        new(32, (int)CRC32Type.Basic, "CRC32/Basic"),
        new(32, (int)CRC32Type.Classic, "CRC32/Classic")
    };

    [Theory]
    [InlineData("CRC16/Basic")]
    [InlineData("CRC16/Classic")]
    [InlineData("CRC16/Modbus")]
    [InlineData("CRC16/CCITTxModem")]
    [InlineData("CRC16/DNP")]
    [InlineData("CRC32/Basic")]
    [InlineData("CRC32/Classic")]
    [InlineData("Mixed")]
    public void ConcurrentFirstUseMatchesIndependentReferenceInFreshAssemblies(string scenario)
    {
        Algorithm[] algorithms = scenario == "Mixed"
            ? Algorithms
            : Algorithms.Where(algorithm => algorithm.Name == scenario).ToArray();
        Assert.NotEmpty(algorithms);
        PreparedAlgorithm[] prepared = algorithms.Select(PrepareAlgorithm).ToArray();

        for (int round = 0; round < ColdStartRounds; round++)
        {
            RoundResult result = RunColdRound(prepared);
            Assert.True(result.Errors.Length == 0,
                $"{scenario}, cold-start round {round + 1}:\n{string.Join("\n", result.Errors)}");
            AssertContextUnloaded(result.Context, scenario, round);
        }
    }

    // Loading another assembly instance gives each round fresh CLR static
    // storage even when other tests have already used the default assembly.
    // This helper must return before collection: its locals include reflection
    // handles and worker delegates that otherwise keep the context alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static RoundResult RunColdRound(PreparedAlgorithm[] prepared)
    {
        string assemblyPath = typeof(CRC16).Assembly.Location;
        var context = new IsolatedIntegrityContext(assemblyPath);
        var contextReference = new WeakReference(context);
        var errors = new ConcurrentQueue<string>();

        try
        {
            Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
            if (ReferenceEquals(assembly, typeof(CRC16).Assembly)
                || !ReferenceEquals(AssemblyLoadContext.GetLoadContext(assembly), context))
                throw new InvalidOperationException("The CRC assembly was not isolated from the default context.");

            // Metadata lookup does not call a constructor, read a static field,
            // or execute a CRC method. The first invocation is behind the gate.
            IsolatedAlgorithm[] isolated = prepared
                .Select(algorithm => new IsolatedAlgorithm(assembly, algorithm))
                .ToArray();
            RunCoordinatedWorkers(isolated, errors);
        }
        catch (Exception exception)
        {
            errors.Enqueue(DescribeException(exception));
        }
        finally
        {
            context.Unload();
        }

        return new RoundResult(contextReference, errors.ToArray());
    }

    private static void RunCoordinatedWorkers(IsolatedAlgorithm[] algorithms, ConcurrentQueue<string> errors)
    {
        using var ready = new CountdownEvent(WorkerCount);
        using var start = new ManualResetEventSlim(false);
        var workers = new List<Thread>(WorkerCount);

        try
        {
            for (int index = 0; index < WorkerCount; index++)
            {
                int workerIndex = index;
                var worker = new Thread(() => RunWorker(algorithms, workerIndex, ready, start, errors))
                {
                    IsBackground = true
                };
                worker.Start();
                workers.Add(worker);
            }

            // Dedicated threads avoid thread-pool starvation at the start gate.
            if (!ready.Wait(WorkerTimeout))
                errors.Enqueue("Not all workers reached the first-use gate within the timeout.");
        }
        finally
        {
            // Release and join already-started workers even if another worker
            // cannot start. Nothing may retain the context after this helper.
            start.Set();
            JoinWorkers(workers, errors);
        }
    }

    private static void RunWorker(IsolatedAlgorithm[] algorithms, int workerIndex,
        CountdownEvent ready, ManualResetEventSlim start, ConcurrentQueue<string> errors)
    {
        IsolatedAlgorithm algorithm = algorithms[workerIndex % algorithms.Length];
        try
        {
            ready.Signal();
            if (!start.Wait(WorkerTimeout))
                throw new TimeoutException("Waiting for the first-use gate timed out.");
            VerifyWorker(algorithm, workerIndex);
        }
        catch (Exception exception)
        {
            errors.Enqueue($"Worker {workerIndex}, {algorithm.Prepared.Algorithm.Name}: {DescribeException(exception)}");
        }
    }

    private static void JoinWorkers(IEnumerable<Thread> workers, ConcurrentQueue<string> errors)
    {
        long deadline = Environment.TickCount64 + (long)WorkerTimeout.TotalMilliseconds;
        foreach (Thread worker in workers)
        {
            int remaining = (int)Math.Max(0, deadline - Environment.TickCount64);
            if (!worker.Join(remaining))
                errors.Enqueue("A CRC worker did not finish within the timeout.");
        }
    }

    private static void VerifyWorker(IsolatedAlgorithm algorithm, int workerIndex)
    {
        object detector = algorithm.CreateDetector();
        TestVector first = algorithm.Prepared.Vectors[0];

        // Alternate the public operation performed immediately after the cold
        // constructor. Reference frames are prepared without production code.
        switch (workerIndex % 4)
        {
            case 0:
                Assert.Equal(first.Suffix, algorithm.Compute(detector, first.Payload));
                break;
            case 1:
                Assert.Equal(first.Frame, algorithm.Encode(detector, first.Payload));
                break;
            case 2:
                Assert.False(algorithm.IsError(detector, first.Frame));
                break;
            case 3:
                Assert.Equal(first.Payload, algorithm.Decode(detector, first.Frame));
                break;
        }

        foreach (TestVector vector in algorithm.Prepared.Vectors)
        {
            Assert.Equal(vector.Suffix, algorithm.Compute(detector, vector.Payload));
            Assert.Equal(vector.Frame, algorithm.Encode(detector, vector.Payload));
            Assert.False(algorithm.IsError(detector, vector.Frame));
            Assert.Equal(vector.Payload, algorithm.Decode(detector, vector.Frame));
            Assert.True(algorithm.IsError(detector, vector.CorruptedFrame));
            Assert.Empty(algorithm.Decode(detector, vector.CorruptedFrame));
        }
    }

    private static PreparedAlgorithm PrepareAlgorithm(Algorithm algorithm)
    {
        // Hit table entry 255 immediately, where the old premature publication
        // was most likely to expose a value that had not yet been generated.
        byte firstByte = algorithm.Width == 32 || algorithm.Variant == (int)CRC16Type.Modbus
            ? (byte)0
            : byte.MaxValue;
        byte[][] payloads =
        {
            new[] { firstByte },
            new byte[] { 0x80, 0x00, 0xFF },
            Encoding.ASCII.GetBytes("123456789"),
            Enumerable.Range(0, 256).Select(value => (byte)value).ToArray(),
            Enumerable.Range(0, 256).Select(value => (byte)(255 - value)).ToArray(),
            Enumerable.Repeat((byte)0x55, 32).ToArray(),
            Enumerable.Repeat((byte)0xAA, 33).ToArray(),
            DeterministicBytes(257),
            DeterministicBytes(1025)
        };
        TestVector[] vectors = payloads.Select(payload => CreateVector(algorithm, payload)).ToArray();
        return new PreparedAlgorithm(algorithm, vectors);
    }

    private static TestVector CreateVector(Algorithm algorithm, byte[] payload)
    {
        uint value;
        if (algorithm.Width == 16)
        {
            ushort canonical = CrcReference.Compute16(payload, (CRC16Type)algorithm.Variant);
            value = algorithm.Variant == (int)CRC16Type.DNP
                ? CrcReference.ToLegacyDnpRepresentation(canonical)
                : canonical;
        }
        else
        {
            value = CrcReference.Compute32(payload, (CRC32Type)algorithm.Variant);
        }

        int byteCount = algorithm.Width / 8;
        var suffix = new byte[byteCount];
        for (int index = 0; index < suffix.Length; index++)
        {
            int shift = (BitConverter.IsLittleEndian ? index : byteCount - 1 - index) * 8;
            suffix[index] = (byte)(value >> shift);
        }

        byte[] frame = payload.Concat(suffix).ToArray();
        byte[] corrupted = (byte[])frame.Clone();
        corrupted[0] ^= 0x01;
        return new TestVector(payload, suffix, frame, corrupted);
    }

    private static byte[] DeterministicBytes(int length)
    {
        var payload = new byte[length];
        uint state = 0xC0FFEE01;
        for (int index = 0; index < payload.Length; index++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            payload[index] = (byte)state;
        }
        return payload;
    }

    private static void AssertContextUnloaded(WeakReference context, string scenario, int round)
    {
        for (int attempt = 0; context.IsAlive && attempt < 10; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Yield();
        }

        Assert.False(context.IsAlive,
            $"{scenario}, cold-start round {round + 1}: the isolated assembly context did not unload.");
    }

    private static string DescribeException(Exception exception)
    {
        while (exception is TargetInvocationException { InnerException: not null })
            exception = exception.InnerException;
        return $"{exception.GetType().Name}: {exception.Message}";
    }

    private sealed class IsolatedIntegrityContext : AssemblyLoadContext
    {
        private readonly string dependencyPath;

        public IsolatedIntegrityContext(string assemblyPath) : base(isCollectible: true)
        {
            dependencyPath = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "Mythosia.dll");
        }

        protected override Assembly? Load(AssemblyName assemblyName)
            => assemblyName.Name == "Mythosia" ? LoadFromAssemblyPath(dependencyPath) : null;
    }

    private sealed class IsolatedAlgorithm
    {
        private readonly ConstructorInfo constructor;
        private readonly object variant;
        private readonly MethodInfo compute;
        private readonly MethodInfo encode;
        private readonly MethodInfo isError;
        private readonly MethodInfo decode;

        public PreparedAlgorithm Prepared { get; }

        public IsolatedAlgorithm(Assembly assembly, PreparedAlgorithm prepared)
        {
            Prepared = prepared;
            string typeName = $"Mythosia.Integrity.CRC.CRC{prepared.Algorithm.Width}";
            Type detectorType = assembly.GetType(typeName, throwOnError: true)!;
            Type variantType = assembly.GetType($"{typeName}Type", throwOnError: true)!;
            constructor = detectorType.GetConstructor(new[] { variantType })!;
            variant = Enum.ToObject(variantType, prepared.Algorithm.Variant);
            Type[] sourceParameter = { typeof(IEnumerable<byte>) };
            compute = detectorType.GetMethod("Compute", sourceParameter)!;
            encode = detectorType.GetMethod("Encode", sourceParameter)!;
            isError = detectorType.GetMethod("IsError", sourceParameter)!;
            decode = detectorType.GetMethod("Decode", sourceParameter)!;
        }

        public object CreateDetector() => constructor.Invoke(new[] { variant });
        public byte[] Compute(object detector, byte[] source) => InvokeBytes(compute, detector, source);
        public byte[] Encode(object detector, byte[] source) => InvokeBytes(encode, detector, source);
        public byte[] Decode(object detector, byte[] source) => InvokeBytes(decode, detector, source);
        public bool IsError(object detector, byte[] source)
            => (bool)isError.Invoke(detector, new object[] { source })!;

        private static byte[] InvokeBytes(MethodInfo method, object detector, byte[] source)
            => ((IEnumerable<byte>)method.Invoke(detector, new object[] { source })!).ToArray();
    }

    private sealed record Algorithm(int Width, int Variant, string Name);
    private sealed record PreparedAlgorithm(Algorithm Algorithm, TestVector[] Vectors);
    private sealed record TestVector(byte[] Payload, byte[] Suffix, byte[] Frame, byte[] CorruptedFrame);
    private sealed record RoundResult(WeakReference Context, string[] Errors);
}

#pragma warning restore CS0618
