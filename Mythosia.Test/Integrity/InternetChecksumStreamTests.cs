using System.Runtime.InteropServices;
using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class InternetChecksumStreamTests
{
    private const int BufferSize = 64 * 1024;
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    public static IEnumerable<object[]> BoundaryLengthsAndModes()
    {
        foreach (int length in new[] { 0, 1, 2, 3, 65535, 65536, 65537, 2 * BufferSize + 17 })
        foreach (bool asynchronous in new[] { false, true })
            yield return new object[] { length, asynchronous };
    }

    [Theory]
    [MemberData(nameof(BoundaryLengthsAndModes))]
    public async Task ShortNonseekableReadsMatchIndependentChecksumAndReuseOneBoundedBuffer(
        int length, bool asynchronous)
    {
        byte[] input = InternetChecksumTests.BinaryInput(length);
        ushort expected = InternetChecksumReference.Compute(input);
        using var source = new ObservedStream(input, asynchronous, new[] { 1, 3, 17, 4096, BufferSize });

        Assert.Equal(expected, await Compute(source, asynchronous));

        Assert.Equal(length, source.BytesConsumed);
        Assert.Equal(1, source.EndOfStreamReads);
        Assert.False(source.Disposed);
        Assert.NotNull(source.Buffer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComputationStartsAtCurrentPositionAndAllowsStreamReuse(bool asynchronous)
    {
        byte[] input = InternetChecksumTests.BinaryInput(BufferSize + 13);
        using var source = new MemoryStream(input, writable: false);
        foreach (int position in new[] { 19, input.Length - 1, input.Length, 0 })
        {
            source.Position = position;
            ushort expected = InternetChecksumReference.Compute(input.AsSpan(position));

            Assert.Equal(expected, await Compute(source, asynchronous));

            Assert.Equal(input.Length, source.Position);
            Assert.True(source.CanRead);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidSourcesAreRejectedWithoutReadOrImplicitDispose(bool asynchronous)
    {
        var nullFailure = await Assert.ThrowsAsync<ArgumentNullException>(() => Compute(null!, asynchronous));
        Assert.Equal("source", nullFailure.ParamName);
        using var unreadable = new ObservedStream(new byte[1], asynchronous) { Readable = false };

        var failure = await Assert.ThrowsAsync<ArgumentException>(() => Compute(unreadable, asynchronous));

        Assert.Equal("source", failure.ParamName);
        Assert.Equal(0, unreadable.ReadCalls);
        Assert.False(unreadable.Disposed);

        unreadable.Readable = true;
        unreadable.Dispose();
        failure = await Assert.ThrowsAsync<ArgumentException>(() => Compute(unreadable, asynchronous));
        Assert.Equal("source", failure.ParamName);
        Assert.Equal(0, unreadable.ReadCalls);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    [InlineData(true, 3)]
    public async Task ReadFailurePropagatesOriginalExceptionAndLeavesSourceOpen(bool asynchronous, int failingRead)
    {
        using var source = new ObservedStream(new byte[64], asynchronous, new[] { 7 })
        {
            FailOnRead = failingRead
        };

        var failure = await Assert.ThrowsAsync<IOException>(() => Compute(source, asynchronous));

        Assert.Same(source.Failure, failure);
        Assert.Equal(failingRead, source.ReadCalls);
        Assert.Equal(7 * (failingRead - 1), source.BytesConsumed);
        Assert.False(source.Disposed);
    }

    [Fact]
    public async Task AsyncReadsForwardProvidedTokenThroughEndOfStream()
    {
        byte[] input = InternetChecksumTests.BinaryInput(29);
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(input, true, new[] { 3 });

        Assert.Equal(InternetChecksumReference.Compute(input), await source.ComputeInternetChecksumAsync(cancellation.Token));

        Assert.Equal(source.ReadCalls, source.Tokens.Count);
        Assert.All(source.Tokens, token => Assert.Equal(cancellation.Token, token));
        Assert.Equal(1, source.EndOfStreamReads);
        Assert.False(source.Disposed);
    }

    [Fact]
    public async Task PreCanceledTokenStopsBeforeReadingAndLeavesSourceOpen()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var source = new ObservedStream(new byte[1], true);

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeInternetChecksumAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, source.ReadCalls);
        Assert.Null(source.Buffer);
        Assert.False(source.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationInsideDataOrEndOfStreamReadDoesNotReturnChecksum(bool cancelAtEnd)
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(new byte[5], true)
        {
            AfterRead = count =>
            {
                if ((count == 0) == cancelAtEnd) cancellation.Cancel();
            }
        };

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ComputeInternetChecksumAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(5, source.BytesConsumed);
        Assert.Equal(cancelAtEnd ? 2 : 1, source.ReadCalls);
        Assert.Equal(cancelAtEnd ? 1 : 0, source.EndOfStreamReads);
        Assert.False(source.Disposed);
    }

    [Fact]
    public async Task GenuinePendingAsyncReadCancelsWithoutSynchronousFallbackOrClosingStream()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(new byte[1], true) { WaitForCancellation = true };
        Task<ushort> computation = source.ComputeInternetChecksumAsync(cancellation.Token);
        await source.ReadStarted.Task.WaitAsync(CompletionTimeout);
        Assert.False(computation.IsCompleted);

        cancellation.Cancel();
        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => computation.WaitAsync(CompletionTimeout));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(cancellation.Token, Assert.Single(source.Tokens));
        Assert.Equal(1, source.ReadCalls);
        Assert.Equal(0, source.BytesConsumed);
        Assert.False(source.Disposed);
    }

    [Fact]
    public async Task ConcurrentAsyncComputationsKeepTheirBuffersAndChecksumStatesIndependent()
    {
        var sources = Enumerable.Range(0, 8).Select(index =>
            new ObservedStream(InternetChecksumTests.BinaryInput(257 + index * 113), true, new[] { 3, 127 })).ToArray();
        try
        {
            ushort[] expected = Enumerable.Range(0, sources.Length)
                .Select(index => InternetChecksumReference.Compute(InternetChecksumTests.BinaryInput(257 + index * 113))).ToArray();

            ushort[] actual = await Task.WhenAll(sources.Select(source => source.ComputeInternetChecksumAsync()))
                .WaitAsync(CompletionTimeout);

            Assert.Equal(expected, actual);
            Assert.Equal(sources.Length, sources.Select(source => source.Buffer).Distinct().Count());
            Assert.All(sources, source => Assert.False(source.Disposed));
        }
        finally { foreach (var source in sources) source.Dispose(); }
    }

    private static Task<ushort> Compute(Stream source, bool asynchronous)
        => asynchronous ? source.ComputeInternetChecksumAsync() : Task.FromResult(source.ComputeInternetChecksum());

    private sealed class ObservedStream(byte[] bytes, bool asynchronous, int[]? chunks = null) : Stream
    {
        public bool Readable { get; set; } = true;
        public bool Disposed { get; private set; }
        public int BytesConsumed { get; private set; }
        public int ReadCalls { get; private set; }
        public int EndOfStreamReads { get; private set; }
        public byte[]? Buffer { get; private set; }
        public List<CancellationToken> Tokens { get; } = new();
        public int FailOnRead { get; init; } = -1;
        public IOException Failure { get; } = new("Injected Internet Checksum stream read failure.");
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
            return ReadCore(buffer.AsMemory(offset, count));
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Assert.True(asynchronous, "The synchronous API used ReadAsync.");
            Tokens.Add(cancellationToken);
            if (WaitForCancellation)
            {
                ObserveBuffer(buffer);
                ReadCalls++;
                ReadStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return ReadCore(buffer);
        }

        private int ReadCore(Memory<byte> buffer)
        {
            Assert.True(CanRead);
            ObserveBuffer(buffer);
            if (++ReadCalls == FailOnRead) throw Failure;
            buffer.Span.Fill(0xE7); // Only the returned prefix may affect the checksum, including at EOF.
            int chunk = chunks == null ? BufferSize : chunks[(ReadCalls - 1) % chunks.Length];
            int count = Math.Min(Math.Min(chunk, buffer.Length), bytes.Length - BytesConsumed);
            bytes.AsMemory(BytesConsumed, count).CopyTo(buffer);
            BytesConsumed += count;
            if (count == 0) EndOfStreamReads++;
            AfterRead?.Invoke(count);
            return count;
        }

        private void ObserveBuffer(Memory<byte> buffer)
        {
            Assert.Equal(BufferSize, buffer.Length);
            Assert.True(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment));
            Assert.Equal(0, segment.Offset);
            Buffer ??= segment.Array;
            Assert.Same(Buffer, segment.Array);
            Assert.Equal(BufferSize, Buffer!.Length);
        }

        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
