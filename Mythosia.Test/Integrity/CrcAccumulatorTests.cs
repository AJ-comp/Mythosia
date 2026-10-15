using System.Text;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

// Exercise every supported enum value, including the obsolete compatibility names.
#pragma warning disable CS0618

[Collection(IntegrityTestCollection.Name)]
public sealed class CrcAccumulatorTests
{
    private const int BufferBoundary = 64 * 1024;
    private static readonly byte[] PublishedCheckInput = Encoding.ASCII.GetBytes("123456789");

    public static IEnumerable<object[]> Algorithms()
    {
        foreach (CRC8Type type in Enum.GetValues<CRC8Type>())
            yield return new object[] { 8, (int)type };
        foreach (CRC16Type type in Enum.GetValues<CRC16Type>())
            yield return new object[] { 16, (int)type };
        foreach (CRC32Type type in Enum.GetValues<CRC32Type>())
            yield return new object[] { 32, (int)type };
    }

    [Fact]
    public void DefaultConstructorsSelectClassicAndReturnValuesOfTheSpecifiedWidth()
    {
        var crc8 = new CRC8Accumulator();
        var crc16 = new CRC16Accumulator();
        var crc32 = new CRC32Accumulator();
        Assert.Equal(CRC8Type.Classic, crc8.Type);
        Assert.Equal(CRC16Type.Classic, crc16.Type);
        Assert.Equal(CRC32Type.Classic, crc32.Type);

        ReadOnlySpan<byte> source = PublishedCheckInput;
        crc8.Append(source);
        crc16.Append(source);
        crc32.Append(source);
        byte value8 = crc8.GetCurrentValue();
        ushort value16 = crc16.GetCurrentValue();
        uint value32 = crc32.GetCurrentValue();
        Assert.Equal((byte)0xF4, value8);
        Assert.Equal((ushort)0xBB3D, value16);
        Assert.Equal(0xCBF43926u, value32);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void TypePreservesTheRequestedEnumThroughAppendAndReset(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        Assert.Equal(variant, accumulator.ReadType());
        accumulator.Append(PublishedCheckInput);
        Assert.Equal(variant, accumulator.ReadType());
        accumulator.Reset();
        Assert.Equal(variant, accumulator.ReadType());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Crc8ConstructorRejectsUndefinedTypes(int variant)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new CRC8Accumulator((CRC8Type)variant));
        Assert.Equal("type", error.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Crc16ConstructorRejectsUndefinedTypes(int variant)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new CRC16Accumulator((CRC16Type)variant));
        Assert.Equal("type", error.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Crc32ConstructorRejectsUndefinedTypes(int variant)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new CRC32Accumulator((CRC32Type)variant));
        Assert.Equal("type", error.ParamName);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void EmptyAppendsPreserveEmptySemanticsBeforeAndAfterReset(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        AssertEmpty(accumulator, width, variant);
        for (int repetition = 0; repetition < 3; repetition++)
        {
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(default);
            accumulator.Append(PublishedCheckInput.AsSpan(4, 0));
            AssertEmpty(accumulator, width, variant);
        }

        // A nonempty all-zero message can have a numerical value of zero. It
        // must still be distinguished from the uninitialized CRC8 state.
        byte[] zeros = { 0, 0 };
        accumulator.Append(zeros);
        AssertCurrent(accumulator, Expected(zeros, width, variant), width, variant, "nonempty zeros");
        accumulator.Append(ReadOnlySpan<byte>.Empty);
        AssertCurrent(accumulator, Expected(zeros, width, variant), width, variant, "empty append after data");

        accumulator.Reset();
        AssertEmpty(accumulator, width, variant);
        accumulator.Append(ReadOnlySpan<byte>.Empty);
        accumulator.Reset();
        AssertEmpty(accumulator, width, variant);
        accumulator.Append(PublishedCheckInput);
        AssertCurrent(accumulator, Expected(PublishedCheckInput, width, variant), width, variant, "reuse after empty reset");
    }

    // Published check values calibrate the existing independent, bitwise
    // oracle before it is used to judge arbitrary streamed partitions.
    // Catalogue provenance is recorded in CrcReference.cs.
    [Theory]
    [InlineData(8, (int)CRC8Type.Basic, 0xF4u)]
    [InlineData(8, (int)CRC8Type.Classic, 0xF4u)]
    [InlineData(8, (int)CRC8Type.Maxim, 0xA1u)]
    [InlineData(16, (int)CRC16Type.Basic, 0xBB3Du)]
    [InlineData(16, (int)CRC16Type.Classic, 0xBB3Du)]
    [InlineData(16, (int)CRC16Type.Modbus, 0x4B37u)]
    [InlineData(16, (int)CRC16Type.CCITTxModem, 0x31C3u)]
    [InlineData(16, (int)CRC16Type.DNP, 0x82EAu)]
    [InlineData(16, (int)CRC16Type.CCITTFalse, 0x29B1u)]
    [InlineData(16, (int)CRC16Type.X25, 0x906Eu)]
    [InlineData(32, (int)CRC32Type.Basic, 0xCBF43926u)]
    [InlineData(32, (int)CRC32Type.Classic, 0xCBF43926u)]
    public void PublishedChecksMatchIndependentReferenceAndLegacyOneShot(int width, int variant, uint expected)
    {
        Assert.Equal(expected, Expected(PublishedCheckInput, width, variant));
        Assert.Equal(expected, LegacyValue(PublishedCheckInput, width, variant));
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        AppendInChunks(accumulator, PublishedCheckInput, 2);
        AssertCurrent(accumulator, expected, width, variant, "published check");
    }

    [Theory]
    // Analog Devices' independently published PEC and Maxim ROM examples.
    // https://www.analog.com/en/resources/analog-dialogue/articles/cyclic-redundancy-and-correct-data-communications.html
    // https://www.analog.com/en/resources/technical-articles/understanding-and-using-cyclic-redundancy-checks-with-maxim-1wire-and-ibutton-products.html
    [InlineData(8, (int)CRC8Type.Classic, "654321", 0x86u)]
    [InlineData(8, (int)CRC8Type.Maxim, "021CB801000000", 0xA2u)]
    // opendnp3's header example has canonical CRC 0x21E9; preserve the
    // Mythosia legacy numeric byte swap, 0xE921, for nonempty DNP messages.
    // https://github.com/dnp3/opendnp3/blob/release/cpp/tests/unit/TestCRC.cpp
    [InlineData(16, (int)CRC16Type.DNP, "056405C001000004", 0xE921u)]
    // Frozen Python zlib.crc32 / binascii.crc_hqx(data, 0) fixtures from
    // ExternalCrcVectorTests.cs, independent of both production and oracle.
    [InlineData(16, (int)CRC16Type.CCITTxModem, "DEADBEEF", 0xC457u)]
    [InlineData(32, (int)CRC32Type.Classic, "DEADBEEF", 0x7C9CA35Au)]
    [InlineData(16, (int)CRC16Type.CCITTxModem, "0123456789ABCDEF80FF00", 0xEAE2u)]
    [InlineData(32, (int)CRC32Type.Classic, "0123456789ABCDEF80FF00", 0xC395ECEFu)]
    public void ExternalBinaryVectorsMatchWhenAppendedByteByByte(int width, int variant, string hex, uint expected)
    {
        byte[] source = Convert.FromHexString(hex);
        Assert.Equal(expected, Expected(source, width, variant));
        Assert.Equal(expected, LegacyValue(source, width, variant));
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        AppendInChunks(accumulator, source, 1);
        AssertCurrent(accumulator, expected, width, variant, hex);
    }

    [Theory]
    [InlineData(65535, 0x96936E4Bu, 0x07E0u)]
    [InlineData(65536, 0xD1229EEFu, 0xD882u)]
    [InlineData(65537, 0x66D87324u, 0xEA27u)]
    public void FrozenExternalVectorsMatchAcross64KiBBoundary(int length, uint expected32, uint expectedXmodem)
    {
        // These values and this generator are frozen in ExternalCrcVectorTests.
        // Python: bytes((i * 73 + (i >> 3) + 19) & 255 for i in range(length))
        byte[] source = Enumerable.Range(0, length)
            .Select(i => (byte)((i * 73 + (i >> 3) + 19) & 255)).ToArray();
        foreach ((int width, int variant, uint expected) in new[]
        {
            (32, (int)CRC32Type.Classic, expected32),
            (16, (int)CRC16Type.CCITTxModem, expectedXmodem)
        })
        {
            Assert.Equal(expected, Expected(source, width, variant));
            Assert.Equal(expected, LegacyValue(source, width, variant));
            AccumulatorProbe accumulator = CreateAccumulator(width, variant);
            accumulator.Append(source.AsSpan(0, BufferBoundary - 1));
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(source.AsSpan(BufferBoundary - 1));
            AssertCurrent(accumulator, expected, width, variant, $"external length {length}");
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void EverySingleByteMatchesIndependentReference(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        var source = new byte[1];
        for (int value = 0; value <= byte.MaxValue; value++)
        {
            source[0] = (byte)value;
            accumulator.Reset();
            accumulator.Append(source);
            AssertCurrent(accumulator, Expected(source, width, variant), width, variant, $"byte {value:X2}");
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void EveryTwoByteCombinationSplitIntoSingleBytesMatchesIndependentReference(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        var source = new byte[2];
        for (int value = 0; value <= ushort.MaxValue; value++)
        {
            source[0] = (byte)(value >> 8);
            source[1] = (byte)value;
            accumulator.Reset();
            accumulator.Append(source.AsSpan(0, 1));
            accumulator.Append(source.AsSpan(1, 1));
            uint expected = Expected(source, width, variant);
            uint actual = accumulator.Read();
            if (actual != expected)
                Assert.Fail($"CRC{width}/{variant}, two-byte input {value:X4}: expected {expected:X}, actual {actual:X}.");
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void EveryTwoChunkSplitIncludingEmptyEdgesMatchesBothReferences(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        foreach (int length in new[] { 1, 2, 3, 7, 9, 17, 31, 64, 127, 257 })
        {
            byte[] source = DeterministicBytes(length, (uint)length);
            uint expected = Expected(source, width, variant);
            Assert.Equal(expected, LegacyValue(source, width, variant));
            for (int split = 0; split <= source.Length; split++)
            {
                accumulator.Reset();
                accumulator.Append(source.AsSpan(0, split));
                accumulator.Append(source.AsSpan(split));
                AssertCurrent(accumulator, expected, width, variant, $"length {length}, split {split}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void EveryPartitionOfSmallBinaryInputsAllowsInterspersedEmptyChunks(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        for (int length = 1; length <= 9; length++)
        {
            foreach (byte[] source in SmallBinaryInputs(length))
            {
                uint expected = Expected(source, width, variant);
                Assert.Equal(expected, LegacyValue(source, width, variant));
                int partitionCount = 1 << (length - 1);
                for (int boundaries = 0; boundaries < partitionCount; boundaries++)
                {
                    accumulator.Reset();
                    accumulator.Append(ReadOnlySpan<byte>.Empty);
                    int start = 0;
                    for (int end = 1; end <= length; end++)
                    {
                        if (end < length && (boundaries & (1 << (end - 1))) == 0)
                            continue;

                        accumulator.Append(source.AsSpan(start, end - start));
                        accumulator.Append(source.AsSpan(end, 0));
                        start = end;
                    }
                    accumulator.Append(ReadOnlySpan<byte>.Empty);
                    AssertCurrent(accumulator, expected, width, variant, $"length {length}, partition {boundaries}");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void FixedChunkSizesMatchAt64KiBLengthAndChunkBoundaries(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        int[] chunkSizes = { 1, 2, 3, 7, 31, 255, 4096, BufferBoundary - 1, BufferBoundary, BufferBoundary + 1 };
        int[] lengths = { BufferBoundary - 1, BufferBoundary, BufferBoundary + 1, 2 * BufferBoundary + 1 };
        foreach (int length in lengths)
        {
            byte[] source = DeterministicBytes(length, (uint)length ^ 0xC0FFEE01);
            uint expected = Expected(source, width, variant);
            Assert.Equal(expected, LegacyValue(source, width, variant));
            foreach (int chunkSize in chunkSizes)
            {
                accumulator.Reset();
                AppendInChunks(accumulator, source, chunkSize);
                AssertCurrent(accumulator, expected, width, variant, $"length {length}, chunk size {chunkSize}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void DeterministicRandomPartitionsMatchBothReferences(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        for (uint sample = 1; sample <= 48; sample++)
        {
            int length = 1 + (int)(sample * 7919 % 32768);
            byte[] source = DeterministicBytes(length, sample * 0x9E3779B9);
            uint expected = Expected(source, width, variant);
            Assert.Equal(expected, LegacyValue(source, width, variant));
            accumulator.Reset();
            uint partitionState = sample ^ 0xA5A5A5A5;
            int offset = 0;
            while (offset < source.Length)
            {
                int count = Math.Min(1 + (int)(NextRandom(ref partitionState) % 1024), source.Length - offset);
                accumulator.Append(source.AsSpan(offset, count));
                offset += count;
                if ((partitionState & 3) == 0)
                    accumulator.Append(source.AsSpan(offset, 0));
            }
            AssertCurrent(accumulator, expected, width, variant, $"random sample {sample}, length {length}");
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void PrefixSnapshotsAreRepeatableAndAppendingContinuesFromUnfinalizedState(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        byte[] source = DeterministicBytes(513, 0x12345678);
        int offset = 0;
        foreach (int chunkSize in new[] { 1, 2, 7, 13, 64, 127, 299 })
        {
            accumulator.Append(source.AsSpan(offset, chunkSize));
            offset += chunkSize;
            byte[] prefix = source.AsSpan(0, offset).ToArray();
            uint expected = Expected(prefix, width, variant);
            Assert.Equal(expected, LegacyValue(prefix, width, variant));
            for (int read = 0; read < 8; read++)
                AssertCurrent(accumulator, expected, width, variant, $"snapshot at prefix {offset}");

            accumulator.Append(ReadOnlySpan<byte>.Empty);
            AssertCurrent(accumulator, expected, width, variant, $"empty append at prefix {offset}");
        }
        Assert.Equal(source.Length, offset);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void ResetDiscardsFullOrPartialMessagesAndAllowsRepeatedReuse(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        byte[][] messages =
        {
            PublishedCheckInput,
            new byte[] { 0x00, 0xFF, 0x80 },
            DeterministicBytes(1025, 0xBADCAFE)
        };
        for (int repetition = 0; repetition < 3; repetition++)
        {
            foreach (byte[] source in messages)
            {
                accumulator.Append(source.AsSpan(0, Math.Max(1, source.Length / 2)));
                accumulator.Reset();
                AssertEmpty(accumulator, width, variant);
                accumulator.Reset();
                AppendInChunks(accumulator, source, 7);
                AssertCurrent(accumulator, Expected(source, width, variant), width, variant, "message after partial reset");
                accumulator.Reset();
                AssertEmpty(accumulator, width, variant);
                Assert.Equal(variant, accumulator.ReadType());
            }
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void SpanSlicesConsumeOnlyTheirBytesAndDoNotRetainOrModifyTheBuffer(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        byte[] source = DeterministicBytes(257, 0x87654321);
        int offset = 0;
        foreach (int count in new[] { 1, 63, 64, 65, 64 })
        {
            const int leadingGuard = 17;
            const int trailingGuard = 23;
            byte[] guarded = Enumerable.Repeat((byte)0xD3, leadingGuard + count + trailingGuard).ToArray();
            source.AsSpan(offset, count).CopyTo(guarded.AsSpan(leadingGuard, count));
            byte[] beforeAppend = guarded.ToArray();
            ReadOnlySpan<byte> slice = guarded.AsSpan(leadingGuard, count);
            accumulator.Append(slice);
            Assert.Equal(beforeAppend, guarded);

            // Changing both the supplied slice and its guards after return
            // must not change the consumed prefix or contaminate later appends.
            Array.Fill(guarded, (byte)0x4C);
            offset += count;
            uint expected = Expected(source.AsSpan(0, offset).ToArray(), width, variant);
            AssertCurrent(accumulator, expected, width, variant, $"mutated backing buffer at prefix {offset}");
        }
        Assert.Equal(source.Length, offset);
        AssertCurrent(accumulator, LegacyValue(source, width, variant), width, variant, "all guarded slices");
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void StackAllocatedReadOnlySpanCanBeConsumedAndReusedImmediately(int width, int variant)
    {
        AccumulatorProbe accumulator = CreateAccumulator(width, variant);
        Span<byte> buffer = stackalloc byte[15];
        buffer.Fill(0xEE);
        PublishedCheckInput.AsSpan().CopyTo(buffer.Slice(3, PublishedCheckInput.Length));
        ReadOnlySpan<byte> slice = buffer.Slice(3, PublishedCheckInput.Length);
        accumulator.Append(slice);
        buffer.Clear();
        AssertCurrent(accumulator, Expected(PublishedCheckInput, width, variant), width, variant, "stackallocated slice");
        accumulator.Reset();
        accumulator.Append(buffer.Slice(5, 1));
        AssertCurrent(accumulator, Expected(new byte[] { 0 }, width, variant), width, variant, "reused stackallocated buffer");
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void InterleavedInstancesKeepSeparateMessagesAndResets(int width, int variant)
    {
        AccumulatorProbe left = CreateAccumulator(width, variant);
        AccumulatorProbe right = CreateAccumulator(width, variant);
        byte[] leftSource = DeterministicBytes(129, 1);
        byte[] rightSource = DeterministicBytes(129, 2);
        for (int offset = 0; offset < leftSource.Length; offset++)
        {
            left.Append(leftSource.AsSpan(offset, 1));
            right.Append(rightSource.AsSpan(offset, 1));
            if (offset % 16 != 0 && offset != leftSource.Length - 1)
                continue;

            AssertCurrent(left, Expected(leftSource.AsSpan(0, offset + 1).ToArray(), width, variant), width, variant, "left interleaved prefix");
            AssertCurrent(right, Expected(rightSource.AsSpan(0, offset + 1).ToArray(), width, variant), width, variant, "right interleaved prefix");
        }

        left.Reset();
        AssertEmpty(left, width, variant);
        AssertCurrent(right, Expected(rightSource, width, variant), width, variant, "right survives left reset");
        left.Append(PublishedCheckInput);
        AssertCurrent(left, Expected(PublishedCheckInput, width, variant), width, variant, "left reused independently");
        AssertCurrent(right, Expected(rightSource, width, variant), width, variant, "right survives left reuse");
    }

    [Fact]
    public void SeparateInstancesOfAllAlgorithmsCanAccumulateConcurrently()
    {
        object[][] algorithms = Algorithms().ToArray();
        Parallel.For(0, algorithms.Length * 8, worker =>
        {
            object[] algorithm = algorithms[worker % algorithms.Length];
            int width = (int)algorithm[0];
            int variant = (int)algorithm[1];
            byte[] source = DeterministicBytes(4097 + worker, (uint)worker + 1);
            uint expected = Expected(source, width, variant);
            AccumulatorProbe accumulator = CreateAccumulator(width, variant);
            AppendInChunks(accumulator, source, 1 + worker % 127);
            AssertCurrent(accumulator, expected, width, variant, $"concurrent worker {worker}");
            accumulator.Reset();
            AssertEmpty(accumulator, width, variant);
            accumulator.Append(PublishedCheckInput);
            AssertCurrent(accumulator, Expected(PublishedCheckInput, width, variant), width, variant, $"concurrent worker {worker} reused");
        });
    }

    private static void AssertEmpty(AccumulatorProbe accumulator, int width, int variant)
    {
        if (width == 8)
        {
            // Reading an empty CRC8 must leave it empty, including after errors.
            for (int read = 0; read < 3; read++)
                Assert.Throws<InvalidOperationException>(() => { accumulator.Read(); });
            return;
        }

        uint expected = width == 16
            ? (CRC16Type)variant is CRC16Type.Modbus or CRC16Type.CCITTFalse or CRC16Type.MCRF4XX ? 0xFFFFu : 0
            : (CRC32Type)variant == CRC32Type.Mpeg2 ? uint.MaxValue : 0;
        Assert.Equal(expected, LegacyValue(Array.Empty<byte>(), width, variant));
        for (int read = 0; read < 3; read++)
            AssertCurrent(accumulator, expected, width, variant, "empty state");
    }

    private static void AssertCurrent(AccumulatorProbe accumulator, uint expected, int width, int variant, string context)
    {
        uint actual = accumulator.Read();
        if (actual != expected)
            Assert.Fail($"CRC{width}/{variant}, {context}: expected 0x{expected:X}, actual 0x{actual:X}.");
    }

    private static uint Expected(byte[] source, int width, int variant) => width switch
    {
        8 => CrcReference.Compute8(source, (CRC8Type)variant),
        16 => LegacyCompatibleReference16(source, (CRC16Type)variant),
        32 => CrcReference.Compute32(source, (CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static ushort LegacyCompatibleReference16(byte[] source, CRC16Type type)
    {
        // DNP's legacy empty value is zero, unlike the canonical CRC's FFFF.
        if (source.Length == 0 && type == CRC16Type.DNP)
            return 0;

        ushort canonical = CrcReference.Compute16(source, type);
        return type == CRC16Type.DNP ? CrcReference.ToLegacyDnpRepresentation(canonical) : canonical;
    }

    private static uint LegacyValue(byte[] source, int width, int variant) => width switch
    {
        8 => source.CRC8((CRC8Type)variant),
        16 => source.CRC16((CRC16Type)variant),
        32 => source.CRC32((CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static AccumulatorProbe CreateAccumulator(int width, int variant)
    {
        switch (width)
        {
            case 8:
                var crc8 = new CRC8Accumulator((CRC8Type)variant);
                return new(() => crc8.GetCurrentValue(), crc8.Append, crc8.Reset, () => (int)crc8.Type);
            case 16:
                var crc16 = new CRC16Accumulator((CRC16Type)variant);
                return new(() => crc16.GetCurrentValue(), crc16.Append, crc16.Reset, () => (int)crc16.Type);
            case 32:
                var crc32 = new CRC32Accumulator((CRC32Type)variant);
                return new(() => crc32.GetCurrentValue(), crc32.Append, crc32.Reset, () => (int)crc32.Type);
            default:
                throw new ArgumentOutOfRangeException(nameof(width));
        }
    }

    private static void AppendInChunks(AccumulatorProbe accumulator, byte[] source, int chunkSize)
    {
        for (int offset = 0; offset < source.Length; offset += chunkSize)
            accumulator.Append(source.AsSpan(offset, Math.Min(chunkSize, source.Length - offset)));
    }

    private static IEnumerable<byte[]> SmallBinaryInputs(int length)
    {
        yield return new byte[length];
        yield return Enumerable.Repeat((byte)0xFF, length).ToArray();
        yield return DeterministicBytes(length, (uint)length);
    }

    private static byte[] DeterministicBytes(int length, uint seed)
    {
        var result = new byte[length];
        uint state = seed == 0 ? 1u : seed;
        for (int index = 0; index < result.Length; index++)
            result[index] = (byte)NextRandom(ref state);
        return result;
    }

    private static uint NextRandom(ref uint state)
    {
        // Explicit xorshift32 keeps both bytes and partitions stable across runtimes.
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    private delegate void AppendOperation(ReadOnlySpan<byte> source);

    // This adapter only dispatches the public API; it has no CRC state or logic.
    private sealed record AccumulatorProbe(
        Func<uint> Read, AppendOperation Append, Action Reset, Func<int> ReadType);
}

#pragma warning restore CS0618
