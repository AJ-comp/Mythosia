using System.Reflection;
using System.Text;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Crc32CastagnoliTests
{
    [Theory]
    [MemberData(nameof(Crc32CTests.PublishedVectors), MemberType = typeof(Crc32CTests))]
    public async Task PublishedCrc32CAnswersMatchCastagnoliNumericDigestFrameAccumulatorAndStreamPaths(
        string name, byte[] input, uint expected)
    {
        const CRC32Type type = CRC32Type.Castagnoli;
        var detector = new CRC32(type);
        byte[] digest = NativeBytes(expected);
        byte[] frame = input.Concat(digest).ToArray();

        // The same fixed public answers calibrate the shared independent
        // normal-polynomial oracle; production CRC32C is not the expected value.
        Assert.Equal(expected, CrcReference.Compute32(input, type));
        Assert.Equal(expected, input.CRC32(type));
        Assert.Equal(digest, detector.Compute(input));
        Assert.Equal(frame, detector.Encode(input));
        Assert.Equal(frame, input.WithCRC32(type));
        Assert.Equal(input, detector.Decode(frame));
        Assert.Equal(input.Length == 0, detector.IsError(frame));

        foreach (int chunkSize in new[] { 1, 3, 64 })
        {
            var accumulator = new CRC32Accumulator(type);
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            for (int offset = 0; offset < input.Length; offset += chunkSize)
                accumulator.Append(input.AsSpan(offset, Math.Min(chunkSize, input.Length - offset)));
            Assert.Equal(expected, accumulator.GetCurrentValue());
            Assert.Equal(expected, accumulator.GetCurrentValue());
            accumulator.Reset();
            Assert.Equal(0u, accumulator.GetCurrentValue());
        }

        using var synchronous = new MemoryStream(input, writable: false);
        using var asynchronous = new MemoryStream(input, writable: false);
        Assert.Equal(expected, synchronous.ComputeCRC32(type));
        Assert.Equal(expected, await asynchronous.ComputeCRC32Async(type));

        if (input.Length == 0) return;
        frame[0] ^= 1;
        Assert.True(detector.IsError(frame), name);
        Assert.Empty(detector.Decode(frame));
    }

    [Fact]
    public void CastagnoliHasExplicitIdentityAndKeepsIeeeDefaultsAndPublishedCorpus()
    {
        byte[] check = Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(5, (int)CRC32Type.Castagnoli);
        Assert.Equal("Castagnoli", Enum.GetName(CRC32Type.Castagnoli));
        Assert.Equal(CRC32Type.Castagnoli, new CRC32(CRC32Type.Castagnoli).Type);
        Assert.Equal(CRC32Type.Castagnoli, new CRC32Accumulator(CRC32Type.Castagnoli).Type);
        Assert.Equal(CRC32Type.Classic, new CRC32().Type);
        Assert.Equal(CRC32Type.Classic, new CRC32Accumulator().Type);
        Assert.Equal(0xCBF43926u, check.CRC32());
        Assert.Equal(0xE3069283u, check.CRC32(CRC32Type.Castagnoli));
        Assert.Equal(new CRC32C().Compute(check), new CRC32(CRC32Type.Castagnoli).Compute(check));
        Assert.Equal(check.WithCRC32C(), check.WithCRC32(CRC32Type.Castagnoli));

        string[] nonemptyInputs = Crc32CTests.PublishedVectors()
            .Select(vector => (byte[])vector[1]).Where(input => input.Length > 0)
            .Select(Convert.ToHexString).ToArray();
        Assert.True(nonemptyInputs.Length >= 30, "Castagnoli requires at least 30 published nonempty inputs.");
        Assert.Equal(nonemptyInputs.Length, nonemptyInputs.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void OnlyLegacyNumericCrc32CMethodIsObsoleteWithPreciseMigrationGuidance()
    {
        MethodInfo? legacyNumeric = typeof(ExtensionForCRC32C)
            .GetMethod("CRC32C", new[] { typeof(IEnumerable<byte>) });
        Assert.NotNull(legacyNumeric);
        ObsoleteAttribute obsolete = Assert.IsType<ObsoleteAttribute>(
            legacyNumeric.GetCustomAttribute<ObsoleteAttribute>());
        Assert.Equal("CRC32C is obsolete and will be removed in a future version. Use CRC32(CRC32Type.Castagnoli) instead.",
            obsolete.Message);
        Assert.False(obsolete.IsError);
        Assert.Equal(typeof(uint), legacyNumeric.ReturnType);

        foreach (Type type in new[] { typeof(CRC32C), typeof(CRC32CAccumulator), typeof(ExtensionForCRC32C) })
        {
            Assert.Null(type.GetCustomAttribute<ObsoleteAttribute>());
            foreach (MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance
                | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (member.Equals(legacyNumeric)) continue;
                Assert.Null(member.GetCustomAttribute<ObsoleteAttribute>());
            }
        }

        foreach (MethodInfo method in typeof(StreamCRCExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name is "ComputeCRC32C" or "ComputeCRC32CAsync"))
            Assert.Null(method.GetCustomAttribute<ObsoleteAttribute>());
    }

    private static byte[] NativeBytes(uint value)
    {
        byte[] bytes = { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }
}
