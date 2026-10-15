using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class OspfLsaChecksumTests
{
    public static TheoryData<string, string, ushort> PublishedCases => OspfLsaPublishedVectors.Cases;

    [Fact]
    public void PublishedCoverageContainsAtLeastThirtyDistinctNonemptyFullLsas()
    {
        var cases = PublishedCases.ToArray();
        Assert.True(cases.Length >= 30);
        Assert.All(cases, row => Assert.True(Convert.FromHexString((string)row[1]).Length >= 20));
        Assert.Equal(cases.Length, cases.Select(row => ((string)row[1])[4..32] + ((string)row[1])[36..]).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(PublishedCases))]
    public void PublishedChecksumFieldsMatchCalculationIndependentReferenceAndWrite(string name, string hex, ushort expected)
    {
        byte[] lsa = Convert.FromHexString(hex);
        byte[] before = (byte[])lsa.Clone();
        Assert.Equal(expected, OspfLsaChecksum.Compute(lsa));
        Assert.Equal(expected, IndependentChecksum(lsa));
        Assert.True(OspfLsaChecksum.IsValid(lsa), name);
        Assert.Equal(before, lsa);

        lsa[16] = 0x12;
        lsa[17] = 0x34;
        Assert.Equal(expected, OspfLsaChecksum.Compute(lsa));
        OspfLsaChecksum.Write(lsa);
        Assert.Equal(before, lsa);
    }

    [Fact]
    public void AgeChangesAreExcludedAndWriteChangesOnlyChecksumBytes()
    {
        byte[] lsa = Convert.FromHexString((string)PublishedCases.First()[1]);
        ushort expected = OspfLsaChecksum.Compute(lsa);
        foreach (ushort age in new ushort[] { 0, 1, 3600, 0x7FFF, 0x8000, 0xFFFF })
        {
            lsa[0] = (byte)(age >> 8);
            lsa[1] = (byte)age;
            byte[] before = (byte[])lsa.Clone();
            lsa[16] = lsa[17] = 0;
            Assert.Equal(expected, OspfLsaChecksum.Compute(lsa));
            OspfLsaChecksum.Write(lsa);
            Assert.Equal(before, lsa);
            Assert.True(OspfLsaChecksum.IsValid(lsa));
        }
    }

    [Fact]
    public void EveryIncludedSingleBitMutationIsRejected()
    {
        byte[] lsa = Convert.FromHexString((string)PublishedCases.First()[1]);
        for (int index = 2; index < lsa.Length; index++)
        for (int bit = 0; bit < 8; bit++)
        {
            lsa[index] ^= (byte)(1 << bit);
            Assert.False(OspfLsaChecksum.IsValid(lsa));
            lsa[index] ^= (byte)(1 << bit);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(16)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(65536)]
    public void MalformedLengthsAreRejected(int length)
    {
        var lsa = new byte[length];
        Assert.False(OspfLsaChecksum.IsValid(lsa));
        Assert.Equal("lsa", Assert.Throws<ArgumentException>(() => OspfLsaChecksum.Compute(lsa)).ParamName);
        Assert.Equal("lsa", Assert.Throws<ArgumentException>(() => OspfLsaChecksum.Write(lsa)).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(19)]
    [InlineData(21)]
    [InlineData(65535)]
    public void HeaderLengthMustMatchExactly(int declaredLength)
    {
        var lsa = new byte[20];
        lsa[18] = (byte)(declaredLength >> 8);
        lsa[19] = (byte)declaredLength;
        Assert.False(OspfLsaChecksum.IsValid(lsa));
        Assert.Throws<ArgumentException>(() => OspfLsaChecksum.Compute(lsa));
        Assert.Throws<ArgumentException>(() => OspfLsaChecksum.Write(lsa));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(65535)]
    public void HeaderOnlyOddAndMaximumLengthLsasUseExactDeclaredRange(int length)
    {
        byte[] lsa = Enumerable.Range(0, length).Select(index => (byte)(index * 73 + 11)).ToArray();
        lsa[18] = (byte)(length >> 8);
        lsa[19] = (byte)length;
        ushort expected = IndependentChecksum(lsa);
        Assert.Equal(expected, OspfLsaChecksum.Compute(lsa));
        OspfLsaChecksum.Write(lsa);
        Assert.Equal((byte)(expected >> 8), lsa[16]);
        Assert.Equal((byte)expected, lsa[17]);
        Assert.True(OspfLsaChecksum.IsValid(lsa));
    }

    [Fact]
    public void ReservedZeroChecksumIsRejectedEvenWhenBothResiduesAreZero()
    {
        var lsa = new byte[255];
        lsa[19] = 255;
        Assert.False(OspfLsaChecksum.IsValid(lsa));
        Assert.Equal((ushort)0xFFFF, OspfLsaChecksum.Compute(lsa));
        OspfLsaChecksum.Write(lsa);
        Assert.True(OspfLsaChecksum.IsValid(lsa));
    }

    // Independent weighted-sum definition with an exhaustive search of the two check bytes.
    // It does not reuse the product's running second sum or check-byte correction formula.
    private static ushort IndependentChecksum(byte[] lsa)
    {
        long sum = 0, weighted = 0;
        for (int index = 2; index < lsa.Length; index++)
        {
            if (index == 16 || index == 17) continue;
            sum += lsa[index];
            weighted += (long)(lsa.Length - index) * lsa[index];
        }
        for (int high = 1; high <= 255; high++)
        for (int low = 1; low <= 255; low++)
            if ((sum + high + low) % 255 == 0
                && (weighted + (long)(lsa.Length - 16) * high + (long)(lsa.Length - 17) * low) % 255 == 0)
                return (ushort)((high << 8) | low);
        throw new InvalidOperationException("No check-byte pair exists.");
    }
}
