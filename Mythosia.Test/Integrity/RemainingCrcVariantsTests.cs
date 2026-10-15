using System.Collections;
using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class RemainingCrcVariantsTests
{
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    public static IEnumerable<object[]> Models()
    {
        yield return new object[] { 8, (int)CRC8Type.SensirionSht4x, 0xF7u };
        yield return new object[] { 16, (int)CRC16Type.MCRF4XX, 0x6F91u };
        yield return new object[] { 16, (int)CRC16Type.USB, 0xB4C8u };
        yield return new object[] { 32, (int)CRC32Type.Mpeg2, 0x0376E6E7u };
        yield return new object[] { 32, (int)CRC32Type.Bzip2, 0xFC891918u };
        yield return new object[] { 32, (int)CRC32Type.AutosarP4, 0x1697D06Au };
    }

    public static IEnumerable<object[]> PublishedVectors() => RemainingCrcVariantsPublishedVectors.Cases();

    [Theory]
    [MemberData(nameof(Models))]
    public void EveryModelHasAtLeastThirtyDistinctPublishedNonemptyAnswers(int width, int variant, uint _)
    {
        object[][] vectors = PublishedVectors().Where(row => (int)row[0] == width && (int)row[1] == variant).ToArray();
        Assert.True(vectors.Length >= 30);
        Assert.All(vectors, row => Assert.NotEmpty((byte[])row[3]));
        Assert.Equal(vectors.Length, vectors.Select(row => Convert.ToHexString((byte[])row[3])).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void StandardCheckCalibratesTheIndependentBitwiseOracle(int width, int variant, uint expected)
    {
        // CRC RevEng model checks, with exact parameter sets recorded in CrcReference.
        Assert.Equal(expected, Reference(CheckInput, width, variant));
        Assert.Equal(expected, Numeric(CheckInput, width, variant));
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public async Task PublishedAnswersMatchNumericNativeDigestFramesPartitionsAndStreams(
        int width, int variant, string name, byte[] input, uint expected)
    {
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.Equal(expected, Reference(input, width, variant));
        Assert.Equal(expected, Numeric(input, width, variant));
        ErrorDetection detector = Detector(width, variant);
        byte[] digest = NativeBytes(expected, width);
        Assert.Equal(digest, detector.Compute(input));
        byte[] frame = input.Concat(digest).ToArray();
        Assert.Equal(frame, detector.Encode(input));
        Assert.Equal(frame, WithDigest(input, width, variant));
        Assert.False(detector.IsError(frame));
        Assert.Equal(input, detector.Decode(frame));

        Accumulator accumulator = CreateAccumulator(width, variant);
        foreach (int split in Splits(input.Length))
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            if (split > 0)
            {
                uint prefix = Reference(input.Take(split), width, variant);
                Assert.Equal(prefix, accumulator.Read());
                Assert.Equal(prefix, accumulator.Read());
            }
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(input.AsSpan(split));
            Assert.Equal(expected, accumulator.Read());
        }

        using var sync = new MemoryStream(input, writable: false);
        using var asyncSource = new MemoryStream(input, writable: false);
        Assert.Equal(expected, ComputeStream(sync, width, variant));
        Assert.Equal(expected, await ComputeStreamAsync(asyncSource, width, variant));
        Assert.True(sync.CanRead);
        Assert.True(asyncSource.CanRead);
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void IndependentFramesRejectAllSingleBitChangesAndDigestOnlyFrames(int width, int variant, uint check)
    {
        ErrorDetection detector = Detector(width, variant);
        byte[] frame = CheckInput.Concat(NativeBytes(check, width)).ToArray();
        for (int bit = 0; bit < frame.Length * 8; bit++)
        {
            byte[] damaged = frame.ToArray();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.True(detector.IsError(damaged));
            Assert.Empty(detector.Decode(damaged));
        }
        Assert.True(detector.IsError(Array.Empty<byte>()));
        Assert.True(detector.IsError(NativeBytes(check, width)));
        Assert.Empty(detector.Decode(NativeBytes(check, width)));
    }

    [Theory]
    [MemberData(nameof(Models))]
    public void OneShotAcceptsForwardOnlySourcesAndDisposesThemOnFailure(int width, int variant, uint expected)
    {
        var source = new SinglePassSource(CheckInput);
        Assert.Equal(NativeBytes(expected, width), Detector(width, variant).Compute(source));
        Assert.Equal(1, source.Enumerations);
        Assert.Equal(1, source.Disposals);
        var faulting = new SinglePassSource(CheckInput, failAfter: 4);
        Assert.Same(faulting.Failure, Assert.Throws<IOException>(() => Detector(width, variant).Compute(faulting)));
        Assert.Equal(1, faulting.Enumerations);
        Assert.Equal(1, faulting.Disposals);
        Assert.Throws<ArgumentNullException>(() => Detector(width, variant).Compute(null!));
    }

    [Theory]
    [MemberData(nameof(Models))]
    public async Task EmptyInputAndResetFollowTheSpecifiedModelAndCrc8NoDataContract(int width, int variant, uint check)
    {
        var detector = Detector(width, variant);
        Accumulator accumulator = CreateAccumulator(width, variant);
        foreach (bool afterReset in new[] { false, true })
        {
            if (afterReset)
            {
                accumulator.Append(CheckInput);
                accumulator.Reset();
            }
            accumulator.Append(default);
            using var sync = new MemoryStream();
            using var asyncSource = new MemoryStream();
            if (width == 8)
            {
                Assert.Empty(detector.Compute(Array.Empty<byte>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => Numeric(Array.Empty<byte>(), width, variant));
                Assert.Throws<InvalidOperationException>(() => accumulator.Read());
                Assert.Throws<InvalidOperationException>(() => ComputeStream(sync, width, variant));
                await Assert.ThrowsAsync<InvalidOperationException>(() => ComputeStreamAsync(asyncSource, width, variant));
            }
            else
            {
                uint expected = Reference(Array.Empty<byte>(), width, variant);
                Assert.Equal(expected, Numeric(Array.Empty<byte>(), width, variant));
                Assert.Equal(NativeBytes(expected, width), detector.Compute(Array.Empty<byte>()));
                Assert.Equal(expected, accumulator.Read());
                Assert.Equal(expected, ComputeStream(sync, width, variant));
                Assert.Equal(expected, await ComputeStreamAsync(asyncSource, width, variant));
            }
            Assert.True(sync.CanRead);
            Assert.True(asyncSource.CanRead);
        }
        accumulator.Append(CheckInput);
        Assert.Equal(check, accumulator.Read());
    }

    [Fact]
    public void AddedEnumValuesAreStableAndExistingDefaultsAreUnchanged()
    {
        Assert.Equal(5, (int)CRC8Type.SensirionSht4x);
        Assert.Equal(7, (int)CRC16Type.MCRF4XX);
        Assert.Equal(8, (int)CRC16Type.USB);
        Assert.Equal(2, (int)CRC32Type.Mpeg2);
        Assert.Equal(3, (int)CRC32Type.Bzip2);
        Assert.Equal(4, (int)CRC32Type.AutosarP4);
        Assert.Equal(CRC8Type.Classic, new CRC8().Type);
        Assert.Equal(CRC16Type.Classic, new CRC16().Type);
        Assert.Equal(CRC32Type.Classic, new CRC32().Type);
    }

    internal static uint Reference(IEnumerable<byte> input, int width, int variant) => width switch
    {
        8 => CrcReference.Compute8(input, (CRC8Type)variant),
        16 => CrcReference.Compute16(input, (CRC16Type)variant),
        32 => CrcReference.Compute32(input, (CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    internal static uint Numeric(IEnumerable<byte> input, int width, int variant) => width switch
    {
        8 => input.CRC8((CRC8Type)variant),
        16 => input.CRC16((CRC16Type)variant),
        32 => input.CRC32((CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    internal static uint ComputeStream(Stream input, int width, int variant) => width switch
    {
        8 => input.ComputeCRC8((CRC8Type)variant),
        16 => input.ComputeCRC16((CRC16Type)variant),
        32 => input.ComputeCRC32((CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    internal static async Task<uint> ComputeStreamAsync(Stream input, int width, int variant, CancellationToken token = default) => width switch
    {
        8 => await input.ComputeCRC8Async((CRC8Type)variant, token),
        16 => await input.ComputeCRC16Async((CRC16Type)variant, token),
        32 => await input.ComputeCRC32Async((CRC32Type)variant, token),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static ErrorDetection Detector(int width, int variant) => width switch
    {
        8 => new CRC8((CRC8Type)variant),
        16 => new CRC16((CRC16Type)variant),
        32 => new CRC32((CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static IEnumerable<byte> WithDigest(byte[] input, int width, int variant) => width switch
    {
        8 => input.WithCRC8((CRC8Type)variant),
        16 => input.WithCRC16((CRC16Type)variant),
        32 => input.WithCRC32((CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static byte[] NativeBytes(uint value, int width) => width switch
    {
        8 => new[] { (byte)value },
        16 => BitConverter.GetBytes((ushort)value),
        32 => BitConverter.GetBytes(value),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static Accumulator CreateAccumulator(int width, int variant)
    {
        if (width == 8)
        {
            var crc = new CRC8Accumulator((CRC8Type)variant);
            return new(crc.Append, () => crc.GetCurrentValue(), crc.Reset);
        }
        if (width == 16)
        {
            var crc = new CRC16Accumulator((CRC16Type)variant);
            return new(crc.Append, () => crc.GetCurrentValue(), crc.Reset);
        }
        var crc32 = new CRC32Accumulator((CRC32Type)variant);
        return new(crc32.Append, crc32.GetCurrentValue, crc32.Reset);
    }

    private static IEnumerable<int> Splits(int length) => length <= 17
        ? Enumerable.Range(0, length + 1)
        : new[] { 0, 1, 7, 8, 15, 16, 31, 32, 63, 64, 127, 128, 255, 256, length / 2, length - 1, length }
            .Where(split => split <= length).Distinct();

    private delegate void AppendBytes(ReadOnlySpan<byte> input);
    private sealed record Accumulator(AppendBytes Append, Func<uint> Read, Action Reset);

    private sealed class SinglePassSource(byte[] input, int failAfter = -1) : IEnumerable<byte>
    {
        public int Enumerations { get; private set; }
        public int Disposals { get; private set; }
        public IOException Failure { get; } = new("Injected enumeration failure.");
        public IEnumerator<byte> GetEnumerator()
        {
            if (++Enumerations != 1) throw new InvalidOperationException("Source enumerated twice.");
            return Enumerate().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private IEnumerable<byte> Enumerate()
        {
            try
            {
                for (int index = 0; index < input.Length; index++)
                {
                    if (index == failAfter) throw Failure;
                    yield return input[index];
                }
            }
            finally { Disposals++; }
        }
    }
}
