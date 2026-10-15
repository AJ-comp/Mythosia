using System.Collections;
using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Fletcher4Tests
{
    public static TheoryData<string, byte[], ulong, ulong, ulong, ulong> PublishedCases => Fletcher4PublishedVectors.Cases;

    [Fact]
    public void PublishedCoverageContainsAtLeastThirtyDistinctNonemptyAlignedInputs()
    {
        var cases = PublishedCases.ToArray();
        Assert.True(cases.Length >= 30);
        Assert.All(cases, row =>
        {
            byte[] data = (byte[])row[1];
            Assert.NotEmpty(data);
            Assert.Equal(0, data.Length % 4);
        });
        Assert.Equal(cases.Length, cases.Select(row => Convert.ToHexString((byte[])row[1])).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(PublishedCases))]
    public void PublishedOpenZfsRecordChecksumsMatchNumericDigestFrameAccumulatorAndIndependentReference(
        string name, byte[] data, ulong a, ulong b, ulong c, ulong d)
    {
        var expected = new Fletcher4Value(a, b, c, d);
        byte[] original = (byte[])data.Clone();
        var detector = new Fletcher4();
        Assert.Equal(expected, data.Fletcher4());
        Assert.Equal(expected, Fletcher4Reference.Compute(data, Fletcher4ByteOrder.LittleEndian));
        Assert.Equal(NativeDigest(expected), detector.Compute(data));
        byte[] frame = data.WithFletcher4().ToArray();
        Assert.False(detector.IsError(frame), name);
        Assert.Equal(data, detector.Decode(frame));
        Assert.Equal(data.Concat(NativeDigest(expected)), frame);
        var accumulator = new Fletcher4Accumulator();
        for (int at = 0; at < data.Length; at += 997)
            accumulator.Append(data.AsSpan(at, Math.Min(997, data.Length - at)));
        Assert.Equal(expected, accumulator.GetCurrentValue());
        Assert.Equal(original, data);
    }

    [Theory]
    [InlineData(Fletcher4ByteOrder.LittleEndian)]
    [InlineData(Fletcher4ByteOrder.BigEndian)]
    public void FourKnownWordsAndEmptyInputDefineWordOrderAndSums(Fletcher4ByteOrder order)
    {
        byte[] data = Enumerable.Range(1, 4).SelectMany(value => order == Fletcher4ByteOrder.LittleEndian
            ? new byte[] { (byte)value, 0, 0, 0 } : new byte[] { 0, 0, 0, (byte)value }).ToArray();
        Assert.Equal(new Fletcher4Value(10, 20, 35, 56), data.Fletcher4(order));
        Assert.Equal(default, Array.Empty<byte>().Fletcher4(order));
        Assert.Equal(new byte[32], new Fletcher4(order).Compute(Array.Empty<byte>()));
        Assert.Equal(order, new Fletcher4(order).ByteOrder);
        Assert.Equal(order, new Fletcher4Accumulator(order).ByteOrder);
        Assert.Equal("Fletcher4", new Fletcher4(order).GetDetectionType());
    }

    [Theory]
    [InlineData(Fletcher4ByteOrder.LittleEndian)]
    [InlineData(Fletcher4ByteOrder.BigEndian)]
    public void EveryShortSplitAndNonmutatingSnapshotsPreservePartialWords(Fletcher4ByteOrder order)
    {
        byte[] data = BinaryInput(64);
        Fletcher4Value expected = Fletcher4Reference.Compute(data, order);
        var accumulator = new Fletcher4Accumulator(order);
        for (int split = 0; split <= data.Length; split++)
        {
            accumulator.Append(data.AsSpan(0, split));
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            if (split % 4 != 0)
                Assert.Throws<InvalidOperationException>(() => accumulator.GetCurrentValue());
            else
            {
                var prefix = Fletcher4Reference.Compute(data.AsSpan(0, split), order);
                Assert.Equal(prefix, accumulator.GetCurrentValue());
                Assert.Equal(prefix, accumulator.GetCurrentValue());
            }
            accumulator.Append(data.AsSpan(split));
            Assert.Equal(expected, accumulator.GetCurrentValue());
            accumulator.Reset();
            Assert.Equal(default, accumulator.GetCurrentValue());
        }
        accumulator.Append(new byte[] { 0xFF });
        accumulator.Reset();
        accumulator.Append(data);
        Assert.Equal(expected, accumulator.GetCurrentValue());
    }

    [Theory]
    [InlineData(Fletcher4ByteOrder.LittleEndian, 8192)]
    [InlineData(Fletcher4ByteOrder.BigEndian, 8192)]
    [InlineData(Fletcher4ByteOrder.LittleEndian, 65532)]
    [InlineData(Fletcher4ByteOrder.BigEndian, 65532)]
    [InlineData(Fletcher4ByteOrder.LittleEndian, 65536)]
    [InlineData(Fletcher4ByteOrder.BigEndian, 65536)]
    [InlineData(Fletcher4ByteOrder.LittleEndian, 65540)]
    [InlineData(Fletcher4ByteOrder.BigEndian, 65540)]
    public void BoundaryPartitionsAndWrappingSumsMatchClosedFormOracle(Fletcher4ByteOrder order, int length)
    {
        byte[] data = length == 8192 ? Enumerable.Repeat((byte)255, length).ToArray() : BinaryInput(length);
        Fletcher4Value expected = Fletcher4Reference.Compute(data, order);
        Assert.Equal(expected, data.Fletcher4(order));
        foreach (int chunkSize in new[] { 1, 3, 4, 257, 65535, 65536 })
        {
            var accumulator = new Fletcher4Accumulator(order);
            for (int at = 0; at < length; at += chunkSize)
                accumulator.Append(data.AsSpan(at, Math.Min(chunkSize, length - at)));
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(65537)]
    public void OneShotCalculationsRejectIncompleteWords(int length)
    {
        byte[] data = BinaryInput(length);
        Assert.Throws<ArgumentException>(() => data.Fletcher4());
        Assert.Throws<ArgumentException>(() => new Fletcher4().Compute(data));
        Assert.Throws<ArgumentException>(() => new Fletcher4().Encode(data));
        Assert.Throws<ArgumentException>(() => data.WithFletcher4());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void InvalidByteOrdersAreRejected(int value)
    {
        var order = (Fletcher4ByteOrder)value;
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fletcher4(order));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fletcher4Accumulator(order));
        Assert.Throws<ArgumentOutOfRangeException>(() => new byte[4].Fletcher4(order));
        Assert.Throws<ArgumentOutOfRangeException>(() => new byte[4].WithFletcher4(order));
    }

    [Theory]
    [InlineData(Fletcher4ByteOrder.LittleEndian)]
    [InlineData(Fletcher4ByteOrder.BigEndian)]
    public void FramesRejectInvalidLengthsAndEverySingleBitMutation(Fletcher4ByteOrder order)
    {
        var detector = new Fletcher4(order);
        foreach (int length in new[] { 0, 1, 31, 32, 33, 34, 35, 37 })
        {
            Assert.True(detector.IsError(new byte[length]));
            Assert.Empty(detector.Decode(new byte[length]));
        }
        byte[] data = BinaryInput(12);
        byte[] frame = detector.Encode(data).ToArray();
        for (int index = 0; index < frame.Length; index++)
        for (int bit = 0; bit < 8; bit++)
        {
            frame[index] ^= (byte)(1 << bit);
            Assert.True(detector.IsError(frame));
            Assert.Empty(detector.Decode(frame));
            frame[index] ^= (byte)(1 << bit);
        }
        byte[] decoded = detector.Decode(frame).ToArray();
        Array.Fill(frame, (byte)255);
        Assert.Equal(data, decoded);
    }

    [Theory]
    [InlineData("Compute")]
    [InlineData("Encode")]
    [InlineData("Decode")]
    [InlineData("IsError")]
    [InlineData("Fletcher4")]
    [InlineData("WithFletcher4")]
    public void SequenceOperationsConsumeOnceAndPropagateEnumerationFailure(string operation)
    {
        Assert.Throws<ArgumentNullException>(() => Invoke(operation, null!));
        byte[] data = BinaryInput(12);
        byte[] input = operation is "Decode" or "IsError" ? data.WithFletcher4().ToArray() : data;
        var source = new SinglePassSource(input);
        Invoke(operation, source);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(1, source.DisposeCount);

        var failure = new IOException("Injected enumeration failure.");
        source = new SinglePassSource(input) { Failure = failure };
        Assert.Same(failure, Assert.Throws<IOException>(() => Invoke(operation, source)));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void ValueEqualityComparesAllFourSums()
    {
        var value = new Fletcher4Value(1, 2, 3, 4);
        var equal = new Fletcher4Value(1, 2, 3, 4);
        Assert.Equal(value, equal);
        Assert.True(value == equal);
        Assert.False(value != equal);
        Assert.Equal(value.GetHashCode(), equal.GetHashCode());
        Assert.True(value.Equals((object)equal));
        Assert.False(value.Equals(null));
        foreach (var different in new[] { new Fletcher4Value(0, 2, 3, 4), new Fletcher4Value(1, 0, 3, 4),
                     new Fletcher4Value(1, 2, 0, 4), new Fletcher4Value(1, 2, 3, 0) })
        {
            Assert.False(value == different);
            Assert.True(value != different);
        }
    }

    internal static byte[] BinaryInput(int length) => Enumerable.Range(0, length).Select(index => (byte)(index * 73 + (index >> 8) + 19)).ToArray();

    private static byte[] NativeDigest(Fletcher4Value value) => new[] { value.A, value.B, value.C, value.D }.SelectMany(BitConverter.GetBytes).ToArray();

    private static void Invoke(string operation, IEnumerable<byte> data)
    {
        var detector = new Fletcher4();
        switch (operation)
        {
            case "Compute": detector.Compute(data).ToArray(); break;
            case "Encode": detector.Encode(data).ToArray(); break;
            case "Decode": detector.Decode(data).ToArray(); break;
            case "IsError": detector.IsError(data); break;
            case "Fletcher4": data.Fletcher4(); break;
            case "WithFletcher4": data.WithFletcher4().ToArray(); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private sealed class SinglePassSource(byte[] bytes) : IEnumerable<byte>
    {
        public int EnumeratorCount { get; private set; }
        public int DisposeCount { get; private set; }
        public Exception? Failure { get; init; }
        public IEnumerator<byte> GetEnumerator()
        {
            if (++EnumeratorCount != 1) throw new InvalidOperationException("Input was enumerated twice.");
            return Enumerate().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private IEnumerable<byte> Enumerate()
        {
            try
            {
                for (int index = 0; index < bytes.Length; index++)
                {
                    if (index == 3 && Failure != null) throw Failure;
                    yield return bytes[index];
                }
            }
            finally { DisposeCount++; }
        }
    }
}
