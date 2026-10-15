using System.Runtime.InteropServices;
using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class XxHashStreamTests
{
    public static IEnumerable<object[]> BoundaryModes()
    {
        foreach (int width in new[] { 32, 64 })
        foreach (bool asynchronous in new[] { false, true })
        foreach (int length in new[] { 0, 15, 16, 17, 31, 32, 33, 65535, 65536, 65537, 131089 })
            yield return new object[] { width, asynchronous, length };
    }

    [Theory]
    [MemberData(nameof(BoundaryModes))]
    public async Task ShortNonseekableReadsMatchIndependentHashAndReuseOneBuffer(int width, bool asynchronous, int length)
    {
        byte[] input = XxHashTests.BinaryInput(length);
        ulong seed = width == 32 ? 0xFEDCBA98u : 0xFEDCBA9876543210ul;
        using var source = new ObservedStream(input, asynchronous);
        using var cancellation = new CancellationTokenSource();
        Assert.Equal(XxHashReference.Compute(input, width, seed), await Compute(source, width, asynchronous, seed, cancellation.Token));
        Assert.Equal(input.Length, source.BytesRead);
        Assert.Equal(1, source.EndReads);
        Assert.False(source.Disposed);
        Assert.NotNull(source.Buffer);
        if (asynchronous) Assert.All(source.Tokens, token => Assert.Equal(cancellation.Token, token));
    }

    [Theory]
    [InlineData(32, false)]
    [InlineData(32, true)]
    [InlineData(64, false)]
    [InlineData(64, true)]
    public async Task CurrentPositionDefaultSeedAndRepeatedUseAreRespected(int width, bool asynchronous)
    {
        byte[] input = XxHashTests.BinaryInput(105);
        using var source = new MemoryStream(input);
        foreach (int offset in new[] { 7, 104, 105, 0 })
        {
            source.Position = offset;
            ulong expected = XxHashReference.Compute(input.AsSpan(offset), width);
            ulong actual = width == 32
                ? asynchronous ? await source.ComputeXxHash32Async() : source.ComputeXxHash32()
                : asynchronous ? await source.ComputeXxHash64Async() : source.ComputeXxHash64();
            Assert.Equal(expected, actual);
            Assert.Equal(input.Length, source.Position);
            Assert.True(source.CanRead);
        }
    }

    [Theory]
    [InlineData(32, false)]
    [InlineData(32, true)]
    [InlineData(64, false)]
    [InlineData(64, true)]
    public async Task InvalidSourcesAndReadFailuresPropagateWithoutDisposal(int width, bool asynchronous)
    {
        Assert.Equal("source", (await Assert.ThrowsAsync<ArgumentNullException>(() => Compute(null!, width, asynchronous))).ParamName);
        using var unreadable = new ObservedStream(new byte[2], asynchronous) { Readable = false };
        Assert.Equal("source", (await Assert.ThrowsAsync<ArgumentException>(() => Compute(unreadable, width, asynchronous))).ParamName);
        Assert.Equal(0, unreadable.ReadCalls);
        Assert.False(unreadable.Disposed);
        foreach (int failingRead in new[] { 1, 3 })
        {
            using var source = new ObservedStream(new byte[128], asynchronous) { FailOnRead = failingRead };
            Assert.Same(source.Failure, await Assert.ThrowsAsync<IOException>(() => Compute(source, width, asynchronous)));
            Assert.Equal(failingRead, source.ReadCalls);
            Assert.False(source.Disposed);
        }
    }

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public async Task PreCanceledTokensPreventAnyRead(int width)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var source = new ObservedStream(new byte[8], true);
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compute(source, width, true, cancellationToken: cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, source.ReadCalls);
        Assert.Null(source.Buffer);
        Assert.False(source.Disposed);
    }

    [Theory]
    [InlineData(32, false)]
    [InlineData(32, true)]
    [InlineData(64, false)]
    [InlineData(64, true)]
    public async Task CancellationDuringDataOrEndReadPreventsReturningHash(int width, bool atEnd)
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(new byte[1], true)
        {
            AfterRead = count => { if ((count == 0) == atEnd) cancellation.Cancel(); }
        };
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compute(source, width, true, cancellationToken: cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(atEnd ? 2 : 1, source.ReadCalls);
        Assert.False(source.Disposed);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public async Task PendingMemoryAsyncReadCanBeCanceledAndNeverFallsBackToSync(int width)
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(new byte[7], true) { WaitForCancellation = true };
        Task<ulong> computation = Compute(source, width, true, cancellationToken: cancellation.Token);
        await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(computation.IsCompleted);
        cancellation.Cancel();
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => computation.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.False(source.Disposed);
    }

    private static async Task<ulong> Compute(Stream source, int width, bool asynchronous,
        ulong seed = 0, CancellationToken cancellationToken = default)
    {
        if (width == 32)
            return asynchronous ? await source.ComputeXxHash32Async((uint)seed, cancellationToken) : source.ComputeXxHash32((uint)seed);
        return asynchronous ? await source.ComputeXxHash64Async(seed, cancellationToken) : source.ComputeXxHash64(seed);
    }

    private sealed class ObservedStream : Stream
    {
        private readonly byte[] _input;
        private readonly bool _asynchronous;
        private readonly int[] _sizes = { 1, 7, 15, 17, 31, 33, 4093, 65536 };
        internal ObservedStream(byte[] input, bool asynchronous) { _input = input; _asynchronous = asynchronous; }
        internal bool Readable { get; set; } = true;
        internal bool Disposed { get; private set; }
        internal int BytesRead { get; private set; }
        internal int ReadCalls { get; private set; }
        internal int EndReads { get; private set; }
        internal byte[]? Buffer { get; private set; }
        internal int FailOnRead { get; set; }
        internal IOException Failure { get; } = new("Observed stream failed.");
        internal List<CancellationToken> Tokens { get; } = new();
        internal Action<int>? AfterRead { get; set; }
        internal bool WaitForCancellation { get; set; }
        internal TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override bool CanRead => Readable && !Disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            Assert.False(_asynchronous);
            Observe(buffer.AsMemory(offset, count));
            return Next(buffer.AsSpan(offset, count));
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Assert.True(_asynchronous);
            Observe(buffer);
            Tokens.Add(cancellationToken);
            Started.TrySetResult(true);
            if (WaitForCancellation) await Task.Delay(Timeout.Infinite, cancellationToken);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return Next(buffer.Span);
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The memory-based asynchronous overload is required.");
        private void Observe(Memory<byte> buffer)
        {
            Assert.Equal(65536, buffer.Length);
            Assert.True(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment));
            if (Buffer == null) Buffer = segment.Array;
            else Assert.Same(Buffer, segment.Array);
        }
        private int Next(Span<byte> buffer)
        {
            ReadCalls++;
            if (ReadCalls == FailOnRead) throw Failure;
            int size = Math.Min(_input.Length - BytesRead, Math.Min(buffer.Length, _sizes[(ReadCalls - 1) % _sizes.Length]));
            _input.AsSpan(BytesRead, size).CopyTo(buffer);
            BytesRead += size;
            if (size == 0) EndReads++;
            AfterRead?.Invoke(size);
            return size;
        }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
