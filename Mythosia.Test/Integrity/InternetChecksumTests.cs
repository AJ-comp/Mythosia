using System.Collections;
using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class InternetChecksumTests
{
    private static readonly byte[] Rfc1071Input = Convert.FromHexString("0001F203F4F5F6F7");
    private const ushort Rfc1071Checksum = 0x220D;

    public static IEnumerable<object[]> PublishedVectors() => InternetChecksumPublishedVectors.Cases();

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public async Task PublishedPacketAnswersMatchNumericDigestFramingAccumulationAndStreaming(
        string name, byte[] input, ushort expected)
    {
        Assert.Equal(expected, InternetChecksumReference.Compute(input));
        Assert.Equal(expected, input.InternetChecksum());
        var detector = new InternetChecksum();
        byte[] digest = NativeBytes(expected);
        byte[] frame = input.Concat(digest).ToArray();
        Assert.Equal(digest, detector.Compute(input));
        Assert.Equal(frame, detector.Encode(input));
        Assert.Equal(frame, input.WithInternetChecksum());
        Assert.False(detector.IsError(frame));
        Assert.Equal(input, detector.Decode(frame));

        var accumulator = new InternetChecksumAccumulator();
        for (int split = 0; split <= input.Length; split++)
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            ushort prefix = InternetChecksumReference.Compute(input.AsSpan(0, split));
            Assert.Equal(prefix, accumulator.GetCurrentValue());
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            Assert.Equal(prefix, accumulator.GetCurrentValue());
            accumulator.Append(input.AsSpan(split));
            Assert.True(expected == accumulator.GetCurrentValue(), $"{name}, split {split}");
        }

        using var synchronous = new MemoryStream(input, writable: false);
        using var asynchronous = new MemoryStream(input, writable: false);
        Assert.Equal(expected, synchronous.ComputeInternetChecksum());
        Assert.Equal(expected, await asynchronous.ComputeInternetChecksumAsync());
    }

    [Fact]
    public void PublishedCorpusContainsAtLeastThirtyDistinctNonemptyFixedAnswers()
    {
        string[] inputs = PublishedVectors().Select(vector => (byte[])vector[1])
            .Where(input => input.Length != 0).Select(Convert.ToHexString).ToArray();

        Assert.True(inputs.Length >= 30, "Internet Checksum requires at least 30 published nonempty inputs.");
        Assert.Equal(inputs.Length, inputs.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Rfc1071ExampleUsesNetworkOrderWordsAndFinalComplement()
    {
        // RFC 1071 section 3 gives the folded sum DDF2 for these eight bytes;
        // the transmitted Internet checksum is its one's complement, 220D.
        // https://www.rfc-editor.org/rfc/rfc1071.html#section-3
        Assert.Equal(Rfc1071Checksum, Rfc1071Input.InternetChecksum());
        Assert.Equal(Rfc1071Checksum, InternetChecksumReference.Compute(Rfc1071Input));
        Assert.Equal((ushort)0, Rfc1071Input.Concat(new byte[] { 0x22, 0x0D }).InternetChecksum());
    }

    [Theory]
    [InlineData("", 0xFFFF)]
    [InlineData("00", 0xFFFF)]
    [InlineData("FF", 0x00FF)]
    [InlineData("FFFF", 0x0000)]
    [InlineData("FFFFFFFF", 0x0000)]
    [InlineData("FFFFFF", 0x00FF)]
    [InlineData("FFFE0001", 0x0000)]
    [InlineData("FFFF0001", 0xFFFE)]
    [InlineData("FFFFFFFF0001", 0xFFFE)]
    [InlineData("010203", 0xFBFD)]
    public void OddPaddingCarriesAndBothRepresentationsOfZeroArePreserved(string hex, int answer)
    {
        byte[] input = Convert.FromHexString(hex);
        ushort expected = (ushort)answer;
        Assert.Equal(expected, input.InternetChecksum());
        Assert.Equal(expected, InternetChecksumReference.Compute(input));
        var accumulator = new InternetChecksumAccumulator();
        foreach (byte value in input)
        {
            accumulator.Append(new byte[] { value });
            _ = accumulator.GetCurrentValue();
        }
        Assert.Equal(expected, accumulator.GetCurrentValue());
    }

    public static IEnumerable<object[]> BinaryBoundaryCases()
    {
        foreach (int length in new[] { 0, 1, 2, 3, 255, 256, 257, 65535, 65536, 65537, 1048577 })
        foreach (bool allMaximumBytes in new[] { false, true })
            yield return new object[] { length, allMaximumBytes };
    }

    [Theory]
    [MemberData(nameof(BinaryBoundaryCases))]
    public void OddAndStreamBufferBoundariesMatchIndependentOracle(int length, bool allMaximumBytes)
    {
        byte[] input = BinaryInput(length);
        if (allMaximumBytes) Array.Fill(input, (byte)0xFF);
        ushort expected = InternetChecksumReference.Compute(input);
        Assert.Equal(expected, input.InternetChecksum());
        Assert.Equal(expected, new LinkedList<byte>(input).InternetChecksum());

        var accumulator = new InternetChecksumAccumulator();
        foreach (int chunkLength in new[] { 1, 3, 257, 65535, 65536 })
        {
            accumulator.Reset();
            for (int offset = 0; offset < input.Length; offset += chunkLength)
                accumulator.Append(input.AsSpan(offset, Math.Min(chunkLength, input.Length - offset)));
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }
    }

    [Fact]
    public void EveryOneAndTwoByteValueMatchesDirectMathematicalAnswer()
    {
        var accumulator = new InternetChecksumAccumulator();
        for (int word = 0; word <= ushort.MaxValue; word++)
        {
            byte[] input = { (byte)(word >> 8), (byte)word };
            ushort expected = (ushort)(word ^ 0xFFFF);
            Assert.Equal(expected, input.InternetChecksum());
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, 1));
            Assert.Equal((ushort)((input[0] << 8) ^ 0xFFFF), accumulator.GetCurrentValue());
            accumulator.Append(input.AsSpan(1));
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }
    }

    [Fact]
    public void SeededPartitionsAndSnapshotsPreservePendingOddByteUntilNextAppend()
    {
        byte[] input = BinaryInput(2 * 65536 + 23);
        var random = new Random(1071);
        var accumulator = new InternetChecksumAccumulator();
        Assert.Equal(ushort.MaxValue, accumulator.GetCurrentValue());
        for (int offset = 0; offset < input.Length;)
        {
            int length = Math.Min(random.Next(1, 2049), input.Length - offset);
            accumulator.Append(input.AsSpan(offset, length));
            offset += length;
            ushort expected = InternetChecksumReference.Compute(input.AsSpan(0, offset));
            Assert.Equal(expected, accumulator.GetCurrentValue());
            Assert.Equal(expected, accumulator.GetCurrentValue());
            accumulator.Append(default);
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }

        accumulator.Reset();
        accumulator.Append(new byte[] { 0x12 });
        Assert.Equal((ushort)0xEDFF, accumulator.GetCurrentValue());
        accumulator.Reset(); // Reset must clear a pending high byte as well as the sum.
        Assert.Equal(ushort.MaxValue, accumulator.GetCurrentValue());
        accumulator.Append(Rfc1071Input);
        Assert.Equal(Rfc1071Checksum, accumulator.GetCurrentValue());
        accumulator.Reset();
        Assert.Equal(ushort.MaxValue, accumulator.GetCurrentValue());
    }

    [Fact]
    public void LongMaximumInputCannotOverflowAnUnfolded32BitSum()
    {
        // 131,076 bytes of FF exceed uint.MaxValue when all 16-bit words are
        // summed without carries. Repeating that input also crosses many reads.
        byte[] input = Enumerable.Repeat((byte)0xFF, 4 * 131076 + 1).ToArray();
        Assert.Equal((ushort)0x00FF, input.InternetChecksum());
        var accumulator = new InternetChecksumAccumulator();
        for (int index = 0; index < input.Length; index += 257)
            accumulator.Append(input.AsSpan(index, Math.Min(257, input.Length - index)));
        Assert.Equal((ushort)0x00FF, accumulator.GetCurrentValue());
    }

    [Fact]
    public void CommonApiDigestUsesNativeEndianAndFrameComparisonWorksForOddPayloads()
    {
        ErrorDetection detector = new InternetChecksum();
        Assert.Equal("InternetChecksum", detector.GetDetectionType());
        Assert.Equal(NativeBytes(Rfc1071Checksum), detector.Compute(Rfc1071Input));
        byte[] odd = { 0x01, 0x02, 0x03 };
        byte[] digest = BitConverter.IsLittleEndian
            ? new byte[] { 0xFD, 0xFB }
            : new byte[] { 0xFB, 0xFD };
        byte[] frame = odd.Concat(digest).ToArray();
        Assert.Equal(digest, detector.Compute(odd));
        Assert.Equal(frame, detector.Encode(odd));
        Assert.False(detector.IsError(frame));
        Assert.Equal(odd, detector.Decode(frame));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ShortAndDigestOnlyFramesAreRejected(int length)
    {
        var detector = new InternetChecksum();
        byte[] frame = NativeBytes(ushort.MaxValue).Take(length).ToArray();
        Assert.True(detector.IsError(frame));
        Assert.Empty(detector.Decode(frame));
        Assert.Equal(NativeBytes(ushort.MaxValue), detector.Encode(Array.Empty<byte>()));
    }

    [Fact]
    public void EverySingleBitChangeInPayloadOrDigestIsRejected()
    {
        var detector = new InternetChecksum();
        byte[] valid = Rfc1071Input.Concat(NativeBytes(Rfc1071Checksum)).ToArray();
        for (int index = 0; index < valid.Length; index++)
        for (int bit = 0; bit < 8; bit++)
        {
            byte[] changed = (byte[])valid.Clone();
            changed[index] ^= (byte)(1 << bit);
            Assert.True(detector.IsError(changed));
            Assert.Empty(detector.Decode(changed));
        }
    }

    [Theory]
    [InlineData("Compute")]
    [InlineData("Encode")]
    [InlineData("Decode")]
    [InlineData("IsError")]
    [InlineData("InternetChecksum")]
    [InlineData("WithInternetChecksum")]
    public void SequenceOperationsRejectNullAndReadSinglePassInputsOnlyOnce(string operation)
    {
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(() => Invoke(operation, null!));
        string parameter = operation switch
        {
            "Decode" or "IsError" => "sourceWithCRC",
            "InternetChecksum" or "WithInternetChecksum" => "data",
            _ => "source"
        };
        Assert.Equal(parameter, failure.ParamName);
        byte[] input = operation is "Decode" or "IsError"
            ? Rfc1071Input.Concat(NativeBytes(Rfc1071Checksum)).ToArray()
            : Rfc1071Input;
        var source = new SinglePassSource(input);
        Invoke(operation, source);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(input.Length, source.YieldCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData("Compute")]
    [InlineData("Encode")]
    [InlineData("Decode")]
    [InlineData("IsError")]
    [InlineData("InternetChecksum")]
    [InlineData("WithInternetChecksum")]
    public void EnumerationFailurePreservesExceptionAndDisposesInput(string operation)
    {
        var injected = new InvalidOperationException("Injected Internet Checksum enumeration failure.");
        var source = new SinglePassSource(Rfc1071Input) { Failure = injected };
        var failure = Assert.Throws<InvalidOperationException>(() => Invoke(operation, source));
        Assert.Same(injected, failure);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void ReusedDetectorReturnsIndependentResultsWithoutMutatingInput()
    {
        var detector = new InternetChecksum();
        byte[] input = (byte[])Rfc1071Input.Clone();
        byte[] digest = detector.Compute(input).ToArray();
        byte[] frame = detector.Encode(input).ToArray();
        byte[] payload = detector.Decode(frame).ToArray();
        Assert.Equal(Rfc1071Input, input);
        Array.Fill(input, (byte)0xFF);
        Array.Fill(frame, (byte)0);
        Assert.Equal(NativeBytes(Rfc1071Checksum), digest);
        Assert.Equal(Rfc1071Input, payload);
        Assert.Equal(NativeBytes(Rfc1071Checksum), detector.Compute(Rfc1071Input));
    }

    private static void Invoke(string operation, IEnumerable<byte> source)
    {
        var detector = new InternetChecksum();
        byte[] digest = NativeBytes(Rfc1071Checksum);
        byte[] frame = Rfc1071Input.Concat(digest).ToArray();
        switch (operation)
        {
            case "Compute": Assert.Equal(digest, detector.Compute(source)); break;
            case "Encode": Assert.Equal(frame, detector.Encode(source)); break;
            case "Decode": Assert.Equal(Rfc1071Input, detector.Decode(source)); break;
            case "IsError": Assert.False(detector.IsError(source)); break;
            case "InternetChecksum": Assert.Equal(Rfc1071Checksum, source.InternetChecksum()); break;
            case "WithInternetChecksum": Assert.Equal(frame, source.WithInternetChecksum()); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    internal static byte[] BinaryInput(int length)
    {
        var bytes = new byte[length];
        uint state = 0x1071C0DE;
        for (int index = 0; index < bytes.Length; index++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            bytes[index] = (byte)state;
        }
        return bytes;
    }

    private static byte[] NativeBytes(ushort value) => BitConverter.IsLittleEndian
        ? new byte[] { (byte)value, (byte)(value >> 8) }
        : new byte[] { (byte)(value >> 8), (byte)value };

    private sealed class SinglePassSource(byte[] bytes) : IEnumerable<byte>
    {
        public int EnumeratorCount { get; private set; }
        public int YieldCount { get; private set; }
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
                foreach (byte value in bytes)
                {
                    if (YieldCount == 3 && Failure != null) throw Failure;
                    YieldCount++;
                    yield return value;
                }
            }
            finally { DisposeCount++; }
        }
    }
}
