using System.Text;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Crc8VariantStreamTests
{
    private const int BufferSize = 64 * 1024;
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    public static IEnumerable<object[]> Types()
    {
        yield return new object[] { CRC8Type.SaeJ1850 };
        yield return new object[] { CRC8Type.AutosarH2F };
    }

    public static IEnumerable<object[]> TypesAndModes()
    {
        foreach (object[] type in Types())
        foreach (bool asynchronous in new[] { false, true })
            yield return new object[] { type[0], asynchronous };
    }

    [Theory]
    [InlineData(CRC8Type.SaeJ1850, false, 0x4B)]
    [InlineData(CRC8Type.SaeJ1850, true, 0x4B)]
    [InlineData(CRC8Type.AutosarH2F, false, 0xDF)]
    [InlineData(CRC8Type.AutosarH2F, true, 0xDF)]
    public async Task PublishedChecksMatchThroughShortNonseekableReads(
        CRC8Type type, bool asynchronous, byte expected)
    {
        using var source = new ObservedStream(CheckInput, asynchronous, new[] { 1, 2, 1, 5 });

        Assert.Equal(expected, await Compute(source, type, asynchronous));

        Assert.Equal(CheckInput.Length, source.BytesConsumed);
        Assert.Equal(1, source.EndOfStreamReads);
        Assert.False(source.Disposed);
    }

    [Theory]
    [MemberData(nameof(TypesAndModes))]
    public async Task BufferBoundariesAndShortReadsMatchIndependentReferenceWithOneReusableBuffer(
        CRC8Type type, bool asynchronous)
    {
        foreach (int length in new[] { BufferSize - 1, BufferSize, BufferSize + 1, 2 * BufferSize + 17 })
        {
            byte[] input = BinaryInput(length);
            byte expected = CrcReference.Compute8(input, type);
            using var source = new ObservedStream(input, asynchronous, new[] { 1, 3, 17, 4096, BufferSize });

            Assert.Equal(expected, input.CRC8(type));
            Assert.Equal(expected, await Compute(source, type, asynchronous));

            Assert.Equal(length, source.BytesConsumed);
            Assert.Equal(1, source.EndOfStreamReads);
            Assert.True(source.ReadCalls > 1);
            Assert.NotNull(source.Buffer);
            Assert.False(source.Disposed);
        }
    }

    [Theory]
    [MemberData(nameof(TypesAndModes))]
    public async Task ComputationUsesCurrentPositionAndAllowsReusingTheOpenStream(
        CRC8Type type, bool asynchronous)
    {
        byte[] input = BinaryInput(BufferSize + 13);
        using var source = new MemoryStream(input, writable: false);
        foreach (int position in new[] { 19, input.Length - 1, 0 })
        {
            source.Position = position;
            byte expected = CrcReference.Compute8(input.Skip(position), type);

            Assert.Equal(expected, await Compute(source, type, asynchronous));

            Assert.Equal(input.Length, source.Position);
            Assert.True(source.CanRead);
        }
    }

    [Theory]
    [MemberData(nameof(TypesAndModes))]
    public async Task EmptyStreamKeepsTheCrc8NoDataExceptionAndLeavesSourceOpen(
        CRC8Type type, bool asynchronous)
    {
        using var source = new ObservedStream(Array.Empty<byte>(), asynchronous);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Compute(source, type, asynchronous));

        Assert.Equal(1, source.ReadCalls);
        Assert.Equal(0, source.BytesConsumed);
        Assert.Equal(1, source.EndOfStreamReads);
        Assert.False(source.Disposed);
    }

    [Theory]
    [MemberData(nameof(TypesAndModes))]
    public async Task InvalidSourcesAreRejectedBeforeReadingOrDisposing(
        CRC8Type type, bool asynchronous)
    {
        var nullFailure = await Assert.ThrowsAsync<ArgumentNullException>(
            () => Compute(null!, type, asynchronous));
        Assert.Equal("source", nullFailure.ParamName);
        using var source = new ObservedStream(CheckInput, asynchronous) { Readable = false };

        var unreadableFailure = await Assert.ThrowsAsync<ArgumentException>(
            () => Compute(source, type, asynchronous));

        Assert.Equal("source", unreadableFailure.ParamName);
        Assert.Equal(0, source.ReadCalls);
        Assert.Null(source.Buffer);
        Assert.False(source.Disposed);
    }

    [Theory]
    [MemberData(nameof(TypesAndModes))]
    public async Task FirstAndLaterReadFailuresPropagateOriginalExceptionAndLeaveSourceOpen(
        CRC8Type type, bool asynchronous)
    {
        foreach (int failingRead in new[] { 1, 3 })
        {
            using var source = new ObservedStream(BinaryInput(64), asynchronous, new[] { 7 })
            {
                FailOnRead = failingRead
            };

            var failure = await Assert.ThrowsAsync<IOException>(() => Compute(source, type, asynchronous));

            Assert.Same(source.Failure, failure);
            Assert.Equal(failingRead, source.ReadCalls);
            Assert.Equal(7 * (failingRead - 1), source.BytesConsumed);
            Assert.Equal(0, source.EndOfStreamReads);
            Assert.False(source.Disposed);
        }
    }

    [Theory]
    [MemberData(nameof(Types))]
    public async Task PreCanceledTokenStopsBeforeReadingAndLeavesSourceOpen(CRC8Type type)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var source = new ObservedStream(CheckInput, true);

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeCRC8Async(type, cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, source.ReadCalls);
        Assert.Null(source.Buffer);
        Assert.False(source.Disposed);
    }

    [Theory]
    [InlineData(CRC8Type.SaeJ1850, false)]
    [InlineData(CRC8Type.SaeJ1850, true)]
    [InlineData(CRC8Type.AutosarH2F, false)]
    [InlineData(CRC8Type.AutosarH2F, true)]
    public async Task CancellationInsideDataOrEofReadPreventsReturningACrc(CRC8Type type, bool cancelAtEnd)
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(CheckInput, true)
        {
            AfterRead = count =>
            {
                if ((count == 0) == cancelAtEnd) cancellation.Cancel();
            }
        };

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeCRC8Async(type, cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(CheckInput.Length, source.BytesConsumed);
        Assert.Equal(cancelAtEnd ? 2 : 1, source.ReadCalls);
        Assert.Equal(cancelAtEnd ? 1 : 0, source.EndOfStreamReads);
        Assert.All(source.ReadTokens, token => Assert.Equal(cancellation.Token, token));
        Assert.False(source.Disposed);
    }

    [Theory]
    [MemberData(nameof(Types))]
    public async Task PendingAsyncReadReceivesCancellationWithoutSynchronousFallbackOrClosing(CRC8Type type)
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(CheckInput, true) { WaitForCancellation = true };
        Task<byte> computation = source.ComputeCRC8Async(type, cancellation.Token);
        await source.ReadStarted.Task.WaitAsync(CompletionTimeout);
        Assert.False(computation.IsCompleted);

        cancellation.Cancel();
        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => computation.WaitAsync(CompletionTimeout));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(cancellation.Token, Assert.Single(source.ReadTokens));
        Assert.Equal(1, source.ReadCalls);
        Assert.Equal(0, source.BytesConsumed);
        Assert.False(source.Disposed);
    }

    [Fact]
    public async Task ConcurrentAlgorithmsKeepTheirBuffersAndStatesIndependent()
    {
        CRC8Type[] types = Enumerable.Range(0, 8)
            .Select(index => index % 2 == 0 ? CRC8Type.SaeJ1850 : CRC8Type.AutosarH2F).ToArray();
        byte[][] inputs = Enumerable.Range(0, types.Length).Select(index => BinaryInput(257 + index * 37)).ToArray();
        var sources = inputs.Select(input => new ObservedStream(input, true, new[] { 3, 127 })).ToArray();
        try
        {
            byte[] expected = inputs.Select((input, index) => CrcReference.Compute8(input, types[index])).ToArray();

            byte[] actual = await Task.WhenAll(sources.Select((source, index) => source.ComputeCRC8Async(types[index])))
                .WaitAsync(CompletionTimeout);

            Assert.Equal(expected, actual);
            Assert.Equal(sources.Length, sources.Select(source => source.Buffer).Distinct().Count());
            Assert.All(sources, source => Assert.False(source.Disposed));
        }
        finally
        {
            foreach (var source in sources) source.Dispose();
        }
    }

    private static Task<byte> Compute(Stream source, CRC8Type type, bool asynchronous)
        => asynchronous ? source.ComputeCRC8Async(type) : Task.FromResult(source.ComputeCRC8(type));

    private static byte[] BinaryInput(int length)
        => Enumerable.Range(0, length).Select(index => unchecked((byte)((index * 73) ^ (index >> 3) ^ 0xA7))).ToArray();

    private sealed class ObservedStream(byte[] bytes, bool asynchronous, int[]? chunks = null) : Stream
    {
        public bool Readable { get; init; } = true;
        public bool Disposed { get; private set; }
        public int BytesConsumed { get; private set; }
        public int ReadCalls { get; private set; }
        public int EndOfStreamReads { get; private set; }
        public byte[]? Buffer { get; private set; }
        public List<CancellationToken> ReadTokens { get; } = new();
        public int FailOnRead { get; init; } = -1;
        public IOException Failure { get; } = new("Injected CRC8 variant read failure.");
        public Action<int>? AfterRead { get; init; }
        public bool WaitForCancellation { get; init; }
        public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool CanRead => Readable && !Disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Assert.False(asynchronous, "The asynchronous API used synchronous Read.");
            ObserveRead(buffer, offset, count);
            return ReadCore(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            Assert.True(asynchronous, "The synchronous API used ReadAsync.");
            ObserveRead(buffer, offset, count);
            ReadTokens.Add(cancellationToken);
            if (WaitForCancellation)
            {
                ReadStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return ReadCore(buffer, offset, count);
        }

        private void ObserveRead(byte[] buffer, int offset, int count)
        {
            Assert.True(CanRead);
            Assert.Equal(0, offset);
            Assert.Equal(BufferSize, count);
            Assert.Equal(BufferSize, buffer.Length);
            Buffer ??= buffer;
            Assert.Same(Buffer, buffer);
            if (++ReadCalls == FailOnRead) throw Failure;
        }

        private int ReadCore(byte[] buffer, int offset, int count)
        {
            buffer.AsSpan(offset, count).Fill(0xE7); // Bytes outside the returned prefix must never enter the CRC.
            int chunk = chunks == null ? BufferSize : chunks[(ReadCalls - 1) % chunks.Length];
            int returned = Math.Min(Math.Min(chunk, count), bytes.Length - BytesConsumed);
            bytes.AsSpan(BytesConsumed, returned).CopyTo(buffer.AsSpan(offset, returned));
            BytesConsumed += returned;
            if (returned == 0) EndOfStreamReads++;
            AfterRead?.Invoke(returned);
            return returned;
        }

        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
