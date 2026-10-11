using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class ExternalCrcVectorTests
{
    // Frozen independently of Mythosia and the C# bitwise test oracle, using
    // Python 3.14.3: zlib.crc32(data) and binascii.crc_hqx(data, 0).
    // Generator expressions are retained below so the fixtures can be audited.
    // https://docs.python.org/3/library/zlib.html#zlib.crc32
    // https://docs.python.org/3/library/binascii.html#binascii.crc_hqx
    // No Python installation, network access, or package is needed to run them.
    [Theory]
    [InlineData("00", 0xD202EF8DU, 0x0000)]
    [InlineData("FF", 0xFF000000U, 0x1EF0)]
    [InlineData("0000", 0x41D912FFU, 0x0000)]
    [InlineData("FFFF", 0xFFFF0000U, 0x1D0F)]
    [InlineData("00010203", 0x8BB98613U, 0x6131)]
    [InlineData("DEADBEEF", 0x7C9CA35AU, 0xC457)]
    [InlineData("313233343536373839", 0xCBF43926U, 0x31C3)]
    [InlineData("000102030405060708090A0B0C0D0E0F", 0xCECEE288U, 0x513D)]
    [InlineData("0123456789ABCDEF80FF00", 0xC395ECEFU, 0xEAE2)]
    [InlineData("54686520717569636B2062726F776E20666F78206A756D7073206F76657220746865206C617A7920646F67", 0x414FA339U, 0xF0C8)]
    public void BinaryPayloads_MatchFrozenPythonValues(string hex, uint expectedCrc32, int expectedXmodem)
    {
        // Python regeneration: data = bytes.fromhex(hex)
        AssertExternalValues(Convert.FromHexString(hex), expectedCrc32, expectedXmodem);
    }

    [Theory]
    [InlineData(1, 0x56BCAE53U, 0x2252)]
    [InlineData(2, 0x42EB4EB2U, 0xCD59)]
    [InlineData(7, 0xC66EA606U, 0xA8C5)]
    [InlineData(8, 0xC81E5556U, 0xC391)]
    [InlineData(15, 0xB20BC94AU, 0x13C3)]
    [InlineData(16, 0xB800C4B6U, 0x0ACC)]
    [InlineData(31, 0xB51AE5D3U, 0x7F9D)]
    [InlineData(32, 0x13D6E8C3U, 0x3EFB)]
    [InlineData(63, 0xA289C715U, 0x3AAF)]
    [InlineData(64, 0xD5CDA253U, 0x3A09)]
    [InlineData(127, 0xEE7FF328U, 0x5BDB)]
    [InlineData(128, 0xF5EED1D4U, 0xFB42)]
    [InlineData(255, 0x4D33EF1CU, 0x362A)]
    [InlineData(256, 0x1F98DAF1U, 0x1092)]
    [InlineData(257, 0xA777D4CBU, 0x8601)]
    [InlineData(511, 0x7C427818U, 0x241C)]
    [InlineData(512, 0xB8CE8D07U, 0xE9CF)]
    [InlineData(513, 0xBEBCB4EDU, 0xC991)]
    [InlineData(1023, 0x5096881AU, 0x7BA4)]
    [InlineData(1024, 0x2030794BU, 0xB211)]
    [InlineData(4095, 0x444DB4CCU, 0x6E8F)]
    [InlineData(4096, 0xA22C56B6U, 0x4A0D)]
    [InlineData(4097, 0x741C94BCU, 0xC6DC)]
    [InlineData(65535, 0x96936E4BU, 0x07E0)]
    [InlineData(65536, 0xD1229EEFU, 0xD882)]
    [InlineData(65537, 0x66D87324U, 0xEA27)]
    public void PatternedPayloads_MatchFrozenPythonValues(int length, uint expectedCrc32, int expectedXmodem)
    {
        // Python regeneration:
        // data = bytes((i * 73 + (i >> 3) + 19) & 255 for i in range(length))
        byte[] data = Enumerable.Range(0, length)
            .Select(i => (byte)((i * 73 + (i >> 3) + 19) & 255)).ToArray();

        AssertExternalValues(data, expectedCrc32, expectedXmodem);
    }

    private static void AssertExternalValues(byte[] data, uint expectedCrc32, int expectedXmodem)
    {
        Assert.Equal(expectedCrc32, data.CRC32(CRC32Type.Classic));
        Assert.Equal((ushort)expectedXmodem, data.CRC16(CRC16Type.CCITTxModem));
        Assert.Equal(BitConverter.GetBytes(expectedCrc32), new CRC32().Compute(data).ToArray());
        Assert.Equal(BitConverter.GetBytes((ushort)expectedXmodem),
            new CRC16(CRC16Type.CCITTxModem).Compute(data).ToArray());
    }
}
