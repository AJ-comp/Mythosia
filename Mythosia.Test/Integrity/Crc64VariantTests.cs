using System.Collections;
using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Crc64VariantTests
{
    private const int BufferSize = 64 * 1024;
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    public static IEnumerable<object[]> PublishedVectors() =>
        Crc64VariantPublishedVectors.Ecma182().Concat(Crc64VariantPublishedVectors.Xz());

    public static IEnumerable<object[]> Variants()
    {
        yield return new object[] { CRC64Type.ECMA182 };
        yield return new object[] { CRC64Type.XZ };
    }

    public static IEnumerable<object[]> BoundaryLengths()
    {
        foreach (object[] variant in Variants())
        foreach (int length in new[] { 0, 1, 2, 7, 8, 9, 31, 32, 33, 255, 256, 257, 65535, 65536, 65537, 131089 })
            yield return new object[] { variant[0], length };
    }

    public static IEnumerable<object[]> SequenceOperations()
    {
        foreach (object[] variant in Variants())
        foreach (string operation in new[] { "Compute", "Encode", "Decode", "IsError", "CRC64", "WithCRC64" })
            yield return new object[] { variant[0], operation };
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void PublishedCorpusContainsAtLeastThirtyDistinctNonemptyInputs(CRC64Type type)
    {
        object[][] vectors = PublishedVectors().Where(vector => (CRC64Type)vector[0] == type).ToArray();
        Assert.True(vectors.Length >= 30, $"{type} needs at least thirty frozen public answers.");
        Assert.All(vectors, vector => Assert.NotEmpty((byte[])vector[2]));
        Assert.Equal(vectors.Length, vectors.Select(vector => Convert.ToHexString((byte[])vector[2])).Distinct().Count());
    }

    [Theory]
    [InlineData(CRC64Type.ECMA182, 0x6C40DF5F0B497347UL)]
    [InlineData(CRC64Type.XZ, 0x995DC9BBDF1939FAUL)]
    public void StandardChecksDistinguishTheSharedPolynomialVariants(CRC64Type type, ulong expected)
    {
        Assert.Equal(expected, Crc64VariantReference.Compute(CheckInput, type));
        Assert.Equal(expected, CheckInput.CRC64(type));
        Assert.Equal(NativeBytes(expected), new CRC64(type).Compute(CheckInput));
        Assert.NotEqual(CheckInput.CRC64(), CheckInput.CRC64(type));
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public async Task PublishedAnswersMatchOracleCalculationFramesSplitsAndStreams(
        CRC64Type type, string name, byte[] input, ulong expected)
    {
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.Equal(expected, Crc64VariantReference.Compute(input, type));
        Assert.Equal(expected, input.CRC64(type));
        var detector = new CRC64(type);
        Assert.Equal(NativeBytes(expected), detector.Compute(input));
        byte[] frame = input.Concat(NativeBytes(expected)).ToArray();
        Assert.Equal(frame, detector.Encode(input));
        Assert.Equal(frame, input.WithCRC64(type));
        Assert.False(detector.IsError(frame));
        Assert.Equal(input, detector.Decode(frame));

        var accumulator = new CRC64Accumulator(type);
        foreach (int split in Splits(input.Length))
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(input.AsSpan(split));
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }

        int chunkSize = input.Length <= 256 ? 3 : 257;
        using var sync = new ShortReadStream(input, asynchronous: false, chunkSize: chunkSize);
        using var asyncSource = new ShortReadStream(input, asynchronous: true, chunkSize: chunkSize);
        Assert.Equal(expected, sync.ComputeCRC64(type));
        Assert.Equal(expected, await asyncSource.ComputeCRC64Async(type));
        Assert.True(sync.CanRead);
        Assert.True(asyncSource.CanRead);
    }

    [Theory]
    [MemberData(nameof(BoundaryLengths))]
    public async Task BinaryInputsAndBufferBoundariesMatchIndependentCalculation(CRC64Type type, int length)
    {
        byte[] input = BinaryInput(length);
        ulong expected = Crc64VariantReference.Compute(input, type);
        Assert.Equal(expected, input.CRC64(type));
        Assert.Equal(NativeBytes(expected), new CRC64(type).Compute(input));
        var accumulator = new CRC64Accumulator(type);
        var random = new Random(0x64EC + length);
        for (int offset = 0; offset < input.Length;)
        {
            int count = Math.Min(random.Next(1, 4097), input.Length - offset);
            accumulator.Append(input.AsSpan(offset, count));
            offset += count;
        }
        Assert.Equal(expected, accumulator.GetCurrentValue());

        using var sync = new ShortReadStream(input, asynchronous: false, chunkSize: BufferSize);
        using var asyncSource = new ShortReadStream(input, asynchronous: true, chunkSize: BufferSize);
        Assert.Equal(expected, sync.ComputeCRC64(type));
        Assert.Equal(expected, await asyncSource.ComputeCRC64Async(type));
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void EveryByteValueAndRepresentativeBinaryPatternsMatchOracle(CRC64Type type)
    {
        for (int value = 0; value <= byte.MaxValue; value++)
        {
            byte[] input = { (byte)value };
            Assert.Equal(Crc64VariantReference.Compute(input, type), input.CRC64(type));
        }

        foreach (byte[] input in new[] { new byte[257], Enumerable.Repeat((byte)0xFF, 257).ToArray(), BinaryInput(257) })
        {
            ulong expected = Crc64VariantReference.Compute(input, type);
            var accumulator = new CRC64Accumulator(type);
            for (int split = 0; split <= input.Length; split++)
            {
                accumulator.Reset();
                accumulator.Append(input.AsSpan(0, split));
                accumulator.Append(input.AsSpan(split));
                Assert.Equal(expected, accumulator.GetCurrentValue());
            }
        }
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void SnapshotsSlicesEmptyChunksAndResetPreserveTheSelectedAlgorithm(CRC64Type type)
    {
        byte[] input = BinaryInput(257);
        var accumulator = new CRC64Accumulator(type);
        Assert.Equal(type, accumulator.Type);
        Assert.Equal(0UL, accumulator.GetCurrentValue());
        int offset = 0;
        foreach (int count in new[] { 1, 63, 64, 65, 64 })
        {
            byte[] guarded = Enumerable.Repeat((byte)0xEE, count + 11).ToArray();
            input.AsSpan(offset, count).CopyTo(guarded.AsSpan(5));
            byte[] unchanged = guarded.ToArray();
            accumulator.Append(guarded.AsSpan(5, count));
            Assert.Equal(unchanged, guarded);
            Array.Fill(guarded, (byte)0);
            offset += count;
            ulong expected = Crc64VariantReference.Compute(input.AsSpan(0, offset), type);
            Assert.Equal(expected, accumulator.GetCurrentValue());
            Assert.Equal(expected, accumulator.GetCurrentValue());
            accumulator.Append(default);
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }
        accumulator.Reset();
        accumulator.Reset();
        Assert.Equal(type, accumulator.Type);
        Assert.Equal(0UL, accumulator.GetCurrentValue());
        accumulator.Append(CheckInput);
        Assert.Equal(Crc64VariantReference.Compute(CheckInput, type), accumulator.GetCurrentValue());
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void IndependentFramesRejectEverySingleBitMutationAndDigestOnlyInputs(CRC64Type type)
    {
        var detector = new CRC64(type);
        byte[] frame = CheckInput.Concat(NativeBytes(Crc64VariantReference.Compute(CheckInput, type))).ToArray();
        Assert.False(detector.IsError(frame));
        Assert.Equal(CheckInput, detector.Decode(frame));
        for (int bit = 0; bit < frame.Length * 8; bit++)
        {
            byte[] damaged = frame.ToArray();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.True(detector.IsError(damaged));
            Assert.Empty(detector.Decode(damaged));
        }
        for (int length = 0; length <= sizeof(ulong); length++)
        {
            Assert.True(detector.IsError(new byte[length]));
            Assert.Empty(detector.Decode(new byte[length]));
        }
        byte[] wrongByteOrder = CheckInput.Concat(NativeBytes(Crc64VariantReference.Compute(CheckInput, type)).Reverse()).ToArray();
        Assert.True(detector.IsError(wrongByteOrder));
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public async Task TypeAndEmptyInputContractsAreExplicit(CRC64Type type)
    {
        var detector = new CRC64(type);
        Assert.IsAssignableFrom<ErrorDetection>(detector);
        Assert.Equal(type, detector.Type);
        Assert.Equal(type.ToString(), detector.GetDetectionType());
        Assert.Equal(0UL, Array.Empty<byte>().CRC64(type));
        Assert.Equal(new byte[8], detector.Compute(Array.Empty<byte>()));
        Assert.Equal(new byte[8], detector.Encode(Array.Empty<byte>()));
        Assert.Equal(new byte[8], Array.Empty<byte>().WithCRC64(type));
        using var sync = new MemoryStream();
        using var asyncSource = new MemoryStream();
        Assert.Equal(0UL, sync.ComputeCRC64(type));
        Assert.Equal(0UL, await asyncSource.ComputeCRC64Async(type));
        Assert.True(sync.CanRead);
        Assert.True(asyncSource.CanRead);
        Assert.Throws<ArgumentNullException>(() => StreamCRCExtensions.ComputeCRC64(null!, type));
        await Assert.ThrowsAsync<ArgumentNullException>(() => StreamCRCExtensions.ComputeCRC64Async(null!, type));
    }

    [Theory]
    [MemberData(nameof(SequenceOperations))]
    public void SequencePathsRejectNullAndConsumeExactlyOnce(CRC64Type type, string operation)
    {
        Assert.Throws<ArgumentNullException>(() => Perform(operation, null!, type));
        byte[] input = operation is "Decode" or "IsError"
            ? CheckInput.Concat(NativeBytes(Crc64VariantReference.Compute(CheckInput, type))).ToArray()
            : CheckInput;
        var source = new SinglePassSource(input);
        Perform(operation, source, type);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData(CRC64Type.ECMA182, false)]
    [InlineData(CRC64Type.ECMA182, true)]
    [InlineData(CRC64Type.XZ, false)]
    [InlineData(CRC64Type.XZ, true)]
    public async Task NonseekableShortReadsStartAtCurrentPositionReuseOneBufferAndRemainOpen(CRC64Type type, bool asynchronous)
    {
        byte[] input = BinaryInput(2 * BufferSize + 17);
        using var cancellation = new CancellationTokenSource();
        using var source = new ShortReadStream(input, asynchronous, chunkSize: 4093, start: 17);
        ulong expected = Crc64VariantReference.Compute(input.AsSpan(17), type);
        ulong actual = asynchronous
            ? await source.ComputeCRC64Async(type, cancellation.Token)
            : source.ComputeCRC64(type);
        Assert.Equal(expected, actual);
        Assert.Equal(input.Length, source.BytesConsumed);
        Assert.True(source.ReadCalls > 2);
        Assert.Equal(BufferSize, source.Buffer!.Length);
        Assert.True(source.CanRead);
        if (asynchronous) Assert.Equal(cancellation.Token, source.LastToken);
    }

    private static IEnumerable<int> Splits(int length) => length <= 64
        ? Enumerable.Range(0, length + 1)
        : new[] { 0, 1, 7, 8, 9, 31, 32, 33, 63, 64, 65, 255, 256, 257, 65535, 65536, 65537, length / 2, length - 1, length }
            .Where(split => split <= length).Distinct();

    private static void Perform(string operation, IEnumerable<byte> source, CRC64Type type)
    {
        var detector = new CRC64(type);
        ulong expected = Crc64VariantReference.Compute(CheckInput, type);
        switch (operation)
        {
            case "Compute": Assert.Equal(NativeBytes(expected), detector.Compute(source)); break;
            case "Encode": Assert.Equal(CheckInput.Concat(NativeBytes(expected)), detector.Encode(source)); break;
            case "Decode": Assert.Equal(CheckInput, detector.Decode(source)); break;
            case "IsError": Assert.False(detector.IsError(source)); break;
            case "CRC64": Assert.Equal(expected, source.CRC64(type)); break;
            case "WithCRC64": Assert.Equal(CheckInput.Concat(NativeBytes(expected)), source.WithCRC64(type)); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static byte[] BinaryInput(int length) => Enumerable.Range(0, length).Select(i => (byte)(i * 73 + i / 7)).ToArray();

    private static byte[] NativeBytes(ulong value)
    {
        var result = new byte[sizeof(ulong)];
        for (int index = 0; index < result.Length; index++)
        {
            int shift = (BitConverter.IsLittleEndian ? index : result.Length - 1 - index) * 8;
            result[index] = (byte)(value >> shift);
        }
        return result;
    }

    private sealed class SinglePassSource(byte[] bytes) : IEnumerable<byte>
    {
        public int EnumeratorCount { get; private set; }
        public int DisposeCount { get; private set; }
        public IEnumerator<byte> GetEnumerator()
        {
            if (++EnumeratorCount != 1) throw new InvalidOperationException("Source was enumerated twice.");
            return Enumerate().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private IEnumerable<byte> Enumerate()
        {
            try { foreach (byte value in bytes) yield return value; }
            finally { DisposeCount++; }
        }
    }

    private sealed class ShortReadStream(byte[] bytes, bool asynchronous, int chunkSize, int start = 0) : Stream
    {
        private bool _disposed;
        public int BytesConsumed { get; private set; } = start;
        public int ReadCalls { get; private set; }
        public byte[]? Buffer { get; private set; }
        public CancellationToken LastToken { get; private set; }
        public override bool CanRead => !_disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            Assert.False(asynchronous, "An asynchronous calculation used synchronous Read.");
            return ReadCore(buffer, offset, count);
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Assert.True(asynchronous, "A synchronous calculation used ReadAsync.");
            LastToken = cancellationToken;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return ReadCore(buffer, offset, count);
        }
        private int ReadCore(byte[] buffer, int offset, int count)
        {
            Assert.True(CanRead);
            Assert.InRange(count, 1, BufferSize);
            Buffer ??= buffer;
            Assert.Same(Buffer, buffer);
            ReadCalls++;
            Array.Fill(buffer, (byte)0xE7, offset, count);
            int read = Math.Min(Math.Min(chunkSize, count), bytes.Length - BytesConsumed);
            Array.Copy(bytes, BytesConsumed, buffer, offset, read);
            BytesConsumed += read;
            return read;
        }
        protected override void Dispose(bool disposing) { _disposed = true; base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
