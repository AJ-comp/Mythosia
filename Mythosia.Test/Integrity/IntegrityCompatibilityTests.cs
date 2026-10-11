using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

// These explicitly characterize existing behavior, including known limitations.
// They are compatibility checks, not endorsements of empty-input semantics or
// of the legacy DNP representation as standard DNP3 wire encoding.
[Collection(IntegrityTestCollection.Name)]
[Trait("Category", "Compatibility")]
public sealed class IntegrityCompatibilityTests
{
    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void Checksum8_EmptyInput_PreservesLegacyEmptyDigestAndEncodeException(Checksum8Type type)
    {
        byte[] empty = Array.Empty<byte>();
        var detector = new Checksum8(type);

        Assert.Empty(detector.Compute(empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.Checksum8(type));
        Assert.Throws<ArgumentOutOfRangeException>(() => detector.Encode(empty).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.WithChecksum8(type).ToArray());
        Assert.True(detector.IsError(empty));
        Assert.True(detector.IsError(new byte[] { 0 }));
        Assert.Empty(detector.Decode(empty));
        Assert.Empty(detector.Decode(new byte[] { 0 }));
    }

    [Theory]
    [InlineData(CRC8Type.Classic)]
    [InlineData(CRC8Type.Maxim)]
    public void Crc8_EmptyInput_PreservesLegacyEmptyDigestAndEncodeException(CRC8Type type)
    {
        byte[] empty = Array.Empty<byte>();
        var detector = new CRC8(type);

        Assert.Empty(detector.Compute(empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.CRC8(type));
        Assert.Throws<ArgumentOutOfRangeException>(() => detector.Encode(empty).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.WithCRC8(type).ToArray());
        Assert.True(detector.IsError(empty));
        Assert.True(detector.IsError(new byte[] { 0 }));
        Assert.Empty(detector.Decode(empty));
    }

    [Theory]
    [InlineData(CRC16Type.Classic, 0x0000)]
    [InlineData(CRC16Type.Modbus, 0xFFFF)]
    [InlineData(CRC16Type.CCITTxModem, 0x0000)]
    [InlineData(CRC16Type.DNP, 0x0000)]
    public void Crc16_EmptyInput_PreservesLegacyValueAndCrcOnlyFrameRejection(CRC16Type type, int expected)
    {
        // DNP's canonical empty-input value is FFFF; its legacy early return is
        // intentionally recorded here rather than used to calibrate an oracle.
        byte[] empty = Array.Empty<byte>();
        byte[] expectedBytes = BitConverter.GetBytes((ushort)expected);
        var detector = new CRC16(type);

        Assert.Equal((ushort)expected, empty.CRC16(type));
        Assert.Equal(expectedBytes, detector.Compute(empty).ToArray());
        Assert.Equal(expectedBytes, detector.Encode(empty).ToArray());
        Assert.Equal(expectedBytes, empty.WithCRC16(type).ToArray());
        Assert.True(detector.IsError(expectedBytes));
        Assert.Empty(detector.Decode(expectedBytes));
    }

    [Fact]
    public void Crc32_EmptyInput_PreservesZeroDigestAndCrcOnlyFrameRejection()
    {
        byte[] empty = Array.Empty<byte>();
        byte[] expected = new byte[4];
        var detector = new CRC32();

        Assert.Equal(0U, empty.CRC32(CRC32Type.Classic));
        Assert.Equal(expected, detector.Compute(empty).ToArray());
        Assert.Equal(expected, detector.Encode(empty).ToArray());
        Assert.Equal(expected, empty.WithCRC32(CRC32Type.Classic).ToArray());
        Assert.True(detector.IsError(expected));
        Assert.Empty(detector.Decode(expected));
    }

    [Theory]
    [InlineData("ChecksumXor", typeof(ArgumentNullException))]
    [InlineData("ChecksumNmea", typeof(ArgumentNullException))]
    [InlineData("ChecksumModulo256", typeof(ArgumentNullException))]
    [InlineData("ChecksumTwosComplement", typeof(ArgumentNullException))]
    [InlineData("Crc8Classic", typeof(ArgumentNullException))]
    [InlineData("Crc8Maxim", typeof(ArgumentNullException))]
    [InlineData("Crc16Classic", typeof(ArgumentNullException))]
    [InlineData("Crc16Modbus", typeof(NullReferenceException))]
    [InlineData("Crc16Xmodem", typeof(ArgumentNullException))]
    [InlineData("Crc16Dnp", typeof(ArgumentNullException))]
    [InlineData("Crc32Classic", typeof(ArgumentNullException))]
    public void PublicOperations_NullInput_PreserveLegacyExceptionTypes(
        string algorithm, Type expectedComputeException)
    {
        ErrorDetection detector = CreateDetector(algorithm);

        // Modbus Compute currently enters foreach directly and dereferences null.
        // Record that limitation explicitly without weakening assertions for
        // Encode/Decode/IsError, which do throw ArgumentNullException.
        Assert.Throws(expectedComputeException, () => detector.Compute(null!).ToArray());
        Assert.Throws<ArgumentNullException>(() => detector.Encode(null!).ToArray());
        Assert.Throws<ArgumentNullException>(() => detector.Decode(null!).ToArray());
        Assert.Throws<ArgumentNullException>(() => detector.IsError(null!));
    }

    [Fact]
    public void IndependentOracle_EmptyInput_HasCanonicalInitialAndFinalValues()
    {
        byte[] empty = Array.Empty<byte>();
        Assert.Equal((byte)0, CrcReference.Compute8(empty, CRC8Type.Classic));
        Assert.Equal((byte)0, CrcReference.Compute8(empty, CRC8Type.Maxim));
        Assert.Equal((ushort)0, CrcReference.Compute16(empty, CRC16Type.Classic));
        Assert.Equal((ushort)0xFFFF, CrcReference.Compute16(empty, CRC16Type.Modbus));
        Assert.Equal((ushort)0, CrcReference.Compute16(empty, CRC16Type.CCITTxModem));
        Assert.Equal((ushort)0xFFFF, CrcReference.Compute16(empty, CRC16Type.DNP));
        Assert.Equal(0U, CrcReference.Compute32(empty, CRC32Type.Classic));
    }

    [Fact]
    public void CrcDefaultsAndBasicAliases_PreserveClassicNumericResultsAndNames()
    {
        byte[] data = Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(Checksum8Type.Xor, new Checksum8().Type);
        Assert.Equal("Xor", new Checksum8().GetDetectionType());
        Assert.Equal(CRC8Type.Classic, new CRC8().Type);
        Assert.Equal(CRC16Type.Classic, new CRC16().Type);
        Assert.Equal(CRC32Type.Classic, new CRC32().Type);

        Assert.Equal((byte)0xF4, data.CRC8());
        Assert.Equal((ushort)0xBB3D, data.CRC16());
        Assert.Equal(0xCBF43926U, data.CRC32());

#pragma warning disable CS0618 // Deprecated public aliases remain supported.
        Assert.Equal(new CRC8().Compute(data), new CRC8(CRC8Type.Basic).Compute(data));
        Assert.Equal(new CRC16().Compute(data), new CRC16(CRC16Type.Basic).Compute(data));
        Assert.Equal(new CRC32().Compute(data), new CRC32(CRC32Type.Basic).Compute(data));
        Assert.Equal(new CRC8().Encode(data), data.WithCRC8(CRC8Type.Basic));
        Assert.Equal(new CRC16().Encode(data), data.WithCRC16(CRC16Type.Basic));
        Assert.Equal(new CRC32().Encode(data), data.WithCRC32(CRC32Type.Basic));
        Assert.Equal("Basic", new CRC8(CRC8Type.Basic).GetDetectionType());
        Assert.Equal("Basic", new CRC16(CRC16Type.Basic).GetDetectionType());
        Assert.Equal("Basic", new CRC32(CRC32Type.Basic).GetDetectionType());
#pragma warning restore CS0618
    }

    private static ErrorDetection CreateDetector(string algorithm) => algorithm switch
    {
        "ChecksumXor" => new Checksum8(),
        "ChecksumNmea" => new Checksum8(Checksum8Type.NMEA),
        "ChecksumModulo256" => new Checksum8(Checksum8Type.Modulo256),
        "ChecksumTwosComplement" => new Checksum8(Checksum8Type.TwosComplement),
        "Crc8Classic" => new CRC8(),
        "Crc8Maxim" => new CRC8(CRC8Type.Maxim),
        "Crc16Classic" => new CRC16(),
        "Crc16Modbus" => new CRC16(CRC16Type.Modbus),
        "Crc16Xmodem" => new CRC16(CRC16Type.CCITTxModem),
        "Crc16Dnp" => new CRC16(CRC16Type.DNP),
        "Crc32Classic" => new CRC32(),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
    };
}
