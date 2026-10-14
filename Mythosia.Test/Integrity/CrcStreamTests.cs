using System.Text;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

// Basic remains a supported compatibility name for each CRC width.
#pragma warning disable CS0618

[Collection(IntegrityTestCollection.Name)]
public sealed class CrcStreamTests
{
    private const int BufferSize = 64 * 1024;
    private static readonly byte[] PublishedCheckInput = Encoding.ASCII.GetBytes("123456789");
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    public enum Algorithm
    {
        Crc8Basic,
        Crc8Classic,
        Crc8Maxim,
        Crc16Basic,
        Crc16Classic,
        Crc16Modbus,
        Crc16Xmodem,
        Crc16Dnp,
        Crc32Basic,
        Crc32Classic
    }

    public enum ReadMode
    {
        Synchronous,
        Asynchronous
    }

    public static IEnumerable<object[]> Algorithms()
        => Enum.GetValues<Algorithm>().Select(algorithm => new object[] { algorithm });

    public static IEnumerable<object[]> AlgorithmsAndReadModes()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
        foreach (ReadMode mode in Enum.GetValues<ReadMode>())
            yield return new object[] { algorithm, mode };
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task PublishedChecks_MatchThroughPartialNonseekableReads(Algorithm algorithm, ReadMode mode)
    {
        using var source = new ObservedReadStream(PublishedCheckInput, new[] { 1, 2, 1, 5 })
        {
            RejectSynchronousReads = mode == ReadMode.Asynchronous
        };
        uint expected = algorithm switch
        {
            Algorithm.Crc8Basic or Algorithm.Crc8Classic => 0xF4,
            Algorithm.Crc8Maxim => 0xA1,
            Algorithm.Crc16Basic or Algorithm.Crc16Classic => 0xBB3D,
            Algorithm.Crc16Modbus => 0x4B37,
            Algorithm.Crc16Xmodem => 0x31C3,
            // Preserve the existing numeric API's byte-swapped DNP value.
            Algorithm.Crc16Dnp => 0x82EA,
            Algorithm.Crc32Basic or Algorithm.Crc32Classic => 0xCBF43926,
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };

        Assert.Equal(expected, ReferenceDigest(PublishedCheckInput, algorithm));
        Assert.Equal(expected, await ComputeForModeAsync(source, algorithm, mode));

        AssertCompletedRead(source, mode, PublishedCheckInput.Length);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task BufferBoundariesAndVariedPartialReads_MatchReferenceAndOneShot(
        Algorithm algorithm, ReadMode mode)
    {
        int[] lengths = { 1, 7, 257, BufferSize - 1, BufferSize, BufferSize + 1, 2 * BufferSize + 17 };
        foreach (int length in lengths)
        {
            byte[] payload = GeneratedBytes(length).ToArray();
            using var source = new ObservedReadStream(payload, new[] { 1, 31, 3, 8191, 64, BufferSize, 5 })
            {
                RejectSynchronousReads = mode == ReadMode.Asynchronous
            };
            uint expected = ReferenceDigest(payload, algorithm);

            Assert.Equal(expected, OneShotDigest(payload, algorithm));
            Assert.Equal(expected, await ComputeForModeAsync(source, algorithm, mode));

            AssertCompletedRead(source, mode, length);
            Assert.True(source.ReadCalls > 1);
        }
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task GeneratedLargeStream_ReadsOnceWithOneBoundedBuffer(Algorithm algorithm, ReadMode mode)
    {
        // The input is generated on demand; even the independent oracle reads
        // an enumerable, so the fixture never materializes the whole stream.
        const long length = 8L * 1024 * 1024 + 17;
        uint expected = ReferenceDigest(GeneratedBytes(length), algorithm);
        using var source = new ObservedReadStream(length)
        {
            RejectSynchronousReads = mode == ReadMode.Asynchronous
        };

        Assert.Equal(expected, await ComputeForModeAsync(source, algorithm, mode));

        AssertCompletedRead(source, mode, length);
        Assert.Equal((int)((length + BufferSize - 1) / BufferSize) + 1, source.ReadCalls);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task CurrentPositionAndReusedOpenStream_ComputeOnlyRemainingBytes(
        Algorithm algorithm, ReadMode mode)
    {
        byte[] payload = GeneratedBytes(BufferSize + 37).ToArray();
        const int prefixLength = 123;
        using var source = new MemoryStream(payload, writable: false);
        source.Position = prefixLength;

        uint remainingExpected = ReferenceDigest(payload.Skip(prefixLength), algorithm);
        Assert.Equal(remainingExpected, await ComputeForModeAsync(source, algorithm, mode));
        Assert.Equal(payload.Length, source.Position);
        Assert.True(source.CanRead);

        source.Position = 0;
        Assert.Equal(OneShotDigest(payload, algorithm), await ComputeForModeAsync(source, algorithm, mode));
        Assert.Equal(payload.Length, source.Position);
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task OmittedType_UsesClassicAndPreservesNumericReturnTypes()
    {
        using var source = new MemoryStream(PublishedCheckInput, writable: false);

        byte sync8 = source.ComputeCRC8();
        source.Position = 0;
        byte async8 = await source.ComputeCRC8Async();
        source.Position = 0;
        ushort sync16 = source.ComputeCRC16();
        source.Position = 0;
        ushort async16 = await source.ComputeCRC16Async();
        source.Position = 0;
        uint sync32 = source.ComputeCRC32();
        source.Position = 0;
        uint async32 = await source.ComputeCRC32Async();

        Assert.Equal((byte)0xF4, sync8);
        Assert.Equal(sync8, async8);
        Assert.Equal((ushort)0xBB3D, sync16);
        Assert.Equal(sync16, async16);
        Assert.Equal(0xCBF43926u, sync32);
        Assert.Equal(sync32, async32);
        Assert.True(source.CanRead);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task EmptyStream_PreservesExplicitEmptyResultAndLeavesSourceOpen(
        Algorithm algorithm, ReadMode mode)
    {
        using var source = new ObservedReadStream(Array.Empty<byte>())
        {
            RejectSynchronousReads = mode == ReadMode.Asynchronous
        };

        if (IsCrc8(algorithm))
            await Assert.ThrowsAsync<InvalidOperationException>(() => ComputeForModeAsync(source, algorithm, mode));
        else
        {
            uint expected = algorithm == Algorithm.Crc16Modbus ? 0xFFFFu : 0u;
            Assert.Equal(expected, await ComputeForModeAsync(source, algorithm, mode));
        }

        AssertCompletedRead(source, mode, 0);
        Assert.Equal(1, source.ReadCalls);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task NullStream_ThrowsArgumentNullExceptionForSource(Algorithm algorithm, ReadMode mode)
    {
        var failure = await Assert.ThrowsAsync<ArgumentNullException>(
            () => ComputeForModeAsync(null!, algorithm, mode));

        Assert.Equal("source", failure.ParamName);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task UnreadableStream_IsRejectedWithoutReadingOrClosing(Algorithm algorithm, ReadMode mode)
    {
        using var source = new ObservedReadStream(PublishedCheckInput) { Readable = false };

        var failure = await Assert.ThrowsAsync<ArgumentException>(
            () => ComputeForModeAsync(source, algorithm, mode));

        Assert.Equal("source", failure.ParamName);
        Assert.Equal(0, source.ReadCalls);
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [InlineData(8, ReadMode.Synchronous)]
    [InlineData(8, ReadMode.Asynchronous)]
    [InlineData(16, ReadMode.Synchronous)]
    [InlineData(16, ReadMode.Asynchronous)]
    [InlineData(32, ReadMode.Synchronous)]
    [InlineData(32, ReadMode.Asynchronous)]
    public async Task InvalidType_IsRejectedWithoutReadingOrClosing(int width, ReadMode mode)
    {
        foreach (int invalidType in new[] { -1, int.MaxValue })
        {
            using var source = new ObservedReadStream(PublishedCheckInput);

            var failure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => ComputeInvalidTypeAsync(source, width, invalidType, mode));

            Assert.Equal("type", failure.ParamName);
            Assert.Equal(0, source.ReadCalls);
            Assert.False(source.IsDisposed);
        }
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task ReadFailure_PropagatesOriginalExceptionAndLeavesSourceOpen(
        Algorithm algorithm, ReadMode mode)
    {
        foreach (int failingRead in new[] { 1, 3 })
        {
            var injectedFailure = new IOException("Injected read failure.");
            using var source = new ObservedReadStream(64, new[] { 7 })
            {
                RejectSynchronousReads = mode == ReadMode.Asynchronous,
                Failure = injectedFailure,
                FailOnReadCall = failingRead
            };

            var failure = await Assert.ThrowsAsync<IOException>(
                () => ComputeForModeAsync(source, algorithm, mode));

            Assert.Same(injectedFailure, failure);
            Assert.Equal(failingRead, source.ReadCalls);
            Assert.Equal((failingRead - 1) * 7, source.BytesReturned);
            Assert.Equal(0, source.EndOfStreamReads);
            AssertReadMode(source, mode);
            AssertBufferUsage(source);
            Assert.False(source.IsDisposed);
        }
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndReadModes))]
    public async Task VirtualSizeBeyondInt32_DoesNotProbeLengthOrPreallocateFileSize(
        Algorithm algorithm, ReadMode mode)
    {
        var injectedFailure = new IOException("Stop the virtual multi-gigabyte input after two chunks.");
        using var source = new ObservedReadStream((long)int.MaxValue + BufferSize)
        {
            RejectSynchronousReads = mode == ReadMode.Asynchronous,
            Failure = injectedFailure,
            FailOnReadCall = 3
        };

        var failure = await Assert.ThrowsAsync<IOException>(
            () => ComputeForModeAsync(source, algorithm, mode));

        Assert.Same(injectedFailure, failure);
        Assert.Equal(2L * BufferSize, source.BytesReturned);
        Assert.Equal(3, source.ReadCalls);
        AssertReadMode(source, mode);
        AssertBufferUsage(source);
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public async Task PreCanceledToken_CancelsBeforeReadingAndLeavesSourceOpen(Algorithm algorithm)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var source = new ObservedReadStream(PublishedCheckInput) { RejectSynchronousReads = true };

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ComputeAsync(source, algorithm, cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, source.ReadCalls);
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public async Task CancellationAfterPartialRead_StopsWithoutReturningDigestOrClosing(Algorithm algorithm)
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedReadStream(1024, new[] { 17 })
        {
            RejectSynchronousReads = true,
            AfterSuccessfulRead = cancellation.Cancel
        };

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ComputeAsync(source, algorithm, cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(17, source.BytesReturned);
        Assert.Equal(0, source.EndOfStreamReads);
        AssertReadMode(source, ReadMode.Asynchronous);
        AssertBufferUsage(source);
        Assert.All(source.ReadTokens, token => Assert.Equal(cancellation.Token, token));
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public async Task BlockingAsyncRead_ReceivesTokenAndCancelsWithoutSynchronousReadOrClose(Algorithm algorithm)
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new BlockingReadStream();
        Task<uint> computation = ComputeAsync(source, algorithm, cancellation.Token);

        await source.ReadStarted.Task.WaitAsync(CompletionTimeout);
        Assert.False(computation.IsCompleted);
        cancellation.Cancel();
        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => computation.WaitAsync(CompletionTimeout));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(1, source.AsynchronousReadCalls);
        Assert.Equal(0, source.SynchronousReadCalls);
        Assert.Equal(0, source.BytesReturned);
        AssertBufferUsage(source);
        Assert.Equal(cancellation.Token, Assert.Single(source.ReadTokens));
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public async Task AsyncContinuation_DoesNotCaptureCallingSynchronizationContext(int width)
    {
        using var source = new ContinuationProbeStream();
        var context = new ObservedSynchronizationContext();
        SynchronizationContext? originalContext = SynchronizationContext.Current;
        Task computation;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            computation = width switch
            {
                8 => source.ComputeCRC8Async(),
                16 => source.ComputeCRC16Async(),
                32 => source.ComputeCRC32Async(),
                _ => throw new ArgumentOutOfRangeException(nameof(width))
            };
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }

        Assert.False(computation.IsCompleted);
        source.CompleteFirstRead();
        await computation.WaitAsync(CompletionTimeout);
        uint actual = width switch
        {
            8 => await (Task<byte>)computation,
            16 => await (Task<ushort>)computation,
            32 => await (Task<uint>)computation,
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };
        uint expected = width switch
        {
            8 => 0xF4,
            16 => 0xBB3D,
            32 => 0xCBF43926,
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };

        Assert.Equal(expected, actual);
        Assert.Equal(0, context.PostCount);
        AssertCompletedRead(source, ReadMode.Asynchronous, PublishedCheckInput.Length);
    }

    private static bool IsCrc8(Algorithm algorithm)
        => algorithm is Algorithm.Crc8Basic or Algorithm.Crc8Classic or Algorithm.Crc8Maxim;

    private static uint ReferenceDigest(IEnumerable<byte> payload, Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc8Basic => CrcReference.Compute8(payload, CRC8Type.Basic),
        Algorithm.Crc8Classic => CrcReference.Compute8(payload, CRC8Type.Classic),
        Algorithm.Crc8Maxim => CrcReference.Compute8(payload, CRC8Type.Maxim),
        Algorithm.Crc16Basic => CrcReference.Compute16(payload, CRC16Type.Basic),
        Algorithm.Crc16Classic => CrcReference.Compute16(payload, CRC16Type.Classic),
        Algorithm.Crc16Modbus => CrcReference.Compute16(payload, CRC16Type.Modbus),
        Algorithm.Crc16Xmodem => CrcReference.Compute16(payload, CRC16Type.CCITTxModem),
        Algorithm.Crc16Dnp => CrcReference.ToLegacyDnpRepresentation(CrcReference.Compute16(payload, CRC16Type.DNP)),
        Algorithm.Crc32Basic => CrcReference.Compute32(payload, CRC32Type.Basic),
        Algorithm.Crc32Classic => CrcReference.Compute32(payload, CRC32Type.Classic),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static uint OneShotDigest(byte[] payload, Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc8Basic => payload.CRC8(CRC8Type.Basic),
        Algorithm.Crc8Classic => payload.CRC8(CRC8Type.Classic),
        Algorithm.Crc8Maxim => payload.CRC8(CRC8Type.Maxim),
        Algorithm.Crc16Basic => payload.CRC16(CRC16Type.Basic),
        Algorithm.Crc16Classic => payload.CRC16(CRC16Type.Classic),
        Algorithm.Crc16Modbus => payload.CRC16(CRC16Type.Modbus),
        Algorithm.Crc16Xmodem => payload.CRC16(CRC16Type.CCITTxModem),
        Algorithm.Crc16Dnp => payload.CRC16(CRC16Type.DNP),
        Algorithm.Crc32Basic => payload.CRC32(CRC32Type.Basic),
        Algorithm.Crc32Classic => payload.CRC32(CRC32Type.Classic),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static uint Compute(Stream source, Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc8Basic => source.ComputeCRC8(CRC8Type.Basic),
        Algorithm.Crc8Classic => source.ComputeCRC8(CRC8Type.Classic),
        Algorithm.Crc8Maxim => source.ComputeCRC8(CRC8Type.Maxim),
        Algorithm.Crc16Basic => source.ComputeCRC16(CRC16Type.Basic),
        Algorithm.Crc16Classic => source.ComputeCRC16(CRC16Type.Classic),
        Algorithm.Crc16Modbus => source.ComputeCRC16(CRC16Type.Modbus),
        Algorithm.Crc16Xmodem => source.ComputeCRC16(CRC16Type.CCITTxModem),
        Algorithm.Crc16Dnp => source.ComputeCRC16(CRC16Type.DNP),
        Algorithm.Crc32Basic => source.ComputeCRC32(CRC32Type.Basic),
        Algorithm.Crc32Classic => source.ComputeCRC32(CRC32Type.Classic),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static async Task<uint> ComputeAsync(Stream source, Algorithm algorithm,
        CancellationToken cancellationToken = default) => algorithm switch
    {
        Algorithm.Crc8Basic => await source.ComputeCRC8Async(CRC8Type.Basic, cancellationToken),
        Algorithm.Crc8Classic => await source.ComputeCRC8Async(CRC8Type.Classic, cancellationToken),
        Algorithm.Crc8Maxim => await source.ComputeCRC8Async(CRC8Type.Maxim, cancellationToken),
        Algorithm.Crc16Basic => await source.ComputeCRC16Async(CRC16Type.Basic, cancellationToken),
        Algorithm.Crc16Classic => await source.ComputeCRC16Async(CRC16Type.Classic, cancellationToken),
        Algorithm.Crc16Modbus => await source.ComputeCRC16Async(CRC16Type.Modbus, cancellationToken),
        Algorithm.Crc16Xmodem => await source.ComputeCRC16Async(CRC16Type.CCITTxModem, cancellationToken),
        Algorithm.Crc16Dnp => await source.ComputeCRC16Async(CRC16Type.DNP, cancellationToken),
        Algorithm.Crc32Basic => await source.ComputeCRC32Async(CRC32Type.Basic, cancellationToken),
        Algorithm.Crc32Classic => await source.ComputeCRC32Async(CRC32Type.Classic, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static async Task<uint> ComputeForModeAsync(Stream source, Algorithm algorithm, ReadMode mode)
        => mode == ReadMode.Synchronous ? Compute(source, algorithm) : await ComputeAsync(source, algorithm);

    private static async Task<uint> ComputeInvalidTypeAsync(Stream source, int width, int invalidType, ReadMode mode)
    {
        if (mode == ReadMode.Asynchronous)
            return width switch
            {
                8 => await source.ComputeCRC8Async((CRC8Type)invalidType),
                16 => await source.ComputeCRC16Async((CRC16Type)invalidType),
                32 => await source.ComputeCRC32Async((CRC32Type)invalidType),
                _ => throw new ArgumentOutOfRangeException(nameof(width))
            };

        return width switch
        {
            8 => source.ComputeCRC8((CRC8Type)invalidType),
            16 => source.ComputeCRC16((CRC16Type)invalidType),
            32 => source.ComputeCRC32((CRC32Type)invalidType),
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };
    }

    private static void AssertCompletedRead(ObservedReadStream source, ReadMode mode, long expectedBytes)
    {
        Assert.Equal(expectedBytes, source.BytesReturned);
        Assert.Equal(1, source.EndOfStreamReads);
        AssertReadMode(source, mode);
        AssertBufferUsage(source);
        Assert.False(source.IsDisposed);
        Assert.True(source.CanRead);
    }

    private static void AssertReadMode(ObservedReadStream source, ReadMode mode)
    {
        Assert.Equal(mode == ReadMode.Synchronous ? source.ReadCalls : 0, source.SynchronousReadCalls);
        Assert.Equal(mode == ReadMode.Asynchronous ? source.ReadCalls : 0, source.AsynchronousReadCalls);
    }

    private static void AssertBufferUsage(ObservedReadStream source)
    {
        Assert.NotNull(source.FirstBuffer);
        Assert.Equal(BufferSize, source.FirstBuffer!.Length);
        Assert.True(source.ReusedFirstBuffer);
        Assert.Equal(0, source.NonzeroOffsetReads);
        Assert.InRange(source.MaximumRequestedCount, 1, BufferSize);
    }

    private static IEnumerable<byte> GeneratedBytes(long length)
    {
        for (long position = 0; position < length; position++)
            yield return GeneratedByte(position);
    }

    private static byte GeneratedByte(long position)
        => (byte)((position * 73 + (position >> 3) + 19) & 255);

    private class ObservedReadStream : Stream
    {
        private readonly byte[]? payload;
        private readonly long length;
        private readonly int[] chunkSizes;
        private int chunkIndex;

        public ObservedReadStream(byte[] payload, int[]? chunkSizes = null)
            : this(payload.LongLength, chunkSizes)
        {
            this.payload = payload;
        }

        public ObservedReadStream(long length, int[]? chunkSizes = null)
        {
            this.length = length;
            this.chunkSizes = chunkSizes ?? new[] { BufferSize };
        }

        public bool Readable { get; init; } = true;
        public bool RejectSynchronousReads { get; init; }
        public IOException? Failure { get; init; }
        public int FailOnReadCall { get; init; } = int.MaxValue;
        public Action? AfterSuccessfulRead { get; init; }
        public bool IsDisposed { get; private set; }
        public int SynchronousReadCalls { get; private set; }
        public int AsynchronousReadCalls { get; private set; }
        public int ReadCalls => SynchronousReadCalls + AsynchronousReadCalls;
        public long BytesReturned { get; private set; }
        public int EndOfStreamReads { get; private set; }
        public byte[]? FirstBuffer { get; private set; }
        public bool ReusedFirstBuffer { get; private set; } = true;
        public int MaximumRequestedCount { get; private set; }
        public int NonzeroOffsetReads { get; private set; }
        public List<CancellationToken> ReadTokens { get; } = new();
        public override bool CanRead => Readable && !IsDisposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException("Length must not be queried.");
        public override long Position
        {
            get => throw new NotSupportedException("Position must not be queried.");
            set => throw new NotSupportedException("Position must not be changed.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            SynchronousReadCalls++;
            RecordBuffer(buffer, offset, count);
            if (RejectSynchronousReads)
                throw new InvalidOperationException("The asynchronous API called synchronous Read.");
            return ReadCore(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            RecordAsyncRead(buffer, offset, count, cancellationToken);
            // A genuine asynchronous completion also catches wrappers that
            // assume ReadAsync always returns an already-completed task.
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return ReadCore(buffer, offset, count);
        }

        protected void RecordAsyncRead(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            AsynchronousReadCalls++;
            ReadTokens.Add(cancellationToken);
            RecordBuffer(buffer, offset, count);
        }

        private void RecordBuffer(byte[] buffer, int offset, int count)
        {
            FirstBuffer ??= buffer;
            ReusedFirstBuffer &= ReferenceEquals(FirstBuffer, buffer);
            MaximumRequestedCount = Math.Max(MaximumRequestedCount, count);
            if (offset != 0)
                NonzeroOffsetReads++;
            Assert.InRange(count, 1, BufferSize);
        }

        protected int ReadCore(byte[] buffer, int offset, int count)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ObservedReadStream));
            if (ReadCalls == FailOnReadCall)
                throw Failure ?? new IOException("Injected read failure.");
            if (BytesReturned == length)
            {
                EndOfStreamReads++;
                return 0;
            }

            int chunkSize = chunkSizes[chunkIndex++ % chunkSizes.Length];
            int returnedCount = (int)Math.Min(length - BytesReturned, Math.Min(count, chunkSize));
            // Poison the unread tail to detect processing count instead of the
            // actual return value, including after a full previous chunk.
            Array.Fill(buffer, (byte)0xA5, offset, count);
            for (int index = 0; index < returnedCount; index++)
                buffer[offset + index] = payload is null
                    ? GeneratedByte(BytesReturned + index)
                    : payload[(int)BytesReturned + index];

            BytesReturned += returnedCount;
            AfterSuccessfulRead?.Invoke();
            return returnedCount;
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class BlockingReadStream : ObservedReadStream
    {
        public BlockingReadStream() : base(PublishedCheckInput)
        {
            RejectSynchronousReads = true;
        }

        public TaskCompletionSource<object?> ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            RecordAsyncRead(buffer, offset, count, cancellationToken);
            ReadStarted.TrySetResult(null);
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return 0;
        }
    }

    private sealed class ContinuationProbeStream : ObservedReadStream
    {
        private readonly TaskCompletionSource<int> firstReadCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int firstReadCount;

        public ContinuationProbeStream() : base(PublishedCheckInput)
        {
            RejectSynchronousReads = true;
        }

        public void CompleteFirstRead() => firstReadCompletion.SetResult(firstReadCount);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            RecordAsyncRead(buffer, offset, count, cancellationToken);
            int returnedCount = ReadCore(buffer, offset, count);
            if (AsynchronousReadCalls != 1)
                return Task.FromResult(returnedCount);

            firstReadCount = returnedCount;
            return firstReadCompletion.Task;
        }
    }

    private sealed class ObservedSynchronizationContext : SynchronizationContext
    {
        private int postCount;

        public int PostCount => Volatile.Read(ref postCount);

        public override void Post(SendOrPostCallback callback, object? state)
        {
            Interlocked.Increment(ref postCount);
            ThreadPool.QueueUserWorkItem(_ => callback(state));
        }
    }
}

#pragma warning restore CS0618
