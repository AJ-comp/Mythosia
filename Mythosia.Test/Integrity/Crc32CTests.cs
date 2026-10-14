using System.Collections;
using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Crc32CTests
{
    private const int BufferBoundary = 64 * 1024;
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    public enum SourceKind
    {
        Array,
        List,
        LinkedList,
        Iterator,
        SinglePass
    }

    public static IEnumerable<object[]> PublishedVectors()
    {
        yield return new object[] { "empty", Array.Empty<byte>(), 0u };
        yield return new object[] { "RevEng check", CheckInput, 0xE3069283u };
        foreach (object[] vector in RfcVectors())
        {
            byte[] bytes = Convert.FromHexString((string)vector[2]);
            uint value = bytes[0] | (uint)bytes[1] << 8 | (uint)bytes[2] << 16 | (uint)bytes[3] << 24;
            yield return new object[] { vector[0], vector[1], value };
        }

        // Google's portable implementation tests freeze this independently
        // published 2048-byte fixture for a longer deterministic binary message.
        // https://github.com/google/crc32c/blob/1.1.2/src/crc32c_extend_unittests.h
        byte[] googleInput = Enumerable.Range(0, 2048)
            .Select(i => unchecked((byte)(3 * i * i + 7 * i + 11))).ToArray();
        yield return new object[] { "Google 2048-byte pattern", googleInput, 0x36DCC753u };

        foreach (object[] vector in GoPublishedVectors())
            yield return vector;
    }

    // Adapted from the castagnoli column of Go's fixed golden answers. Inputs
    // are ASCII without a terminator; punctuation and double spaces are exact.
    // The IEEE column and marshaled hash states are deliberately not imported.
    // https://github.com/golang/go/blob/go1.25.0/src/hash/crc32/crc32_test.go#L38-L69
    // Castagnoli = reflected polynomial 82F63B78; initial/final XOR FFFFFFFF.
    // https://github.com/golang/go/blob/go1.25.0/src/hash/crc32/crc32.go
    /*
     * Copyright 2009 The Go Authors.
     *
     * Redistribution and use in source and binary forms, with or without
     * modification, are permitted provided that the following conditions are
     * met:
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
    private static IEnumerable<object[]> GoPublishedVectors()
    {
        var vectors = new (string Input, uint Expected)[]
        {
            ("a", 0xC1D04330u),
            ("ab", 0xE2A22936u),
            ("abc", 0x364B3FB7u),
            ("abcd", 0x92C80A31u),
            ("abcde", 0xC450D697u),
            ("abcdef", 0x53BCEFF1u),
            ("abcdefg", 0xE627F441u),
            ("abcdefgh", 0x0A9421B7u),
            ("abcdefghi", 0x2DDC99FCu),
            ("abcdefghij", 0xE6599437u),
            ("Discard medicine more than two years old.", 0xB2CC01FEu),
            ("He who has a shady past knows that nice guys finish last.", 0x0E28207Fu),
            ("I wouldn't marry him with a ten foot pole.", 0xBE93F964u),
            ("Free! Free!/A trip/to Mars/for 900/empty jars/Burma Shave", 0x9E3BE0C3u),
            ("The days of the digital watch are numbered.  -Tom Stoppard", 0xF505EF04u),
            ("Nepal premier won't resign.", 0x85D3DC82u),
            ("For every action there is an equal and opposite government program.", 0xC5142380u),
            ("His money is twice tainted: 'taint yours and 'taint mine.", 0x75EB77DDu),
            ("There is no reason for any individual to have a computer in their home. -Ken Olsen, 1977", 0x91EBE9F7u),
            ("It's a tiny change to the code and not completely disgusting. - Bob Manchek", 0xF0B1168Eu),
            ("size:  a.out:  bad magic", 0x572B74E2u),
            ("The major problem is with sendmail.  -Mark Horton", 0x8A58A6D5u),
            ("Give me a rock, paper and scissors and I will move the world.  CCFestoon", 0x9C426C50u),
            ("If the enemy is within range, then so are you.", 0x735400A4u),
            ("It's well we cannot hear the screams/That we create in others' dreams.", 0xBEC49C95u),
            ("You remind me of a TV show, but that's all right: I watch it anyway.", 0xA95A2079u),
            ("C is as portable as Stonehedge!!", 0xDE2E65C5u),
            ("Even if I could be Shakespeare, I think I should still choose to be Faraday. - A. Huxley", 0x297A88EDu),
            ("The fugacity of a constituent in a mixture of gases at a given temperature is proportional to its mole fraction.  Lewis-Randall Rule", 0x66ED1D8Bu),
            ("How can you write a big system without C++?  -Paul Glick", 0xDCDED527u)
        };
        foreach ((string input, uint expected) in vectors)
            yield return new object[] { "Go " + input, Encoding.ASCII.GetBytes(input), expected };
    }

    public static IEnumerable<object[]> RfcVectors()
    {
        // RFC 7143 A.4 prints CRC octets, not a hexadecimal uint. These
        // strings retain that exact ordering and are tested independently
        // from the numeric representation and host-native digest packing.
        // https://www.rfc-editor.org/rfc/rfc7143.html#appendix-A.4
        yield return new object[] { "RFC zero bytes", new byte[32], "AA36918A" };
        yield return new object[] { "RFC FF bytes", Enumerable.Repeat((byte)0xFF, 32).ToArray(), "43ABA862" };
        yield return new object[] { "RFC increasing", Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(), "4E79DD46" };
        yield return new object[] { "RFC decreasing", Enumerable.Range(0, 32).Select(i => (byte)(31 - i)).ToArray(), "5CDB3F11" };
        yield return new object[]
        {
            "RFC SCSI Read(10)",
            Convert.FromHexString(
                "01C00000000000000000000000000000" +
                "14000000000004000000001400000018" +
                "28000000000000000200000000000000"),
            "563A96D9"
        };
    }

    public static IEnumerable<object[]> SourcesAndLengths()
    {
        foreach (SourceKind kind in Enum.GetValues<SourceKind>())
        foreach (int length in new[] { 0, 1, 3, 9, 255, 256, 257, 4097 })
            yield return new object[] { kind, length };
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public async Task PublishedAnswersCalibrateIndependentOracleAndAllNumericPaths(string name, byte[] input, uint expected)
    {
        Assert.Equal(expected, Crc32CReference.Compute(input));
        Assert.Equal(expected, input.CRC32C());
        Assert.Equal(NativeBytes(expected), new CRC32C().Compute(input).ToArray());

        var accumulator = new CRC32CAccumulator();
        AppendInChunks(accumulator, input, 3);
        AssertValue(expected, accumulator.GetCurrentValue(), name);

        for (int split = 0; split <= input.Length; split++)
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(input.AsSpan(split));
            AssertValue(expected, accumulator.GetCurrentValue(), $"{name}, split {split}");
        }

        using var synchronous = new MemoryStream(input, writable: false);
        using var asynchronous = new MemoryStream(input, writable: false);
        Assert.Equal(expected, synchronous.ComputeCRC32C());
        Assert.Equal(expected, await asynchronous.ComputeCRC32CAsync());
    }

    [Fact]
    public void PublishedCorpusHasAtLeastThirtyDistinctNonemptyInputs()
    {
        string[] inputs = PublishedVectors().Select(vector => (byte[])vector[1])
            .Where(input => input.Length > 0).Select(Convert.ToHexString).ToArray();
        Assert.True(inputs.Length >= 30, "CRC32C requires at least 30 published nonempty inputs.");
        Assert.Equal(inputs.Length, inputs.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [MemberData(nameof(RfcVectors))]
    public void RfcDigestOctetsMatchExplicitHostNativePacking(string name, byte[] input, string rfcOctets)
    {
        byte[] expected = Convert.FromHexString(rfcOctets);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(expected);

        Assert.Equal(expected, new CRC32C().Compute(input).ToArray());
        Assert.Equal(input.Concat(expected), new CRC32C().Encode(input));
        Assert.False(new CRC32C().IsError(input.Concat(expected)), name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(257)]
    public void LittleEndianCodewordsHaveThePublishedCrc32CResidue(int length)
    {
        // Catalogue residue B798B438 is the uncomplemented reflected register.
        // Our finalized numeric API XORs FFFFFFFF, yielding 48674BC7. RFC
        // codeword bytes are explicitly little-endian, independent of the host.
        byte[] input = DeterministicBytes(length, 0x1BADB002);
        uint crc = Crc32CReference.Compute(input);
        byte[] codeword = input.Concat(LittleEndianBytes(crc)).ToArray();
        Assert.Equal(0x48674BC7u, Crc32CReference.Compute(codeword));
        Assert.Equal(0x48674BC7u, codeword.CRC32C());
    }

    [Theory]
    [MemberData(nameof(SourcesAndLengths))]
    public void EnumerableShapesProduceIndependentNumericDigestAndFrameResults(SourceKind kind, int length)
    {
        byte[] payload = DeterministicBytes(length, 0xC001D00D);
        uint expected = Crc32CReference.Compute(payload);
        byte[] expectedDigest = NativeBytes(expected);
        byte[] expectedFrame = payload.Concat(expectedDigest).ToArray();
        var detector = new CRC32C();

        Assert.Equal(expected, CreateSource(kind, payload).CRC32C());
        Assert.Equal(expectedDigest, detector.Compute(CreateSource(kind, payload)));
        Assert.Equal(expectedFrame, detector.Encode(CreateSource(kind, payload)));
        Assert.Equal(expectedFrame, CreateSource(kind, payload).WithCRC32C());
        Assert.Equal(length == 0, detector.IsError(CreateSource(kind, expectedFrame)));
        Assert.Equal(payload, detector.Decode(CreateSource(kind, expectedFrame)));
    }

    [Fact]
    public void EmptyInputAndEmptyAppendsHaveZeroCrcAndRejectDigestOnlyFrames()
    {
        var accumulator = new CRC32CAccumulator();
        var detector = new CRC32C();
        byte[] empty = Array.Empty<byte>();
        Assert.Equal(0u, Crc32CReference.Compute(empty));
        Assert.Equal(0u, accumulator.GetCurrentValue());

        for (int repetition = 0; repetition < 4; repetition++)
        {
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(default);
            Assert.Equal(0u, accumulator.GetCurrentValue());
            accumulator.Reset();
        }

        Assert.Equal(new byte[4], detector.Compute(empty));
        Assert.Equal(new byte[4], detector.Encode(empty));
        Assert.Equal(new byte[4], empty.WithCRC32C());
        Assert.True(detector.IsError(new byte[4]));
        Assert.Empty(detector.Decode(new byte[4]));
        Assert.Equal("CRC32C", detector.GetDetectionType());
        Assert.IsAssignableFrom<ErrorDetection>(detector);
    }

    [Fact]
    public void EverySingleByteMatchesIndependentOracleAndAllNumericPaths()
    {
        var accumulator = new CRC32CAccumulator();
        var detector = new CRC32C();
        byte[] input = new byte[1];
        for (int value = 0; value <= byte.MaxValue; value++)
        {
            input[0] = (byte)value;
            uint expected = Crc32CReference.Compute(input);
            AssertValue(expected, input.CRC32C(), $"byte {value:X2}");
            Assert.Equal(NativeBytes(expected), detector.Compute(input));
            accumulator.Reset();
            accumulator.Append(input);
            AssertValue(expected, accumulator.GetCurrentValue(), $"byte {value:X2}");
        }
    }

    [Fact]
    public void EveryTwoByteCombinationMatchesOracleWhenComputedWholeOrSplit()
    {
        var accumulator = new CRC32CAccumulator();
        var detector = new CRC32C();
        byte[] input = new byte[2];
        for (int value = 0; value <= ushort.MaxValue; value++)
        {
            input[0] = (byte)(value >> 8);
            input[1] = (byte)value;
            uint expected = Crc32CReference.Compute(input);
            uint actual = input.CRC32C();
            AssertValue(expected, actual, $"two bytes {value:X4}, numeric");
            Assert.Equal(NativeBytes(expected), detector.Compute(input));
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, 1));
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(input.AsSpan(1));
            AssertValue(expected, accumulator.GetCurrentValue(), $"two bytes {value:X4}, split");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(64)]
    [InlineData(127)]
    [InlineData(257)]
    public void EveryTwoChunkSplitIncludingEmptyEdgesMatchesOracle(int length)
    {
        byte[] input = DeterministicBytes(length, (uint)length);
        uint expected = Crc32CReference.Compute(input);
        var accumulator = new CRC32CAccumulator();
        for (int split = 0; split <= length; split++)
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            accumulator.Append(input.AsSpan(split));
            AssertValue(expected, accumulator.GetCurrentValue(), $"length {length}, split {split}");
        }
    }

    [Fact]
    public void EveryPartitionOfSmallBinaryInputsMatchesOracleWithEmptyChunks()
    {
        var accumulator = new CRC32CAccumulator();
        for (int length = 1; length <= 10; length++)
        foreach (byte[] input in SmallBinaryInputs(length))
        {
            uint expected = Crc32CReference.Compute(input);
            int partitionCount = 1 << (length - 1);
            for (int boundaries = 0; boundaries < partitionCount; boundaries++)
            {
                accumulator.Reset();
                AppendPartition(accumulator, input, boundaries);
                AssertValue(expected, accumulator.GetCurrentValue(), $"length {length}, partition {boundaries}");
            }
        }
    }

    [Theory]
    [InlineData(65535)]
    [InlineData(65536)]
    [InlineData(65537)]
    [InlineData(131071)]
    [InlineData(131072)]
    [InlineData(131073)]
    public void FixedChunkSizesMatchOracleAt64KiBBoundaries(int length)
    {
        byte[] input = DeterministicBytes(length, (uint)length ^ 0xC0FFEE01);
        uint expected = Crc32CReference.Compute(input);
        Assert.Equal(expected, input.CRC32C());
        var accumulator = new CRC32CAccumulator();
        foreach (int chunkSize in new[] { 1, 2, 3, 7, 31, 255, 4096, BufferBoundary - 1, BufferBoundary, BufferBoundary + 1 })
        {
            accumulator.Reset();
            AppendInChunks(accumulator, input, chunkSize);
            AssertValue(expected, accumulator.GetCurrentValue(), $"length {length}, chunk {chunkSize}");
        }
    }

    [Fact]
    public void DeterministicRandomPartitionsMatchOracleAcrossVariedMessageLengths()
    {
        var accumulator = new CRC32CAccumulator();
        for (uint sample = 1; sample <= 48; sample++)
        {
            int length = 1 + (int)(sample * 7919 % 32768);
            byte[] input = DeterministicBytes(length, sample * 0x9E3779B9);
            uint expected = Crc32CReference.Compute(input);
            Assert.Equal(expected, input.CRC32C());
            accumulator.Reset();
            AppendRandomPartitions(accumulator, input, sample ^ 0xA5A5A5A5);
            AssertValue(expected, accumulator.GetCurrentValue(), $"sample {sample}, length {length}");
        }
    }

    [Fact]
    public void RepeatedPrefixSnapshotsAllowAppendingAfterFinalizationWithoutChangingState()
    {
        var accumulator = new CRC32CAccumulator();
        byte[] input = DeterministicBytes(513, 0x12345678);
        for (int offset = 0; offset < input.Length; offset++)
        {
            accumulator.Append(input.AsSpan(offset, 1));
            uint expected = Crc32CReference.Compute(input.AsSpan(0, offset + 1));
            for (int repetition = 0; repetition < 4; repetition++)
                AssertValue(expected, accumulator.GetCurrentValue(), $"prefix {offset + 1}");
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            AssertValue(expected, accumulator.GetCurrentValue(), $"empty chunk after prefix {offset + 1}");
        }
    }

    [Fact]
    public void ResetDiscardsPartialOrCompleteMessagesAndAllowsRepeatedReuse()
    {
        var accumulator = new CRC32CAccumulator();
        byte[][] messages = { CheckInput, new byte[] { 0, 0xFF, 0x80 }, DeterministicBytes(1025, 0xBADCAFE) };
        for (int repetition = 0; repetition < 4; repetition++)
        foreach (byte[] input in messages)
        {
            accumulator.Append(input.AsSpan(0, Math.Max(1, input.Length / 2)));
            accumulator.Reset();
            Assert.Equal(0u, accumulator.GetCurrentValue());
            accumulator.Reset();
            AppendInChunks(accumulator, input, 7);
            Assert.Equal(Crc32CReference.Compute(input), accumulator.GetCurrentValue());
            accumulator.Reset();
            Assert.Equal(0u, accumulator.GetCurrentValue());
        }
    }

    [Fact]
    public void SpanSlicesConsumeOnlyTheirBytesWithoutRetainingOrModifyingBackingStorage()
    {
        var accumulator = new CRC32CAccumulator();
        byte[] input = DeterministicBytes(257, 0x87654321);
        int offset = 0;
        foreach (int count in new[] { 1, 63, 64, 65, 64 })
        {
            const int leadingGuard = 17;
            byte[] guarded = Enumerable.Repeat((byte)0xD3, leadingGuard + count + 23).ToArray();
            input.AsSpan(offset, count).CopyTo(guarded.AsSpan(leadingGuard));
            byte[] beforeAppend = guarded.ToArray();
            accumulator.Append(guarded.AsSpan(leadingGuard, count));
            Assert.Equal(beforeAppend, guarded);

            Array.Fill(guarded, (byte)0x4C);
            offset += count;
            Assert.Equal(Crc32CReference.Compute(input.AsSpan(0, offset)), accumulator.GetCurrentValue());
        }
        Assert.Equal(input.Length, offset);
    }

    [Fact]
    public void StackAllocatedSpanCanBeConsumedAndReusedImmediately()
    {
        var accumulator = new CRC32CAccumulator();
        Span<byte> buffer = stackalloc byte[15];
        buffer.Fill(0xEE);
        CheckInput.CopyTo(buffer.Slice(3));
        accumulator.Append(buffer.Slice(3, CheckInput.Length));
        buffer.Clear();
        Assert.Equal(0xE3069283u, accumulator.GetCurrentValue());

        accumulator.Reset();
        accumulator.Append(buffer.Slice(5, 1));
        Assert.Equal(Crc32CReference.Compute(new byte[] { 0 }), accumulator.GetCurrentValue());
    }

    [Fact]
    public void InterleavedInstancesMaintainIndependentStateThroughResetAndReuse()
    {
        var left = new CRC32CAccumulator();
        var right = new CRC32CAccumulator();
        byte[] leftInput = DeterministicBytes(129, 1);
        byte[] rightInput = DeterministicBytes(129, 2);
        for (int offset = 0; offset < leftInput.Length; offset++)
        {
            left.Append(leftInput.AsSpan(offset, 1));
            right.Append(rightInput.AsSpan(offset, 1));
            Assert.Equal(Crc32CReference.Compute(leftInput.AsSpan(0, offset + 1)), left.GetCurrentValue());
            Assert.Equal(Crc32CReference.Compute(rightInput.AsSpan(0, offset + 1)), right.GetCurrentValue());
        }
        left.Reset();
        Assert.Equal(0u, left.GetCurrentValue());
        Assert.Equal(Crc32CReference.Compute(rightInput), right.GetCurrentValue());
        left.Append(CheckInput);
        Assert.Equal(0xE3069283u, left.GetCurrentValue());
        Assert.Equal(Crc32CReference.Compute(rightInput), right.GetCurrentValue());
    }

    [Fact]
    public void SeparateInstancesAndStatelessDetectorCanBeUsedByConcurrentWorkers()
    {
        var detector = new CRC32C();
        Parallel.For(0, 64, worker =>
        {
            byte[] input = DeterministicBytes(4097 + worker, (uint)worker + 1);
            uint expected = Crc32CReference.Compute(input);
            var accumulator = new CRC32CAccumulator();
            AppendInChunks(accumulator, input, 1 + worker % 127);
            Assert.Equal(expected, accumulator.GetCurrentValue());
            Assert.Equal(NativeBytes(expected), detector.Compute(input));
            accumulator.Reset();
            accumulator.Append(CheckInput);
            Assert.Equal(0xE3069283u, accumulator.GetCurrentValue());
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(17)]
    [InlineData(257)]
    public void IndependentFramesAcceptOriginalPayloadAndRejectEverySingleBitMutation(int length)
    {
        byte[] input = DeterministicBytes(length, 0xDEADBEEF);
        byte[] frame = ReferenceFrame(input);
        var detector = new CRC32C();
        Assert.False(detector.IsError(frame));
        Assert.Equal(input, detector.Decode(frame));

        for (int bit = 0; bit < frame.Length * 8; bit++)
        {
            byte[] damaged = (byte[])frame.Clone();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            AssertRejected(detector, damaged, $"frame bit {bit}, payload length {length}");
        }
    }

    [Fact]
    public void PayloadBurstPatternsThrough32BitsAreDetectedAcrossByteBoundaries()
    {
        byte[] frame = ReferenceFrame(DeterministicBytes(17, 0xFA17CAFE));
        var detector = new CRC32C();
        for (int length = 1; length <= 32; length++)
        for (int start = 0; start <= 17 * 8 - length; start++)
        for (int pattern = 0; pattern < 4; pattern++)
        {
            byte[] damaged = (byte[])frame.Clone();
            FlipBurst(damaged, start, length, pattern);
            AssertRejected(detector, damaged, $"burst length {length}, start {start}, pattern {pattern}");
        }
    }

    [Fact]
    public void EveryPairOfChangedPayloadBitsInAShortBinaryMessageIsDetected()
    {
        // This deliberately bounded message avoids asserting that a finite
        // CRC detects every two-bit error at arbitrarily large separations.
        byte[] payload = { 0x00, 0xFF, 0x80, 0x7F, 0xA5, 0x5A, 0x01, 0xFE };
        byte[] frame = ReferenceFrame(payload);
        var detector = new CRC32C();
        for (int first = 0; first < payload.Length * 8; first++)
        for (int second = first + 1; second < payload.Length * 8; second++)
        {
            byte[] damaged = (byte[])frame.Clone();
            damaged[first / 8] ^= (byte)(1 << (first % 8));
            damaged[second / 8] ^= (byte)(1 << (second % 8));
            AssertRejected(detector, damaged, $"payload bits {first} and {second}");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void FramesWithoutAnyPayloadAreAlwaysRejected(int length)
    {
        var detector = new CRC32C();
        AssertRejected(detector, new byte[length], $"zero frame length {length}");
        AssertRejected(detector, Enumerable.Repeat((byte)0xFF, length).ToArray(), $"FF frame length {length}");
    }

    [Fact]
    public void TruncatedExtendedReorderedAndWrongAlgorithmFramesAreRejected()
    {
        byte[] frame = ReferenceFrame(CheckInput);
        var detector = new CRC32C();
        for (int length = 0; length < frame.Length; length++)
            AssertRejected(detector, frame.AsSpan(0, length).ToArray(), $"truncation {length}");

        AssertRejected(detector, frame.Concat(new byte[] { 0 }).ToArray(), "appended byte");
        AssertRejected(detector, new byte[] { 0 }.Concat(frame).ToArray(), "prepended byte");
        byte[] reordered = (byte[])frame.Clone();
        (reordered[0], reordered[1]) = (reordered[1], reordered[0]);
        AssertRejected(detector, reordered, "reordered payload");
        byte[] reversedDigest = CheckInput.Concat(NativeBytes(0xE3069283).Reverse()).ToArray();
        AssertRejected(detector, reversedDigest, "reversed digest");
        AssertRejected(detector, CheckInput.Concat(NativeBytes(0xCBF43926)).ToArray(), "IEEE CRC32 suffix");
    }

    [Theory]
    [InlineData("Compute")]
    [InlineData("Encode")]
    [InlineData("Decode")]
    [InlineData("IsError")]
    [InlineData("CRC32C")]
    [InlineData("WithCRC32C")]
    public void NullEnumerablesThrowArgumentNullException(string operation)
    {
        Assert.Throws<ArgumentNullException>(() => InvokeOperation(operation, null!));
    }

    [Theory]
    [InlineData("Compute")]
    [InlineData("Encode")]
    [InlineData("Decode")]
    [InlineData("IsError")]
    [InlineData("CRC32C")]
    [InlineData("WithCRC32C")]
    public void EachOperationConsumesASinglePassEnumerableExactlyOnceAndDisposesIt(string operation)
    {
        byte[] payload = operation is "Decode" or "IsError" ? ReferenceFrame(CheckInput) : CheckInput;
        var source = new SinglePassSource(payload);
        InvokeOperation(operation, source);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(payload.Length, source.CurrentCount);
        Assert.Equal(payload.Length + 1, source.MoveNextCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData("Compute", false)]
    [InlineData("Compute", true)]
    [InlineData("Encode", false)]
    [InlineData("Encode", true)]
    [InlineData("Decode", false)]
    [InlineData("Decode", true)]
    [InlineData("IsError", false)]
    [InlineData("IsError", true)]
    [InlineData("CRC32C", false)]
    [InlineData("CRC32C", true)]
    [InlineData("WithCRC32C", false)]
    [InlineData("WithCRC32C", true)]
    public void EnumerationFailuresPropagateWithoutWrappingAndDisposeTheEnumerator(string operation, bool failOnCurrent)
    {
        var source = new FailingSource(failOnCurrent);
        Exception error = Assert.Throws<InvalidOperationException>(() => InvokeOperation(operation, source));
        Assert.Same(source.Failure, error);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void ReusingDetectorAndEnumeratingResultsDoesNotRereadOrMutateInputs()
    {
        var detector = new CRC32C();
        byte[] input = CheckInput.ToArray();
        var computeSource = new SinglePassSource(input);
        IEnumerable<byte> digest = detector.Compute(computeSource);
        byte[] expectedDigest = NativeBytes(0xE3069283);
        Assert.Equal(expectedDigest, digest);
        Array.Fill(input, (byte)0);
        Assert.Equal(expectedDigest, digest);
        Assert.Equal(1, computeSource.EnumeratorCount);

        byte[] payload = CheckInput.ToArray();
        byte[] frame = detector.Encode(payload).ToArray();
        byte[] decoded = detector.Decode(frame).ToArray();
        Assert.Equal(CheckInput, payload);
        Assert.Equal(CheckInput, decoded);
        Assert.Equal(ReferenceFrame(CheckInput), frame);
        Assert.Equal(NativeBytes(0xE3069283), detector.Compute(CheckInput));
    }

    [Fact]
    public void AddingCrc32CLeavesIeeeResultsAliasesAndUndefinedEnumBehaviorUnchanged()
    {
#pragma warning disable CS0618 // The old alias is a supported compatibility contract.
        Assert.Equal(0xCBF43926u, CheckInput.CRC32(CRC32Type.Basic));
        Assert.Equal(0xCBF43926u, CheckInput.CRC32(CRC32Type.Classic));
        Assert.Equal(new[] { CRC32Type.Basic, CRC32Type.Classic }, Enum.GetValues<CRC32Type>());
#pragma warning restore CS0618
        Assert.Equal(0xE3069283u, CheckInput.CRC32C());
        Assert.Empty(new CRC32((CRC32Type)2).Compute(CheckInput));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CRC32Accumulator((CRC32Type)2));
        Assert.Throws<ArgumentOutOfRangeException>(() => CheckInput.CRC32((CRC32Type)2));
    }

    private static void InvokeOperation(string operation, IEnumerable<byte> source)
    {
        var detector = new CRC32C();
        switch (operation)
        {
            case "Compute": _ = detector.Compute(source).ToArray(); break;
            case "Encode": _ = detector.Encode(source).ToArray(); break;
            case "Decode": _ = detector.Decode(source).ToArray(); break;
            case "IsError": _ = detector.IsError(source); break;
            case "CRC32C": _ = source.CRC32C(); break;
            case "WithCRC32C": _ = source.WithCRC32C().ToArray(); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static IEnumerable<byte> CreateSource(SourceKind kind, byte[] payload) => kind switch
    {
        SourceKind.Array => payload,
        SourceKind.List => payload.ToList(),
        SourceKind.LinkedList => new LinkedList<byte>(payload),
        SourceKind.Iterator => YieldBytes(payload),
        SourceKind.SinglePass => new SinglePassSource(payload),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static IEnumerable<byte> YieldBytes(byte[] payload)
    {
        foreach (byte value in payload)
            yield return value;
    }

    private static IEnumerable<byte[]> SmallBinaryInputs(int length)
    {
        yield return new byte[length];
        yield return Enumerable.Repeat((byte)0xFF, length).ToArray();
        yield return Enumerable.Range(0, length).Select(i => (byte)(i % 2 == 0 ? 0x80 : 0x01)).ToArray();
    }

    private static void AppendPartition(CRC32CAccumulator accumulator, byte[] input, int boundaries)
    {
        accumulator.Append(ReadOnlySpan<byte>.Empty);
        int start = 0;
        for (int end = 1; end <= input.Length; end++)
        {
            if (end < input.Length && (boundaries & (1 << (end - 1))) == 0)
                continue;
            accumulator.Append(input.AsSpan(start, end - start));
            accumulator.Append(input.AsSpan(end, 0));
            start = end;
        }
    }

    private static void AppendInChunks(CRC32CAccumulator accumulator, byte[] input, int chunkSize)
    {
        for (int offset = 0; offset < input.Length; offset += chunkSize)
            accumulator.Append(input.AsSpan(offset, Math.Min(chunkSize, input.Length - offset)));
    }

    private static void AppendRandomPartitions(CRC32CAccumulator accumulator, byte[] input, uint state)
    {
        int offset = 0;
        while (offset < input.Length)
        {
            int count = Math.Min(1 + (int)(NextRandom(ref state) % 1024), input.Length - offset);
            accumulator.Append(input.AsSpan(offset, count));
            offset += count;
            if ((state & 3) == 0)
                accumulator.Append(input.AsSpan(offset, 0));
        }
    }

    private static byte[] DeterministicBytes(int length, uint seed)
    {
        var result = new byte[length];
        uint state = seed;
        for (int index = 0; index < result.Length; index++)
            result[index] = (byte)(NextRandom(ref state) >> 24);
        return result;
    }

    private static uint NextRandom(ref uint state)
    {
        state = unchecked(state * 1664525 + 1013904223);
        return state;
    }

    private static byte[] ReferenceFrame(byte[] input)
        => input.Concat(NativeBytes(Crc32CReference.Compute(input))).ToArray();

    private static byte[] NativeBytes(uint value)
    {
        byte[] result = LittleEndianBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(result);
        return result;
    }

    private static byte[] LittleEndianBytes(uint value)
        => new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };

    private static void AssertValue(uint expected, uint actual, string context)
    {
        if (actual != expected)
            Assert.Fail($"CRC32C {context}: expected {expected:X8}, actual {actual:X8}.");
    }

    private static void AssertRejected(CRC32C detector, byte[] damaged, string context)
    {
        Assert.True(detector.IsError(damaged), context);
        Assert.Empty(detector.Decode(damaged));
    }

    private static void FlipBurst(byte[] frame, int start, int length, int pattern)
    {
        for (int offset = 0; offset < length; offset++)
        {
            bool endpoint = offset == 0 || offset == length - 1;
            bool interior = pattern switch
            {
                0 => false,
                1 => true,
                2 => offset % 2 == 0,
                3 => (offset * 13 + start * 7 + length) % 5 < 2,
                _ => throw new ArgumentOutOfRangeException(nameof(pattern))
            };
            if (!endpoint && !interior)
                continue;
            int bit = start + offset;
            frame[bit / 8] ^= (byte)(1 << (bit % 8));
        }
    }

    private sealed class SinglePassSource : IEnumerable<byte>
    {
        private readonly byte[] payload;
        public int EnumeratorCount { get; private set; }
        public int CurrentCount { get; private set; }
        public int MoveNextCount { get; private set; }
        public int DisposeCount { get; private set; }

        public SinglePassSource(byte[] payload) => this.payload = payload;

        public IEnumerator<byte> GetEnumerator()
        {
            EnumeratorCount++;
            if (EnumeratorCount != 1)
                throw new InvalidOperationException("The source cannot be enumerated twice.");
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : IEnumerator<byte>
        {
            private readonly SinglePassSource owner;
            private int position = -1;

            public Enumerator(SinglePassSource owner) => this.owner = owner;
            public byte Current
            {
                get
                {
                    owner.CurrentCount++;
                    return owner.payload[position];
                }
            }
            object IEnumerator.Current => Current;
            public bool MoveNext()
            {
                owner.MoveNextCount++;
                return ++position < owner.payload.Length;
            }
            public void Reset() => throw new NotSupportedException();
            public void Dispose() => owner.DisposeCount++;
        }
    }

    private sealed class FailingSource : IEnumerable<byte>
    {
        private readonly bool failOnCurrent;
        public InvalidOperationException Failure { get; } = new("Injected enumeration failure.");
        public int DisposeCount { get; private set; }

        public FailingSource(bool failOnCurrent) => this.failOnCurrent = failOnCurrent;
        public IEnumerator<byte> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : IEnumerator<byte>
        {
            private readonly FailingSource owner;
            private int position = -1;
            public Enumerator(FailingSource owner) => this.owner = owner;
            public byte Current => owner.failOnCurrent && position == 2 ? throw owner.Failure : (byte)position;
            object IEnumerator.Current => Current;
            public bool MoveNext()
            {
                position++;
                if (!owner.failOnCurrent && position == 2)
                    throw owner.Failure;
                return position < 9;
            }
            public void Reset() => throw new NotSupportedException();
            public void Dispose() => owner.DisposeCount++;
        }
    }
}
