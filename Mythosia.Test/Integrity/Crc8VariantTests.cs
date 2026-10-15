using System.Collections;
using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Crc8VariantTests
{
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    public static IEnumerable<object[]> Variants()
    {
        yield return new object[] { CRC8Type.SaeJ1850 };
        yield return new object[] { CRC8Type.AutosarH2F };
    }

    public static IEnumerable<object[]> PublishedVectors()
    {
        foreach (object[] vector in Crc8SaeJ1850PublishedVectors.Cases())
            yield return new object[] { CRC8Type.SaeJ1850, vector[0], vector[1], vector[2] };
        foreach (object[] vector in Crc8AutosarH2FPublishedVectors.Cases())
            yield return new object[] { CRC8Type.AutosarH2F, vector[0], vector[1], vector[2] };
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void PublishedCorpusContainsAtLeastThirtyDistinctNonemptyInputs(CRC8Type type)
    {
        object[][] vectors = PublishedVectors().Where(vector => (CRC8Type)vector[0] == type).ToArray();
        Assert.True(vectors.Length >= 30, $"{type} needs at least thirty frozen public answers.");
        Assert.All(vectors, vector => Assert.NotEmpty((byte[])vector[2]));
        Assert.Equal(vectors.Length, vectors.Select(vector => Convert.ToHexString((byte[])vector[2])).Distinct().Count());
    }

    [Theory]
    [InlineData(CRC8Type.SaeJ1850, 0x4B, 0xC4)]
    [InlineData(CRC8Type.AutosarH2F, 0xDF, 0x42)]
    public void StandardChecksAndRawResiduesIdentifyTheExactModel(CRC8Type type, byte check, byte residue)
    {
        // CRC RevEng's catalogue publishes checks and raw residues for these models.
        // Its residue is the register before the final XOR, unlike our numeric API.
        Assert.Equal(check, CrcReference.Compute8(CheckInput, type));
        Assert.Equal(check, CheckInput.CRC8(type));
        byte[] codeword = CheckInput.Append(check).ToArray();
        Assert.Equal(residue, (byte)(CrcReference.Compute8(codeword, type) ^ byte.MaxValue));
        Assert.Equal(residue, (byte)(codeword.CRC8(type) ^ byte.MaxValue));
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public async Task PublishedAnswersMatchOracleNumericDigestFramesSplitsAndStreams(
        CRC8Type type, string name, byte[] input, byte expected)
    {
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.Equal(expected, CrcReference.Compute8(input, type));
        Assert.Equal(expected, input.CRC8(type));
        ErrorDetection detector = new CRC8(type);
        Assert.Equal(new[] { expected }, detector.Compute(input));
        byte[] frame = input.Append(expected).ToArray();
        Assert.Equal(frame, detector.Encode(input));
        Assert.Equal(frame, input.WithCRC8(type));
        Assert.False(detector.IsError(frame));
        Assert.Equal(input, detector.Decode(frame));

        var accumulator = new CRC8Accumulator(type);
        foreach (int split in Splits(input.Length))
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            if (split > 0)
            {
                byte prefix = CrcReference.Compute8(input.Take(split), type);
                Assert.Equal(prefix, accumulator.GetCurrentValue());
                Assert.Equal(prefix, accumulator.GetCurrentValue());
            }
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(input.AsSpan(split));
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }

        using var sync = new MemoryStream(input, writable: false);
        using var asyncSource = new MemoryStream(input, writable: false);
        Assert.Equal(expected, sync.ComputeCRC8(type));
        Assert.Equal(expected, await asyncSource.ComputeCRC8Async(type));
        Assert.True(sync.CanRead);
        Assert.True(asyncSource.CanRead);
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void IndependentFramesRejectEverySingleBitMutationAndRequireAPayload(CRC8Type type)
    {
        var detector = new CRC8(type);
        byte[] frame = CheckInput.Append(CrcReference.Compute8(CheckInput, type)).ToArray();
        for (int bit = 0; bit < frame.Length * 8; bit++)
        {
            byte[] damaged = frame.ToArray();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.True(detector.IsError(damaged));
            Assert.Empty(detector.Decode(damaged));
        }
        Assert.True(detector.IsError(Array.Empty<byte>()));
        for (int digest = 0; digest <= byte.MaxValue; digest++)
        {
            Assert.True(detector.IsError(new[] { (byte)digest }));
            Assert.Empty(detector.Decode(new[] { (byte)digest }));
        }
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void EmptyInputKeepsTheCrc8ContractAndResetRestoresTheInitialState(CRC8Type type)
    {
        var detector = new CRC8(type);
        Assert.Equal(type, detector.Type);
        Assert.Equal(type.ToString(), detector.GetDetectionType());
        Assert.Empty(detector.Compute(Array.Empty<byte>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => Array.Empty<byte>().CRC8(type));
        Assert.Throws<ArgumentOutOfRangeException>(() => detector.Encode(Array.Empty<byte>()).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => Array.Empty<byte>().WithCRC8(type).ToArray());

        var accumulator = new CRC8Accumulator(type);
        Assert.Throws<InvalidOperationException>(() => accumulator.GetCurrentValue());
        accumulator.Append(default);
        Assert.Throws<InvalidOperationException>(() => accumulator.GetCurrentValue());
        accumulator.Append(CheckInput);
        accumulator.Reset();
        accumulator.Reset();
        Assert.Equal(type, accumulator.Type);
        Assert.Throws<InvalidOperationException>(() => accumulator.GetCurrentValue());
        accumulator.Append(CheckInput);
        Assert.Equal(CrcReference.Compute8(CheckInput, type), accumulator.GetCurrentValue());
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void CalculationAcceptsSinglePassSourcesAndDisposesThemOnFailure(CRC8Type type)
    {
        byte expected = CrcReference.Compute8(CheckInput, type);
        var source = new SinglePassSource(CheckInput);
        Assert.Equal(new[] { expected }, new CRC8(type).Compute(source));
        Assert.Equal(1, source.Enumerations);
        Assert.Equal(1, source.Disposals);

        var extensionSource = new SinglePassSource(CheckInput);
        Assert.Equal(expected, extensionSource.CRC8(type));
        Assert.Equal(1, extensionSource.Enumerations);
        Assert.Equal(1, extensionSource.Disposals);

        var faultingSource = new SinglePassSource(CheckInput, failAfter: 4);
        var failure = Assert.Throws<IOException>(() => new CRC8(type).Compute(faultingSource));
        Assert.Same(faultingSource.Failure, failure);
        Assert.Equal(1, faultingSource.Enumerations);
        Assert.Equal(1, faultingSource.Disposals);
        Assert.Throws<ArgumentNullException>(() => new CRC8(type).Compute(null!));
        Assert.Throws<ArgumentNullException>(() => ExtensionForCRC8.CRC8(null!, type));
    }

    [Fact]
    public void EnumValuesDefaultsAliasesAndLegacyAnswersStayUnchanged()
    {
#pragma warning disable CS0618 // These aliases remain public compatibility contracts.
        Assert.Equal(0, (int)CRC8Type.Basic);
        Assert.Equal(1, (int)CRC8Type.Classic);
        Assert.Equal(2, (int)CRC8Type.Maxim);
        Assert.Equal(3, (int)CRC8Type.SaeJ1850);
        Assert.Equal(4, (int)CRC8Type.AutosarH2F);
        Assert.Equal(new[] { CRC8Type.Basic, CRC8Type.Classic, CRC8Type.Maxim, CRC8Type.SaeJ1850, CRC8Type.AutosarH2F, CRC8Type.SensirionSht4x }, Enum.GetValues<CRC8Type>());
        Assert.Equal(CRC8Type.Classic, new CRC8().Type);
        Assert.Equal(CRC8Type.Classic, new CRC8Accumulator().Type);
        Assert.Equal((byte)0xF4, CheckInput.CRC8());
        Assert.Equal((byte)0xF4, CheckInput.CRC8(CRC8Type.Basic));
        Assert.Equal((byte)0xA1, CheckInput.CRC8(CRC8Type.Maxim));
        Assert.Equal(new CRC8().Compute(CheckInput), new CRC8(CRC8Type.Basic).Compute(CheckInput));
#pragma warning restore CS0618
        foreach (int undefined in new[] { -1, 6, int.MaxValue })
        {
            var detector = new CRC8((CRC8Type)undefined);
            Assert.Empty(detector.Compute(CheckInput));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CRC8Accumulator((CRC8Type)undefined));
        }
    }

    private static IEnumerable<int> Splits(int length) => length <= 512
        ? Enumerable.Range(0, length + 1)
        : new[] { 0, 1, 7, 8, 31, 32, 255, 256, 257, 65535, 65536, 65537, length / 2, length - 1, length }
            .Where(split => split <= length).Distinct();

    private sealed class SinglePassSource(byte[] bytes, int failAfter = -1) : IEnumerable<byte>
    {
        public int Enumerations { get; private set; }
        public int Disposals { get; private set; }
        public IOException Failure { get; } = new("Injected source failure.");
        public IEnumerator<byte> GetEnumerator()
        {
            if (++Enumerations != 1) throw new InvalidOperationException("Source was enumerated twice.");
            return Enumerate().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private IEnumerable<byte> Enumerate()
        {
            try
            {
                for (int index = 0; index < bytes.Length; index++)
                {
                    if (index == failAfter) throw Failure;
                    yield return bytes[index];
                }
            }
            finally { Disposals++; }
        }
    }
}
