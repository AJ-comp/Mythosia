using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Mythosia.Integrity.CRC;
using Mythosia.Test.Integrity;

namespace Mythosia.Test.Performance;

internal static class IntegrityBenchmarkData
{
    public static ErrorDetection CreateDetector(IntegrityAlgorithm algorithm) => algorithm switch
    {
        IntegrityAlgorithm.ChecksumXor => new Checksum8(Checksum8Type.Xor),
        IntegrityAlgorithm.ChecksumNmea => new Checksum8(Checksum8Type.NMEA),
        IntegrityAlgorithm.ChecksumModulo256 => new Checksum8(Checksum8Type.Modulo256),
        IntegrityAlgorithm.ChecksumTwosComplement => new Checksum8(Checksum8Type.TwosComplement),
        IntegrityAlgorithm.Crc8Classic => new CRC8(CRC8Type.Classic),
        IntegrityAlgorithm.Crc8Maxim => new CRC8(CRC8Type.Maxim),
        IntegrityAlgorithm.Crc16Classic => new CRC16(CRC16Type.Classic),
        IntegrityAlgorithm.Crc16Modbus => new CRC16(CRC16Type.Modbus),
        IntegrityAlgorithm.Crc16Xmodem => new CRC16(CRC16Type.CCITTxModem),
        IntegrityAlgorithm.Crc16Dnp => new CRC16(CRC16Type.DNP),
        IntegrityAlgorithm.Crc32Classic => new CRC32(CRC32Type.Classic),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
    };

    public static byte[] CreatePayload(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        byte[] payload = new byte[length];
        uint state = 0xD1B54A35;
        for (int i = 0; i < payload.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            payload[i] = (byte)(state >> 24);
        }
        return payload;
    }

    public static IEnumerable<byte> CreateSource(byte[] values, IntegrityInput input) => input switch
    {
        IntegrityInput.Array => values,
        IntegrityInput.List => new List<byte>(values),
        IntegrityInput.Yield => Enumerate(values),
        _ => throw new ArgumentOutOfRangeException(nameof(input), input, null)
    };

    // No collection interface: Count must traverse it, and every enumeration
    // starts again. LINQ collection fast paths cannot mask sequential-input cost.
    private static IEnumerable<byte> Enumerate(byte[] values)
    {
        foreach (byte value in values)
            yield return value;
    }

    public static byte[] ComputeExpectedDigest(byte[] payload, IntegrityAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case IntegrityAlgorithm.ChecksumXor:
            case IntegrityAlgorithm.ChecksumNmea:
                byte xor = 0;
                foreach (byte value in payload) xor ^= value;
                return [xor];
            case IntegrityAlgorithm.ChecksumModulo256:
            case IntegrityAlgorithm.ChecksumTwosComplement:
                uint sum = 0;
                foreach (byte value in payload) sum += value;
                return [(byte)(algorithm == IntegrityAlgorithm.ChecksumModulo256 ? sum : unchecked(0u - sum))];
            case IntegrityAlgorithm.Crc8Classic:
                return [CrcReference.Compute8(payload, CRC8Type.Classic)];
            case IntegrityAlgorithm.Crc8Maxim:
                return [CrcReference.Compute8(payload, CRC8Type.Maxim)];
            case IntegrityAlgorithm.Crc16Classic:
                return PackNative(CrcReference.Compute16(payload, CRC16Type.Classic), 2);
            case IntegrityAlgorithm.Crc16Modbus:
                return PackNative(CrcReference.Compute16(payload, CRC16Type.Modbus), 2);
            case IntegrityAlgorithm.Crc16Xmodem:
                return PackNative(CrcReference.Compute16(payload, CRC16Type.CCITTxModem), 2);
            case IntegrityAlgorithm.Crc16Dnp:
                ushort canonical = CrcReference.Compute16(payload, CRC16Type.DNP);
                return PackNative(CrcReference.ToLegacyDnpRepresentation(canonical), 2);
            case IntegrityAlgorithm.Crc32Classic:
                return PackNative(CrcReference.Compute32(payload, CRC32Type.Classic), 4);
            default:
                throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
        }
    }

    internal static byte[] PackNative(uint value, int width)
    {
        byte[] result = new byte[width];
        for (int i = 0; i < width; i++)
        {
            int shift = 8 * (BitConverter.IsLittleEndian ? i : width - 1 - i);
            result[i] = (byte)(value >> shift);
        }
        return result;
    }
}
