using System.Collections;
using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class XxHashTests
{
    public static IEnumerable<object[]> PublishedVectors() => XxHashPublishedVectors.All();

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public void PublishedAnswersMatchNumericNativeDigestFramesAndIndependentOracle(
        string description, int width, string inputHex, ulong seed, ulong expected)
    {
        _ = description;
        byte[] input = Convert.FromHexString(inputHex);
        Assert.Equal(expected, XxHashReference.Compute(input, width, seed));
        Assert.Equal(expected, Numeric(input, width, seed));
        Assert.Equal(expected, Numeric(new OnceOnly(input), width, seed));
        ErrorDetection detector = Detector(width, seed);
        byte[] digest = NativeBytes(expected, width);
        Assert.Equal(digest, detector.Compute(new OnceOnly(input)));
        byte[] frame = input.Concat(digest).ToArray();
        Assert.Equal(frame, detector.Encode(new OnceOnly(input)));
        Assert.Equal(frame, width == 32 ? input.WithXxHash32((uint)seed) : input.WithXxHash64(seed));
        Assert.Equal(input.Length == 0, detector.IsError(new OnceOnly(frame)));
        Assert.Equal(input, detector.Decode(new OnceOnly(frame)));
        Assert.Equal(width == 32 ? "XxHash32" : "XxHash64", detector.GetDetectionType());
    }

    [Fact]
    public void EachWidthRetainsAtLeastThirtyDistinctPublishedNonemptySeedZeroAnswers()
    {
        foreach (int width in new[] { 32, 64 })
        {
            object[][] rows = PublishedVectors().Where(row => (int)row[1] == width && (ulong)row[3] == 0 && ((string)row[2]).Length != 0).ToArray();
            Assert.True(rows.Length >= 30);
            Assert.Equal(rows.Length, rows.Select(row => (string)row[2]).Distinct().Count());
        }
    }

    public static IEnumerable<object[]> BoundaryLengths()
    {
        foreach (int width in new[] { 32, 64 })
        foreach (int length in new[] { 0, 1, 3, 4, 7, 8, 15, 16, 17, 31, 32, 33, 63, 64, 65, 255, 256, 257 })
            yield return new object[] { width, length };
    }

    [Theory]
    [MemberData(nameof(BoundaryLengths))]
    public void EverySplitAndChunkSizePreservesSnapshotsAndSeedAcrossReset(int width, int length)
    {
        byte[] input = BinaryInput(length);
        ulong seed = width == 32 ? uint.MaxValue : ulong.MaxValue;
        ulong expected = XxHashReference.Compute(input, width, seed);
        Assert.Equal(expected, Numeric(input, width, seed));
        for (int split = 0; split <= length; split++)
        {
            var accumulator = new Accumulator(width, seed);
            accumulator.Append(input.AsSpan(0, split));
            ulong prefix = XxHashReference.Compute(input.AsSpan(0, split), width, seed);
            Assert.Equal(prefix, accumulator.Value);
            Assert.Equal(prefix, accumulator.Value);
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            Assert.Equal(prefix, accumulator.Value);
            accumulator.Append(input.AsSpan(split));
            Assert.Equal(expected, accumulator.Value);
            accumulator.Reset();
            Assert.Equal(seed, accumulator.Seed);
            Assert.Equal(XxHashReference.Compute(ReadOnlySpan<byte>.Empty, width, seed), accumulator.Value);
            accumulator.Append(input);
            Assert.Equal(expected, accumulator.Value);
        }
        foreach (int chunkSize in new[] { 1, 3, 7, 16, 31, 32, 33, 64 })
        {
            var accumulator = new Accumulator(width, seed);
            for (int offset = 0; offset < length; offset += chunkSize)
                accumulator.Append(input.AsSpan(offset, Math.Min(chunkSize, length - offset)));
            Assert.Equal(expected, accumulator.Value);
        }
    }

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public void IndependentAccumulatorsDoNotRetainMutableSourceOrAffectEachOther(int width)
    {
        byte[] input = BinaryInput(97);
        ulong expected = XxHashReference.Compute(input, width);
        var first = new Accumulator(width, 0);
        var other = new Accumulator(width, 42);
        first.Append(input);
        other.Append(input.AsSpan(0, 17));
        Array.Fill(input, (byte)0);
        Assert.Equal(expected, first.Value);
        other.Reset();
        Assert.Equal(expected, first.Value);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public void FramesRejectEverySingleBitMutationMissingPayloadAndWrongSeed(int width)
    {
        byte[] input = BinaryInput(33);
        ErrorDetection detector = Detector(width, 42);
        byte[] frame = detector.Encode(input).ToArray();
        Assert.False(detector.IsError(frame));
        Assert.True(Detector(width, 0).IsError(frame));
        for (int bit = 0; bit < frame.Length * 8; bit++)
        {
            byte[] changed = frame.ToArray();
            changed[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.True(detector.IsError(changed));
            Assert.Empty(detector.Decode(changed));
        }
        for (int length = 0; length <= width / 8; length++)
        {
            Assert.True(detector.IsError(new byte[length]));
            Assert.Empty(detector.Decode(new byte[length]));
        }
        byte[] emptyFrame = detector.Encode(Array.Empty<byte>()).ToArray();
        Assert.Equal(width / 8, emptyFrame.Length);
        Assert.True(detector.IsError(emptyFrame));
        Assert.Empty(detector.Decode(emptyFrame));
    }

    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    public void NullInputsFailImmediatelyAndEnumerationFailuresDisposeTheEnumerator(int width)
    {
        ErrorDetection detector = Detector(width, 0);
        Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => detector.Compute(null!)).ParamName);
        Assert.Equal("source", Assert.Throws<ArgumentNullException>(() => detector.Encode(null!)).ParamName);
        Assert.Equal("sourceWithCRC", Assert.Throws<ArgumentNullException>(() => detector.Decode(null!)).ParamName);
        Assert.Equal("sourceWithCRC", Assert.Throws<ArgumentNullException>(() => detector.IsError(null!)).ParamName);
        Assert.Equal("data", Assert.Throws<ArgumentNullException>(() => Numeric(null!, width, 0)).ParamName);
        Assert.Equal("data", Assert.Throws<ArgumentNullException>(() =>
            width == 32 ? ((IEnumerable<byte>)null!).WithXxHash32() : ((IEnumerable<byte>)null!).WithXxHash64()).ParamName);
        foreach (Action<IEnumerable<byte>> operation in new Action<IEnumerable<byte>>[]
        {
            source => detector.Compute(source), source => detector.Encode(source),
            source => detector.Decode(source), source => detector.IsError(source), source => Numeric(source, width, 0)
        })
        {
            var source = new OnceOnly(BinaryInput(7), failAfter: 3);
            Assert.Same(source.Failure, Assert.Throws<IOException>(() => operation(source)));
            Assert.True(source.Disposed);
        }
    }

    internal static byte[] BinaryInput(int length) => Enumerable.Range(0, length).Select(i => (byte)((i * 73 + (i >> 8) * 29) & 255)).ToArray();
    internal static ulong Numeric(IEnumerable<byte> source, int width, ulong seed = 0) =>
        width == 32 ? source.XxHash32((uint)seed) : source.XxHash64(seed);
    private static ErrorDetection Detector(int width, ulong seed) => width == 32 ? new XxHash32((uint)seed) : new XxHash64(seed);
    private static byte[] NativeBytes(ulong value, int width) => width == 32 ? BitConverter.GetBytes((uint)value) : BitConverter.GetBytes(value);

    internal sealed class Accumulator
    {
        private readonly XxHash32Accumulator? _narrow;
        private readonly XxHash64Accumulator? _wide;
        internal Accumulator(int width, ulong seed)
        {
            if (width == 32) _narrow = new XxHash32Accumulator((uint)seed);
            else _wide = new XxHash64Accumulator(seed);
        }
        internal void Append(ReadOnlySpan<byte> source)
        {
            if (_narrow != null) _narrow.Append(source);
            else _wide!.Append(source);
        }
        internal ulong Value => _narrow != null ? _narrow.GetCurrentValue() : _wide!.GetCurrentValue();
        internal ulong Seed => _narrow != null ? _narrow.Seed : _wide!.Seed;
        internal void Reset()
        {
            if (_narrow != null) _narrow.Reset();
            else _wide!.Reset();
        }
    }

    private sealed class OnceOnly : IEnumerable<byte>
    {
        private readonly byte[] _input;
        private readonly int _failAfter;
        private bool _enumerated;
        internal bool Disposed { get; private set; }
        internal IOException Failure { get; } = new("Published input enumeration failed.");
        internal OnceOnly(byte[] input, int failAfter = -1) { _input = input; _failAfter = failAfter; }
        public IEnumerator<byte> GetEnumerator()
        {
            Assert.False(_enumerated);
            _enumerated = true;
            return Enumerate().GetEnumerator();
        }
        private IEnumerable<byte> Enumerate()
        {
            try
            {
                for (int i = 0; i < _input.Length; i++)
                {
                    if (i == _failAfter) throw Failure;
                    yield return _input[i];
                }
            }
            finally { Disposed = true; }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
