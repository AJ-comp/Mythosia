using System.Runtime.InteropServices;
using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Fletcher4StreamTests
{
    public static IEnumerable<object[]> BoundaryCases()
    {
        foreach (var order in new[] { Fletcher4ByteOrder.LittleEndian, Fletcher4ByteOrder.BigEndian })
        foreach (bool asynchronous in new[] { false, true })
        foreach (int length in new[] { 0, 4, 12, 65532, 65536, 65540, 131076 })
            yield return new object[] { order, asynchronous, length };
    }

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public async Task PartialNonseekableReadsMatchIndependentOracleAndReuseOneBoundedBuffer(
        Fletcher4ByteOrder order, bool asynchronous, int length)
    {
        byte[] data = Fletcher4Tests.BinaryInput(length);
        Fletcher4Value expected = Fletcher4Reference.Compute(data, order);
        using var source = new ObservedStream(data, asynchronous);
        Assert.Equal(expected, await Compute(source, order, asynchronous));
        Assert.Equal(length, source.PositionRead);
        Assert.Equal(1, source.EndReads);
        Assert.NotNull(source.Buffer);
        Assert.False(source.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadsStartAtCurrentPositionAndDoNotCloseStream(bool asynchronous)
    {
        byte[] data = Fletcher4Tests.BinaryInput(100);
        using var source = new MemoryStream(data, writable: false);
        source.Position = 12;
        Assert.Equal(Fletcher4Reference.Compute(data.AsSpan(12), Fletcher4ByteOrder.BigEndian),
            await Compute(source, Fletcher4ByteOrder.BigEndian, asynchronous));
        Assert.True(source.CanRead);
        Assert.Equal(100, source.Position);
        Assert.Equal(default, await Compute(source, Fletcher4ByteOrder.LittleEndian, asynchronous));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 65537)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 65537)]
    public async Task IncompleteFinalWordIsRejectedAndStreamRemainsOpen(bool asynchronous, int length)
    {
        using var source = new ObservedStream(new byte[length], asynchronous);
        await Assert.ThrowsAsync<InvalidDataException>(() => Compute(source, Fletcher4ByteOrder.LittleEndian, asynchronous));
        Assert.False(source.Disposed);
        Assert.Equal(length, source.PositionRead);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidSourcesAndByteOrdersFailBeforeReads(bool asynchronous)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Compute(null!, Fletcher4ByteOrder.LittleEndian, asynchronous));
        using var source = new ObservedStream(new byte[4], asynchronous) { Readable = false };
        await Assert.ThrowsAsync<ArgumentException>(() => Compute(source, Fletcher4ByteOrder.LittleEndian, asynchronous));
        source.Readable = true;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Compute(source, (Fletcher4ByteOrder)2, asynchronous));
        Assert.Equal(0, source.ReadCalls);
        Assert.False(source.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadFailurePropagatesOriginalExceptionWithoutDisposingSource(bool asynchronous)
    {
        using var source = new ObservedStream(new byte[100], asynchronous) { FailOnRead = 3 };
        Assert.Same(source.Failure, await Assert.ThrowsAsync<IOException>(() => Compute(source, Fletcher4ByteOrder.LittleEndian, asynchronous)));
        Assert.Equal(3, source.ReadCalls);
        Assert.False(source.Disposed);
    }

    [Fact]
    public async Task AlreadyCancelledTokenPreventsReads()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var source = new ObservedStream(new byte[100], true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.ComputeFletcher4Async(cancellationToken: cancellation.Token));
        Assert.Equal(0, source.ReadCalls);
        Assert.False(source.Disposed);
    }

    [Fact]
    public async Task CancellationAfterAReadIsObservedBeforeAnotherRead()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(new byte[100], true) { AfterRead = cancellation.Cancel };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.ComputeFletcher4Async(cancellationToken: cancellation.Token));
        Assert.Equal(1, source.ReadCalls);
        Assert.Equal(cancellation.Token, source.LastToken);
        Assert.False(source.Disposed);
    }

    [Fact]
    public async Task AsyncReadReceivesCancellationTokenIncludingTheEofRead()
    {
        using var cancellation = new CancellationTokenSource();
        using var source = new ObservedStream(new byte[4], true) { ExpectedToken = cancellation.Token };
        Assert.Equal(default, await source.ComputeFletcher4Async(cancellationToken: cancellation.Token));
        Assert.Equal(1, source.EndReads);
        Assert.Equal(cancellation.Token, source.LastToken);
    }

    private static Task<Fletcher4Value> Compute(Stream source, Fletcher4ByteOrder order, bool asynchronous) => asynchronous
        ? source.ComputeFletcher4Async(order)
        : Task.FromResult(source.ComputeFletcher4(order));

    private sealed class ObservedStream(byte[] data, bool asynchronous) : Stream
    {
        private static readonly int[] ChunkSizes = { 1, 3, 5, 257, 65536 };
        public bool Readable { get; set; } = true;
        public int ReadCalls { get; private set; }
        public int PositionRead { get; private set; }
        public int EndReads { get; private set; }
        public bool Disposed { get; private set; }
        public byte[]? Buffer { get; private set; }
        public int FailOnRead { get; init; }
        public Action? AfterRead { get; init; }
        public CancellationToken? ExpectedToken { get; init; }
        public CancellationToken LastToken { get; private set; }
        public IOException Failure { get; } = new("Injected Fletcher-4 read failure.");
        public override bool CanRead => Readable && !Disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Assert.False(asynchronous, "Async computation must use ReadAsync(Memory<byte>).");
            ObserveBuffer(buffer, offset, count);
            return Consume(buffer.AsSpan(offset, count));
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Assert.True(asynchronous);
            Assert.True(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment));
            ObserveBuffer(segment.Array!, segment.Offset, segment.Count);
            LastToken = cancellationToken;
            if (ExpectedToken.HasValue) Assert.Equal(ExpectedToken.Value, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            return Consume(buffer.Span);
        }

        private void ObserveBuffer(byte[] buffer, int offset, int count)
        {
            Assert.Equal(0, offset);
            Assert.Equal(64 * 1024, count);
            Buffer ??= buffer;
            Assert.Same(Buffer, buffer);
        }

        private int Consume(Span<byte> buffer)
        {
            ReadCalls++;
            if (ReadCalls == FailOnRead) throw Failure;
            int count = Math.Min(Math.Min(buffer.Length, ChunkSizes[(ReadCalls - 1) % ChunkSizes.Length]), data.Length - PositionRead);
            data.AsSpan(PositionRead, count).CopyTo(buffer);
            PositionRead += count;
            if (count == 0) EndReads++;
            AfterRead?.Invoke();
            return count;
        }

        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
