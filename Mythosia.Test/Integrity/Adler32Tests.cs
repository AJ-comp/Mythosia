using System.Collections;
using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Adler32Tests
{
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    // Exact input bytes and frozen answers adapted from Go 1.25.0's golden
    // corpus. Empty input is included separately from the 42 nonempty inputs.
    // Marshal-state columns are not part of the imported test vectors.
    // https://github.com/golang/go/blob/6e676ab2b809d46623acb5988248d95d1eb7939c/src/hash/adler32/adler32_test.go#L20-L68
    // The \xff repetitions below are raw FF bytes, not UTF-8 encoded U+00FF.
    /*
     * Copyright 2009 The Go Authors.
     *
     * Redistribution and use in source and binary forms, with or without
     * modification, are permitted provided that the following conditions are
     * met:
     *
     *    * Redistributions of source code must retain the above copyright
     * notice, this list of conditions and the following disclaimer.
     *    * Redistributions in binary form must reproduce the above
     * copyright notice, this list of conditions and the following disclaimer
     * in the documentation and/or other materials provided with the
     * distribution.
     *    * Neither the name of Google LLC nor the names of its
     * contributors may be used to endorse or promote products derived from
     * this software without specific prior written permission.
     *
     * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
     * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
     * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
     * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
     * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
     * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
     * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
     * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
     * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
     * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
     * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
     */
    public static IEnumerable<object[]> PublishedVectors()
    {
        yield return new object[] { "Go golden 00", Encoding.ASCII.GetBytes(""), 0x00000001u };
        yield return new object[] { "Go golden 01", Encoding.ASCII.GetBytes("a"), 0x00620062u };
        yield return new object[] { "Go golden 02", Encoding.ASCII.GetBytes("ab"), 0x012600C4u };
        yield return new object[] { "Go golden 03", Encoding.ASCII.GetBytes("abc"), 0x024D0127u };
        yield return new object[] { "Go golden 04", Encoding.ASCII.GetBytes("abcd"), 0x03D8018Bu };
        yield return new object[] { "Go golden 05", Encoding.ASCII.GetBytes("abcde"), 0x05C801F0u };
        yield return new object[] { "Go golden 06", Encoding.ASCII.GetBytes("abcdef"), 0x081E0256u };
        yield return new object[] { "Go golden 07", Encoding.ASCII.GetBytes("abcdefg"), 0x0ADB02BDu };
        yield return new object[] { "Go golden 08", Encoding.ASCII.GetBytes("abcdefgh"), 0x0E000325u };
        yield return new object[] { "Go golden 09", Encoding.ASCII.GetBytes("abcdefghi"), 0x118E038Eu };
        yield return new object[] { "Go golden 10", Encoding.ASCII.GetBytes("abcdefghij"), 0x158603F8u };
        yield return new object[] { "Go golden 11", Encoding.ASCII.GetBytes("Discard medicine more than two years old."), 0x3F090F02u };
        yield return new object[] { "Go golden 12", Encoding.ASCII.GetBytes("He who has a shady past knows that nice guys finish last."), 0x46D81477u };
        yield return new object[] { "Go golden 13", Encoding.ASCII.GetBytes("I wouldn't marry him with a ten foot pole."), 0x40EE0EE1u };
        yield return new object[] { "Go golden 14", Encoding.ASCII.GetBytes("Free! Free!/A trip/to Mars/for 900/empty jars/Burma Shave"), 0x16661315u };
        yield return new object[] { "Go golden 15", Encoding.ASCII.GetBytes("The days of the digital watch are numbered.  -Tom Stoppard"), 0x5B2E1480u };
        yield return new object[] { "Go golden 16", Encoding.ASCII.GetBytes("Nepal premier won't resign."), 0x8C3C09EAu };
        yield return new object[] { "Go golden 17", Encoding.ASCII.GetBytes("For every action there is an equal and opposite government program."), 0x45AC18FDu };
        yield return new object[] { "Go golden 18", Encoding.ASCII.GetBytes("His money is twice tainted: 'taint yours and 'taint mine."), 0x53C61462u };
        yield return new object[] { "Go golden 19", Encoding.ASCII.GetBytes("There is no reason for any individual to have a computer in their home. -Ken Olsen, 1977"), 0x7E511E63u };
        yield return new object[] { "Go golden 20", Encoding.ASCII.GetBytes("It's a tiny change to the code and not completely disgusting. - Bob Manchek"), 0xE4801A6Au };
        yield return new object[] { "Go golden 21", Encoding.ASCII.GetBytes("size:  a.out:  bad magic"), 0x61B507DFu };
        yield return new object[] { "Go golden 22", Encoding.ASCII.GetBytes("The major problem is with sendmail.  -Mark Horton"), 0xB8631171u };
        yield return new object[] { "Go golden 23", Encoding.ASCII.GetBytes("Give me a rock, paper and scissors and I will move the world.  CCFestoon"), 0x8B5E1904u };
        yield return new object[] { "Go golden 24", Encoding.ASCII.GetBytes("If the enemy is within range, then so are you."), 0x7CC6102Bu };
        yield return new object[] { "Go golden 25", Encoding.ASCII.GetBytes("It's well we cannot hear the screams/That we create in others' dreams."), 0x700318E7u };
        yield return new object[] { "Go golden 26", Encoding.ASCII.GetBytes("You remind me of a TV show, but that's all right: I watch it anyway."), 0x1E601747u };
        yield return new object[] { "Go golden 27", Encoding.ASCII.GetBytes("C is as portable as Stonehedge!!"), 0xB55B0B09u };
        yield return new object[] { "Go golden 28", Encoding.ASCII.GetBytes("Even if I could be Shakespeare, I think I should still choose to be Faraday. - A. Huxley"), 0x39111DD0u };
        yield return new object[] { "Go golden 29", Encoding.ASCII.GetBytes("The fugacity of a constituent in a mixture of gases at a given temperature is proportional to its mole fraction.  Lewis-Randall Rule"), 0x91DD304Fu };
        yield return new object[] { "Go golden 30", Encoding.ASCII.GetBytes("How can you write a big system without C++?  -Paul Glick"), 0x2E5D1316u };
        yield return new object[] { "Go golden 31", Encoding.ASCII.GetBytes("'Invariant assertions' is the most elegant programming technique!  -Tom Szymanski"), 0xD0201DF6u };
        yield return new object[] { "Go golden 32", RepeatPattern(new byte[] { 0xFF }, 5548, "8"), 0x211297C8u };
        yield return new object[] { "Go golden 33", RepeatPattern(new byte[] { 0xFF }, 5549, "9"), 0xBAA198C8u };
        yield return new object[] { "Go golden 34", RepeatPattern(new byte[] { 0xFF }, 5550, "0"), 0x553499BEu };
        yield return new object[] { "Go golden 35", RepeatPattern(new byte[] { 0xFF }, 5551, "1"), 0xF0C19ABEu };
        yield return new object[] { "Go golden 36", RepeatPattern(new byte[] { 0xFF }, 5552, "2"), 0x8D5C9BBEu };
        yield return new object[] { "Go golden 37", RepeatPattern(new byte[] { 0xFF }, 5553, "3"), 0x2AF69CBEu };
        yield return new object[] { "Go golden 38", RepeatPattern(new byte[] { 0xFF }, 5554, "4"), 0xC9809DBEu };
        yield return new object[] { "Go golden 39", RepeatPattern(new byte[] { 0xFF }, 5555, "5"), 0x69189EBEu };
        yield return new object[] { "Go golden 40", RepeatPattern(new byte[] { 0 }, 100000, ""), 0x86AF0001u };
        yield return new object[] { "Go golden 41", RepeatPattern(Encoding.ASCII.GetBytes("a"), 100000, ""), 0x79660B4Du };
        yield return new object[] { "Go golden 42", RepeatPattern(Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), 10000, ""), 0x110588EEu };
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public async Task PublishedAnswersMatchIndependentOracleNumericDigestFramingAndStreaming(
        string name, byte[] input, uint expected)
    {
        Assert.Equal(expected, Adler32Reference.Compute(input));
        Assert.Equal(expected, input.Adler32());
        var detector = new Adler32();
        byte[] digest = NativeBytes(expected);
        byte[] frame = input.Concat(digest).ToArray();
        Assert.Equal(digest, detector.Compute(input));
        Assert.Equal(frame, detector.Encode(input));
        Assert.Equal(frame, input.WithAdler32());
        Assert.Equal(input.Length == 0, detector.IsError(frame));
        Assert.Equal(input, detector.Decode(frame));

        var accumulator = new Adler32Accumulator();
        foreach (int split in SplitPositions(input.Length))
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(input.AsSpan(split));
            Assert.True(expected == accumulator.GetCurrentValue(), $"{name}, split {split}");
        }

        using var synchronous = new MemoryStream(input, writable: false);
        using var asynchronous = new MemoryStream(input, writable: false);
        Assert.Equal(expected, synchronous.ComputeAdler32());
        Assert.Equal(expected, await asynchronous.ComputeAdler32Async());
    }

    [Fact]
    public void PublishedCorpusContainsAtLeastThirtyDistinctNonemptyInputs()
    {
        string[] inputs = PublishedVectors().Select(vector => (byte[])vector[1])
            .Where(input => input.Length != 0).Select(Convert.ToHexString).ToArray();

        Assert.True(inputs.Length >= 30, "Adler-32 requires at least 30 published nonempty inputs.");
        Assert.Equal(inputs.Length, inputs.Distinct(StringComparer.Ordinal).Count());
        Assert.Single(PublishedVectors().Where(vector => ((byte[])vector[1]).Length == 0));
    }

    public static IEnumerable<object[]> BinaryBoundaryCases()
    {
        int[] lengths = { 0, 1, 15, 255, 256, 257, 5551, 5552, 5553, 11103, 11104, 11105,
            65520, 65521, 65522, 65535, 65536, 65537, 1048577 };
        foreach (int length in lengths)
        foreach (bool allMaximumBytes in new[] { false, true })
            yield return new object[] { length, allMaximumBytes };
    }

    [Theory]
    [MemberData(nameof(BinaryBoundaryCases))]
    public void BlockModulusAndBufferBoundariesMatchWeightedOracleForWholeAndChunkedInputs(
        int length, bool allMaximumBytes)
    {
        byte[] input = BinaryInput(length);
        if (allMaximumBytes) Array.Fill(input, (byte)0xFF);
        uint expected = Adler32Reference.Compute(input);
        Assert.Equal(expected, input.Adler32());
        Assert.Equal(expected, new LinkedList<byte>(input).Adler32());

        var accumulator = new Adler32Accumulator();
        foreach (int chunkLength in new[] { 1, 257, 5551, 5552, 5553, 65536 })
        {
            accumulator.Reset();
            for (int offset = 0; offset < input.Length; offset += chunkLength)
                accumulator.Append(input.AsSpan(offset, Math.Min(chunkLength, input.Length - offset)));
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }
    }

    [Fact]
    public void EveryByteValueAndEveryShortSplitAreCovered()
    {
        var accumulator = new Adler32Accumulator();
        for (int value = 0; value <= byte.MaxValue; value++)
        {
            byte[] input = { (byte)value };
            uint expected = (uint)(value + 1) * 65537u;
            Assert.Equal(expected, input.Adler32());
            accumulator.Reset();
            accumulator.Append(input);
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }

        foreach (int length in new[] { 2, 3, 7, 31, 127, 257 })
        {
            byte[] input = BinaryInput(length);
            uint expected = Adler32Reference.Compute(input);
            for (int split = 0; split <= input.Length; split++)
            {
                accumulator.Reset();
                accumulator.Append(input.AsSpan(0, split));
                accumulator.Append(input.AsSpan(split));
                Assert.Equal(expected, accumulator.GetCurrentValue());
            }
        }
    }

    [Fact]
    public void SeededPartitionsPreservePrefixStateAcrossSnapshotsEmptyAppendsAndReset()
    {
        byte[] input = BinaryInput(2 * 65536 + 23);
        var random = new Random(1950);
        var accumulator = new Adler32Accumulator();
        Assert.Equal(1u, accumulator.GetCurrentValue());
        for (int offset = 0; offset < input.Length;)
        {
            int length = Math.Min(random.Next(1, 8193), input.Length - offset);
            accumulator.Append(input.AsSpan(offset, length));
            offset += length;
            uint expected = Adler32Reference.Compute(input.AsSpan(0, offset));
            Assert.Equal(expected, accumulator.GetCurrentValue());
            Assert.Equal(expected, accumulator.GetCurrentValue());
            accumulator.Append(default);
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }

        accumulator.Reset();
        accumulator.Append(ReadOnlySpan<byte>.Empty);
        Assert.Equal(1u, accumulator.GetCurrentValue());
        accumulator.Append(CheckInput);
        Assert.Equal(0x091E01DEu, accumulator.GetCurrentValue());
        accumulator.Reset();
        Assert.Equal(1u, accumulator.GetCurrentValue());
    }

    [Fact]
    public void NativeDigestBytesAreExplicitAndIndependentFromZlibNetworkOrder()
    {
        // RFC 1950 numeric checksum for these bytes is 0x091E01DE. Its zlib
        // trailer is 09 1E 01 DE, while the common ErrorDetection API uses
        // native-endian digest framing, like CRC32/CRC32C.
        byte[] native = BitConverter.IsLittleEndian
            ? new byte[] { 0xDE, 0x01, 0x1E, 0x09 }
            : new byte[] { 0x09, 0x1E, 0x01, 0xDE };
        ErrorDetection detector = new Adler32();
        Assert.Equal("Adler32", detector.GetDetectionType());
        Assert.Equal(0x091E01DEu, CheckInput.Adler32());
        Assert.Equal(native, detector.Compute(CheckInput));
        Assert.Equal(CheckInput.Concat(native), detector.Encode(CheckInput));
        Assert.False(detector.IsError(CheckInput.Concat(native)));
        Assert.Equal(CheckInput, detector.Decode(CheckInput.Concat(native)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ShortAndDigestOnlyFramesAreRejected(int length)
    {
        var detector = new Adler32();
        byte[] frame = NativeBytes(1).Take(length).ToArray();
        Assert.True(detector.IsError(frame));
        Assert.Empty(detector.Decode(frame));
        Assert.Equal(NativeBytes(1), detector.Encode(Array.Empty<byte>()));
    }

    [Fact]
    public void EverySingleBitChangeInPayloadOrDigestIsRejected()
    {
        var detector = new Adler32();
        byte[] valid = CheckInput.Concat(NativeBytes(0x091E01DE)).ToArray();
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
    [InlineData("Adler32")]
    [InlineData("WithAdler32")]
    public void SequenceOperationsRejectNullAndReadSinglePassInputsOnlyOnce(string operation)
    {
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(() => Invoke(operation, null!));
        string parameter = operation switch
        {
            "Decode" or "IsError" => "sourceWithCRC",
            "Adler32" or "WithAdler32" => "data",
            _ => "source"
        };
        Assert.Equal(parameter, failure.ParamName);
        byte[] input = operation is "Decode" or "IsError"
            ? CheckInput.Concat(NativeBytes(0x091E01DE)).ToArray()
            : CheckInput;
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
    [InlineData("Adler32")]
    [InlineData("WithAdler32")]
    public void EnumerationFailurePreservesExceptionAndDisposesInput(string operation)
    {
        var injected = new InvalidOperationException("Injected Adler-32 enumeration failure.");
        var source = new SinglePassSource(CheckInput) { Failure = injected };

        var failure = Assert.Throws<InvalidOperationException>(() => Invoke(operation, source));

        Assert.Same(injected, failure);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void ReusedDetectorReturnsIndependentResultsWithoutMutatingInputs()
    {
        var detector = new Adler32();
        byte[] input = (byte[])CheckInput.Clone();
        byte[] expectedDigest = NativeBytes(0x091E01DE);
        byte[] digest = detector.Compute(input).ToArray();
        byte[] frame = detector.Encode(input).ToArray();
        byte[] payload = detector.Decode(frame).ToArray();
        Assert.Equal(CheckInput, input);
        Array.Fill(input, (byte)0xFF);
        Array.Fill(frame, (byte)0);
        Assert.Equal(expectedDigest, digest);
        Assert.Equal(CheckInput, payload);
        Assert.Equal(expectedDigest, detector.Compute(CheckInput));
        Assert.Equal(1u, Array.Empty<byte>().Adler32());
    }

    private static void Invoke(string operation, IEnumerable<byte> source)
    {
        var detector = new Adler32();
        byte[] digest = NativeBytes(0x091E01DE);
        byte[] frame = CheckInput.Concat(digest).ToArray();
        switch (operation)
        {
            case "Compute": Assert.Equal(digest, detector.Compute(source)); break;
            case "Encode": Assert.Equal(frame, detector.Encode(source)); break;
            case "Decode": Assert.Equal(CheckInput, detector.Decode(source)); break;
            case "IsError": Assert.False(detector.IsError(source)); break;
            case "Adler32": Assert.Equal(0x091E01DEu, source.Adler32()); break;
            case "WithAdler32": Assert.Equal(frame, source.WithAdler32()); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static IEnumerable<int> SplitPositions(int length)
    {
        if (length <= 128) return Enumerable.Range(0, length + 1);
        return new[] { 0, 1, length / 2, length - 1, length, 5551, 5552, 5553, 65535, 65536, 65537 }
            .Where(split => split <= length).Distinct();
    }

    internal static byte[] BinaryInput(int length)
    {
        var bytes = new byte[length];
        uint state = 0xAD1E1950;
        for (int index = 0; index < bytes.Length; index++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            bytes[index] = (byte)state;
        }
        return bytes;
    }

    private static byte[] NativeBytes(uint value)
    {
        var bytes = new byte[4];
        for (int index = 0; index < bytes.Length; index++)
            bytes[BitConverter.IsLittleEndian ? index : bytes.Length - index - 1] = (byte)(value >> (8 * index));
        return bytes;
    }

    private static byte[] RepeatPattern(byte[] pattern, int repetitions, string suffix)
    {
        byte[] ending = Encoding.ASCII.GetBytes(suffix);
        var bytes = new byte[checked(pattern.Length * repetitions + ending.Length)];
        for (int offset = 0; offset < pattern.Length * repetitions; offset += pattern.Length)
            pattern.CopyTo(bytes, offset);
        ending.CopyTo(bytes, pattern.Length * repetitions);
        return bytes;
    }

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
