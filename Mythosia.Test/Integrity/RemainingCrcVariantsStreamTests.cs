using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class RemainingCrcVariantsStreamTests
{
    private const int BufferSize = 64 * 1024;

    public static IEnumerable<object[]> Models() => RemainingCrcVariantsTests.Models();

    [Theory]
    [MemberData(nameof(Models))]
    public async Task ShortNonseekableReadsCross64KiBBoundariesWithOneReusableBuffer(int width, int variant, uint _)
    {
        // Shared CRC stream tests already cover lifecycle and validation of the
        // common reader. Here each added model must survive short reads and a
        // register carried across both sides of the reader's buffer boundary.
        foreach (int length in new[] { BufferSize - 1, BufferSize, BufferSize + 1 })
        {
            byte[] input = Enumerable.Range(0, length).Select(index => (byte)(index * 73 + (index >> 3) + 19)).ToArray();
            uint expected = RemainingCrcVariantsTests.Reference(input, width, variant);
            using var sync = new ShortReadStream(input, asynchronous: false);
            using var asyncSource = new ShortReadStream(input, asynchronous: true);
            Assert.Equal(expected, RemainingCrcVariantsTests.ComputeStream(sync, width, variant));
            Assert.Equal(expected, await RemainingCrcVariantsTests.ComputeStreamAsync(asyncSource, width, variant));
            foreach (ShortReadStream source in new[] { sync, asyncSource })
            {
                Assert.Equal(length, source.BytesRead);
                Assert.True(source.ReadCalls > 1);
                Assert.NotNull(source.Buffer);
                Assert.True(source.CanRead);
                Assert.False(source.Disposed);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Models))]
    public async Task CurrentPositionIsRespectedAndTheSourceStaysOpen(int width, int variant, uint _)
    {
        byte[] input = Enumerable.Range(0, 257).Select(index => (byte)index).ToArray();
        foreach (bool asynchronous in new[] { false, true })
        {
            using var source = new MemoryStream(input, writable: false);
            source.Position = 17;
            uint expected = RemainingCrcVariantsTests.Reference(input.Skip(17), width, variant);
            uint actual = asynchronous
                ? await RemainingCrcVariantsTests.ComputeStreamAsync(source, width, variant)
                : RemainingCrcVariantsTests.ComputeStream(source, width, variant);
            Assert.Equal(expected, actual);
            Assert.Equal(input.Length, source.Position);
            Assert.True(source.CanRead);
        }
    }

    [Theory]
    [MemberData(nameof(Models))]
    public async Task CancellationBeforeOrDuringAReadStopsEveryAddedModelAndLeavesTheSourceOpen(int width, int variant, uint _)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var untouched = new ShortReadStream(new byte[17], asynchronous: true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RemainingCrcVariantsTests.ComputeStreamAsync(untouched, width, variant, cancellation.Token));
        Assert.Equal(0, untouched.ReadCalls);
        Assert.False(untouched.Disposed);

        using var duringRead = new CancellationTokenSource();
        using var source = new ShortReadStream(new byte[17], asynchronous: true, cancellation: duringRead);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RemainingCrcVariantsTests.ComputeStreamAsync(source, width, variant, duringRead.Token));
        Assert.Equal(1, source.ReadCalls);
        Assert.False(source.Disposed);
    }

    private sealed class ShortReadStream(byte[] input, bool asynchronous, CancellationTokenSource? cancellation = null) : Stream
    {
        private readonly int[] _readSizes = { 1, 3, 17, 4096, BufferSize };
        public int BytesRead { get; private set; }
        public int ReadCalls { get; private set; }
        public byte[]? Buffer { get; private set; }
        public bool Disposed { get; private set; }
        public override bool CanRead => !Disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Assert.False(asynchronous, "Async API used synchronous Read.");
            ObserveBuffer(buffer, offset, count);
            return CopyTo(buffer.AsSpan(offset, count));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Assert.True(asynchronous);
            ObserveBuffer(buffer, offset, count);
            return ReadAfterYield(buffer.AsMemory(offset, count), cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Assert.True(asynchronous);
            Assert.True(System.Runtime.InteropServices.MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment));
            ObserveBuffer(segment.Array!, segment.Offset, segment.Count);
            return new ValueTask<int>(ReadAfterYield(buffer, cancellationToken));
        }

        private async Task<int> ReadAfterYield(Memory<byte> buffer, CancellationToken token)
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            int count = CopyTo(buffer.Span);
            cancellation?.Cancel();
            return count;
        }

        private void ObserveBuffer(byte[] buffer, int offset, int count)
        {
            Assert.Equal(0, offset);
            Assert.Equal(BufferSize, count);
            if (Buffer == null) Buffer = buffer;
            else Assert.Same(Buffer, buffer);
        }

        private int CopyTo(Span<byte> destination)
        {
            int count = Math.Min(Math.Min(destination.Length, _readSizes[ReadCalls % _readSizes.Length]), input.Length - BytesRead);
            ReadCalls++;
            input.AsSpan(BytesRead, count).CopyTo(destination);
            BytesRead += count;
            return count;
        }

        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
