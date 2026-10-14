using System.Text;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Crc32CStreamTests
{
    private const int BufferSize = 64 * 1024;
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    public enum ReadMode
    {
        Synchronous,
        Asynchronous
    }

    public static IEnumerable<object[]> PublishedVectorsAndModes()
    {
        // RFC 7143 A.4 publishes digest octets in iSCSI wire order. These
        // constants are the corresponding reflected CRC32C numeric values.
        // https://www.rfc-editor.org/rfc/rfc7143.html#appendix-A.4
        (byte[] Input, uint Expected)[] vectors =
        {
            (new byte[32], 0x8A9136AA),
            (Enumerable.Repeat((byte)0xFF, 32).ToArray(), 0x62A8AB43),
            (Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(), 0x46DD794E),
            (Enumerable.Range(0, 32).Reverse().Select(value => (byte)value).ToArray(), 0x113FDB5C),
            (Convert.FromHexString(
                "01C0000000000000000000000000000014000000000004000000001400000018" +
                "28000000000000000200000000000000"), 0xD9963A56),
            // CRC RevEng CRC-32/ISCSI check value for ASCII "123456789".
            // https://reveng.sourceforge.io/crc-catalogue/17plus.htm#crc.cat.crc-32-iscsi
            (CheckInput, 0xE3069283)
        };

        foreach (var vector in vectors)
        foreach (ReadMode mode in Enum.GetValues<ReadMode>())
            yield return new object[] { vector.Input, vector.Expected, mode };
    }

    public static IEnumerable<object[]> BoundaryLengthsAndModes()
    {
        int[] lengths =
        {
            1, 2, 7, 31, 256, 257, 4097,
            BufferSize - 1, BufferSize, BufferSize + 1,
            2 * BufferSize - 1, 2 * BufferSize, 2 * BufferSize + 17
        };
        foreach (int length in lengths)
        foreach (ReadMode mode in Enum.GetValues<ReadMode>())
            yield return new object[] { length, mode };
    }

    [Theory]
    [MemberData(nameof(PublishedVectorsAndModes))]
    public async Task PublishedVectors_ProduceFixedAnswersWithShortNonseekableReads(
        byte[] input, uint expected, ReadMode mode)
    {
        using var source = CreateObserved(input, mode, new[] { 1, 2, 1, 5 });

        Assert.Equal(expected, Crc32CReference.Compute(input));
        Assert.Equal(expected, await ComputeForModeAsync(source, mode));

        AssertCompletedRead(source, mode, input.Length);
    }

    [Theory]
    [MemberData(nameof(BoundaryLengthsAndModes))]
    public async Task BufferBoundariesAndPartialReadPatterns_MatchIndependentAndOneShotCalculations(
        int length, ReadMode mode)
    {
        byte[] input = GeneratedBytes(length);
        uint expected = Crc32CReference.Compute(input);
        Assert.Equal(expected, input.CRC32C());
        int[][] patterns =
        {
            new[] { 1 }, new[] { 3 }, new[] { 31 }, new[] { 4096 },
            new[] { BufferSize }, new[] { BufferSize, 1, 7 },
            new[] { 1, 31, 3, 8191, 64, BufferSize, 5 }
        };

        foreach (int[] pattern in patterns)
        {
            using var source = CreateObserved(input, mode, pattern);

            Assert.Equal(expected, await ComputeForModeAsync(source, mode));

            AssertCompletedRead(source, mode, length);
        }
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task EveryShortInputSplit_MatchesWholeInputAndIndependentCalculation(ReadMode mode)
    {
        foreach (int length in new[] { 2, 3, 7, 31, 127, 257 })
        {
            byte[] input = GeneratedBytes(length);
            uint expected = Crc32CReference.Compute(input);
            for (int split = 1; split < input.Length; split++)
            {
                using var source = CreateObserved(input, mode, new[] { split, input.Length - split });

                Assert.Equal(expected, await ComputeForModeAsync(source, mode));

                AssertCompletedRead(source, mode, length);
                Assert.Equal(3, source.ReadCalls);
            }
        }
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task SeededRandomReadPartitions_MatchIndependentCalculation(ReadMode mode)
    {
        byte[] input = GeneratedBytes(3 * BufferSize + 53);
        uint expected = Crc32CReference.Compute(input);
        var random = new Random(0x32C7143);
        for (int iteration = 0; iteration < 24; iteration++)
        {
            int[] pattern = Enumerable.Range(0, 19).Select(_ => random.Next(1, BufferSize + 1)).ToArray();
            using var source = CreateObserved(input, mode, pattern);

            Assert.Equal(expected, await ComputeForModeAsync(source, mode));

            AssertCompletedRead(source, mode, input.Length);
        }
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task GeneratedLargeStream_ReadsOnceWithOneBoundedBuffer(ReadMode mode)
    {
        const long length = 8L * 1024 * 1024 + 17;
        uint expected = ComputeGeneratedReference(length);
        using var source = new ObservedReadStream(length)
        {
            RejectSynchronousReads = mode == ReadMode.Asynchronous
        };

        Assert.Equal(expected, await ComputeForModeAsync(source, mode));

        AssertCompletedRead(source, mode, length);
        Assert.Equal((length + BufferSize - 1) / BufferSize + 1, source.ReadCalls);
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task CurrentPositionAndReusedStream_ComputeOnlyRemainingBytes(ReadMode mode)
    {
        byte[] input = GeneratedBytes(BufferSize + 37);
        using var source = new MemoryStream(input, writable: false);
        foreach (int position in new[] { 123, input.Length - 1, input.Length, 0 })
        {
            source.Position = position;
            uint expected = Crc32CReference.Compute(input.AsSpan(position));

            Assert.Equal(expected, await ComputeForModeAsync(source, mode));

            Assert.Equal(input.Length, source.Position);
            Assert.True(source.CanRead);
        }
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task EmptyStream_ReturnsZeroAndLeavesStreamOpen(ReadMode mode)
    {
        using var source = CreateObserved(Array.Empty<byte>(), mode);

        Assert.Equal(0u, await ComputeForModeAsync(source, mode));

        AssertCompletedRead(source, mode, 0);
        Assert.Equal(1, source.ReadCalls);
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task NullStream_ReportsSourceParameter(ReadMode mode)
    {
        var failure = await Assert.ThrowsAsync<ArgumentNullException>(
            () => ComputeForModeAsync(null!, mode));

        Assert.Equal("source", failure.ParamName);
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task UnreadableStream_IsRejectedWithoutReadingOrClosing(ReadMode mode)
    {
        using var source = new ObservedReadStream(CheckInput) { Readable = false };

        var failure = await Assert.ThrowsAsync<ArgumentException>(
            () => ComputeForModeAsync(source, mode));

        Assert.Equal("source", failure.ParamName);
        Assert.Equal(0, source.ReadCalls);
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task DisposedStream_IsRejectedWithoutReading(ReadMode mode)
    {
        var source = new ObservedReadStream(CheckInput);
        source.Dispose();

        var failure = await Assert.ThrowsAsync<ArgumentException>(
            () => ComputeForModeAsync(source, mode));

        Assert.Equal("source", failure.ParamName);
        Assert.Equal(0, source.ReadCalls);
        Assert.True(source.IsDisposed);
    }

    [Theory]
    [InlineData(ReadMode.Synchronous, 1)]
    [InlineData(ReadMode.Synchronous, 3)]
    [InlineData(ReadMode.Asynchronous, 1)]
    [InlineData(ReadMode.Asynchronous, 3)]
    public async Task ReadFailure_PropagatesOriginalExceptionAndLeavesStreamOpen(ReadMode mode, int failingRead)
    {
        var injectedFailure = new IOException("Injected CRC32C stream read failure.");
        using var source = new ObservedReadStream(64, new[] { 7 })
        {
            RejectSynchronousReads = mode == ReadMode.Asynchronous,
            Failure = injectedFailure,
            FailOnReadCall = failingRead
        };

        var failure = await Assert.ThrowsAsync<IOException>(() => ComputeForModeAsync(source, mode));

        Assert.Same(injectedFailure, failure);
        Assert.Equal(failingRead, source.ReadCalls);
        Assert.Equal((failingRead - 1) * 7, source.BytesReturned);
        Assert.Equal(0, source.EndOfStreamReads);
        AssertReadMode(source, mode);
        AssertBufferUsage(source);
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [InlineData(ReadMode.Synchronous)]
    [InlineData(ReadMode.Asynchronous)]
    public async Task VirtualLengthBeyondInt32_DoesNotQueryLengthOrPreallocateWholeInput(ReadMode mode)
    {
        var injectedFailure = new IOException("Stop the virtual multi-gigabyte input after two chunks.");
        using var source = new ObservedReadStream((long)int.MaxValue + BufferSize)
        {
            RejectSynchronousReads = mode == ReadMode.Asynchronous,
            Failure = injectedFailure,
            FailOnReadCall = 3
        };

        var failure = await Assert.ThrowsAsync<IOException>(() => ComputeForModeAsync(source, mode));

        Assert.Same(injectedFailure, failure);
        Assert.Equal(2L * BufferSize, source.BytesReturned);
        Assert.Equal(3, source.ReadCalls);
        AssertReadMode(source, mode);
        AssertBufferUsage(source);
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [InlineData(ReadMode.Synchronous, -1)]
    [InlineData(ReadMode.Synchronous, BufferSize + 1)]
    [InlineData(ReadMode.Asynchronous, -1)]
    [InlineData(ReadMode.Asynchronous, BufferSize + 1)]
    public async Task InvalidReadCount_ThrowsWithoutReturningADigestOrClosing(ReadMode mode, int invalidCount)
    {
        using var source = new InvalidReadCountStream(invalidCount)
        {
            RejectSynchronousReads = mode == ReadMode.Asynchronous
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ComputeForModeAsync(source, mode));

        Assert.Equal(1, source.ReadCalls);
        AssertReadMode(source, mode);
        Assert.False(source.IsDisposed);
    }

    [Fact]
    public async Task ConcurrentAsyncComputations_KeepBuffersAndCrcStateIndependent()
    {
        Task[] computations = Enumerable.Range(0, 32).Select(async worker =>
        {
            byte[] input = GeneratedBytes(4097 + worker * 37);
            for (int index = 0; index < input.Length; index++)
                input[index] ^= (byte)worker;
            uint expected = Crc32CReference.Compute(input);
            using var source = CreateObserved(input, ReadMode.Asynchronous, new[] { 1, 127, 7 });

            Assert.Equal(expected, await source.ComputeCRC32CAsync());

            AssertCompletedRead(source, ReadMode.Asynchronous, input.Length);
        }).ToArray();

        await Task.WhenAll(computations).WaitAsync(CompletionTimeout);
    }

    [Fact]
    public async Task AsyncReads_ForwardProvidedTokenOnEveryReadIncludingEndOfStream()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = CreateObserved(CheckInput, ReadMode.Asynchronous, new[] { 2 });

        uint actual = await source.ComputeCRC32CAsync(cancellation.Token);

        Assert.Equal(0xE3069283u, actual);
        AssertCompletedRead(source, ReadMode.Asynchronous, CheckInput.Length);
        Assert.Equal(source.ReadCalls, source.ReadTokens.Count);
        Assert.All(source.ReadTokens, token => Assert.Equal(cancellation.Token, token));
    }

    [Fact]
    public async Task PreCanceledToken_CancelsBeforeReadingAndLeavesStreamOpen()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var source = CreateObserved(CheckInput, ReadMode.Asynchronous);

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeCRC32CAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, source.ReadCalls);
        Assert.Null(source.FirstBuffer);
        Assert.False(source.IsDisposed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task CancellationInsidePartialRead_StopsWithoutReturningDigestOrClosing(int cancelOnRead)
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedReadStream(1024, new[] { 17 })
        {
            RejectSynchronousReads = true,
            AfterRead = (readCall, _) =>
            {
                if (readCall == cancelOnRead) cancellation.Cancel();
            }
        };

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeCRC32CAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(cancelOnRead * 17, source.BytesReturned);
        Assert.Equal(cancelOnRead, source.ReadCalls);
        Assert.Equal(0, source.EndOfStreamReads);
        AssertReadMode(source, ReadMode.Asynchronous);
        AssertBufferUsage(source);
        Assert.All(source.ReadTokens, token => Assert.Equal(cancellation.Token, token));
        Assert.False(source.IsDisposed);
    }

    [Fact]
    public async Task CancellationInsideEndOfStreamRead_DoesNotReturnACompletedDigest()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedReadStream(CheckInput)
        {
            RejectSynchronousReads = true,
            AfterRead = (_, count) =>
            {
                if (count == 0) cancellation.Cancel();
            }
        };

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeCRC32CAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        AssertCompletedRead(source, ReadMode.Asynchronous, CheckInput.Length);
        Assert.Equal(2, source.ReadCalls);
    }

    [Fact]
    public async Task BlockingAsyncRead_ReceivesTokenAndCancelsWithoutSynchronousReadOrClose()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new BlockingReadStream();
        Task<uint> computation = source.ComputeCRC32CAsync(cancellation.Token);

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

    [Fact]
    public async Task ReaderCancellation_PropagatesOriginalExceptionWithoutClosing()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var injectedFailure = new OperationCanceledException(cancellation.Token);
        using var source = new ObservedReadStream(CheckInput)
        {
            RejectSynchronousReads = true,
            Failure = injectedFailure,
            FailOnReadCall = 1
        };

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeCRC32CAsync());

        Assert.Same(injectedFailure, failure);
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        AssertReadMode(source, ReadMode.Asynchronous);
        Assert.False(source.IsDisposed);
    }

    [Fact]
    public async Task CanceledRead_LeavesPositionConsumedAndAllowsExplicitRemainingInputCalculation()
    {
        byte[] input = GeneratedBytes(91);
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedReadStream(input, new[] { 17 })
        {
            RejectSynchronousReads = true,
            AfterRead = (readCall, _) =>
            {
                if (readCall == 1) cancellation.Cancel();
            }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeCRC32CAsync(cancellation.Token));
        Assert.Equal(17, source.BytesReturned);
        Assert.False(source.IsDisposed);

        Assert.Equal(Crc32CReference.Compute(input.AsSpan(17)), await source.ComputeCRC32CAsync());
        Assert.Equal(input.Length, source.BytesReturned);
        Assert.False(source.IsDisposed);
    }

    [Fact]
    public async Task AsyncContinuation_DoesNotCaptureCallingSynchronizationContext()
    {
        using var source = new ContinuationProbeStream();
        var context = new ObservedSynchronizationContext();
        SynchronizationContext? originalContext = SynchronizationContext.Current;
        Task<uint> computation;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            computation = source.ComputeCRC32CAsync();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(originalContext);
        }

        Assert.False(computation.IsCompleted);
        source.CompleteFirstRead();

        Assert.Equal(0xE3069283u, await computation.WaitAsync(CompletionTimeout));
        Assert.Equal(0, context.PostCount);
        AssertCompletedRead(source, ReadMode.Asynchronous, CheckInput.Length);
    }

    private static ObservedReadStream CreateObserved(byte[] input, ReadMode mode, int[]? chunks = null)
        => new(input, chunks) { RejectSynchronousReads = mode == ReadMode.Asynchronous };

    private static async Task<uint> ComputeForModeAsync(Stream source, ReadMode mode)
        => mode == ReadMode.Synchronous ? source.ComputeCRC32C() : await source.ComputeCRC32CAsync();

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

    private static byte[] GeneratedBytes(int length)
    {
        var result = new byte[length];
        for (int position = 0; position < length; position++)
            result[position] = GeneratedByte(position);
        return result;
    }

    private static uint ComputeGeneratedReference(long length)
    {
        // The independently implemented oracle and fixture both use bounded
        // memory; neither creates a byte array the size of the input stream.
        var buffer = new byte[4096];
        uint register = Crc32CReference.InitialRegister;
        for (long position = 0; position < length; position += buffer.Length)
        {
            int count = (int)Math.Min(buffer.Length, length - position);
            for (int index = 0; index < count; index++)
                buffer[index] = GeneratedByte(position + index);
            register = Crc32CReference.Update(register, buffer.AsSpan(0, count));
        }
        return Crc32CReference.Finish(register);
    }

    private static byte GeneratedByte(long position)
        => (byte)((position * 73 + (position >> 3) + 19) & 255);

    private class ObservedReadStream : Stream
    {
        private readonly byte[]? payload;
        private readonly long length;
        private readonly int[] chunkSizes;
        private int chunkIndex;
        private int previousReturnedCount;
        private bool initializedBuffer;

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
        public Exception? Failure { get; init; }
        public int FailOnReadCall { get; init; } = int.MaxValue;
        public Action<int, int>? AfterRead { get; init; }
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
        public override long Length => throw new NotSupportedException("CRC32C must not query stream length.");
        public override long Position
        {
            get => throw new NotSupportedException("CRC32C must not query stream position.");
            set => throw new NotSupportedException("CRC32C must not change stream position.");
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
            // Force actual asynchronous completion, rather than only exercising
            // the already-completed MemoryStream ReadAsync implementation.
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
            if (offset != 0) NonzeroOffsetReads++;
            Assert.InRange(count, 1, BufferSize);
        }

        protected int ReadCore(byte[] buffer, int offset, int count)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(ObservedReadStream));
            if (ReadCalls == FailOnReadCall) throw Failure ?? new IOException("Injected read failure.");
            if (BytesReturned == length)
            {
                EndOfStreamReads++;
                AfterRead?.Invoke(ReadCalls, 0);
                return 0;
            }

            int chunkSize = chunkSizes[chunkIndex++ % chunkSizes.Length];
            int returnedCount = (int)Math.Min(length - BytesReturned, Math.Min(count, chunkSize));
            // Keep the unread tail poisoned without refilling 64 KiB for every
            // one-byte read. Also erase bytes left by a previous longer read.
            if (!initializedBuffer)
            {
                Array.Fill(buffer, (byte)0xA5, offset, count);
                initializedBuffer = true;
            }
            else if (returnedCount < previousReturnedCount)
                Array.Fill(buffer, (byte)0xA5, offset + returnedCount, previousReturnedCount - returnedCount);
            for (int index = 0; index < returnedCount; index++)
                buffer[offset + index] = payload is null
                    ? GeneratedByte(BytesReturned + index)
                    : payload[(int)BytesReturned + index];
            BytesReturned += returnedCount;
            previousReturnedCount = returnedCount;
            AfterRead?.Invoke(ReadCalls, returnedCount);
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

    private sealed class InvalidReadCountStream : ObservedReadStream
    {
        private readonly int returnedCount;

        public InvalidReadCountStream(int returnedCount) : base(CheckInput)
        {
            this.returnedCount = returnedCount;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            base.Read(buffer, offset, count);
            return returnedCount;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            await base.ReadAsync(buffer, offset, count, cancellationToken);
            return returnedCount;
        }
    }

    private sealed class BlockingReadStream : ObservedReadStream
    {
        public BlockingReadStream() : base(CheckInput)
        {
            RejectSynchronousReads = true;
        }

        public TaskCompletionSource<object?> ReadStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

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
        private readonly TaskCompletionSource<int> firstReadCompletion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int firstReadCount;

        public ContinuationProbeStream() : base(CheckInput)
        {
            RejectSynchronousReads = true;
        }

        public void CompleteFirstRead() => firstReadCompletion.SetResult(firstReadCount);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            RecordAsyncRead(buffer, offset, count, cancellationToken);
            int returnedCount = ReadCore(buffer, offset, count);
            if (AsynchronousReadCalls != 1) return Task.FromResult(returnedCount);
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
