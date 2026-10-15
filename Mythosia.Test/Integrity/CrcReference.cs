using Mythosia.Integrity.CRC;

namespace Mythosia.Test.Integrity;

// Exercise the obsolete aliases because they remain part of the public API.
#pragma warning disable CS0618

/// <summary>
/// Independent, bit-at-a-time oracle. It uses normal-form polynomials and a
/// left-shifting register even for reflected CRCs, and never uses production
/// lookup tables, Compute methods, or numeric conversion extensions.
/// </summary>
internal static class CrcReference
{
    // Parameter/check-value provenance: Greg Cook's CRC RevEng catalogue.
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-smbus
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-maxim-dow
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-sae-j1850
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-autosar
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-arc
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-modbus
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-xmodem
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-dnp
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-ibm-3740
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-ibm-sdlc
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-iso-hdlc
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-nrsc-5
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-mcrf4xx
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-16-usb
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-mpeg-2
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-bzip2
    // https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-32-autosar
    private static readonly Parameters Smbus = new(8, 0x07, 0, false, false, 0);
    private static readonly Parameters Maxim = new(8, 0x31, 0, true, true, 0);
    private static readonly Parameters SaeJ1850 = new(8, 0x1D, 0xFF, false, false, 0xFF);
    private static readonly Parameters AutosarH2F = new(8, 0x2F, 0xFF, false, false, 0xFF);
    private static readonly Parameters SensirionSht4x = new(8, 0x31, 0xFF, false, false, 0);
    private static readonly Parameters Mcrf4xx = new(16, 0x1021, 0xFFFF, true, true, 0);
    private static readonly Parameters Usb = new(16, 0x8005, 0xFFFF, true, true, 0xFFFF);
    private static readonly Parameters Mpeg2 = new(32, 0x04C11DB7, 0xFFFFFFFF, false, false, 0);
    private static readonly Parameters Bzip2 = new(32, 0x04C11DB7, 0xFFFFFFFF, false, false, 0xFFFFFFFF);
    private static readonly Parameters AutosarP4 = new(32, 0xF4ACFB13, 0xFFFFFFFF, true, true, 0xFFFFFFFF);
    private static readonly Parameters Arc = new(16, 0x8005, 0, true, true, 0);
    private static readonly Parameters Modbus = new(16, 0x8005, 0xFFFF, true, true, 0);
    private static readonly Parameters Xmodem = new(16, 0x1021, 0, false, false, 0);
    private static readonly Parameters Dnp = new(16, 0x3D65, 0, true, true, 0xFFFF);
    private static readonly Parameters CcittFalse = new(16, 0x1021, 0xFFFF, false, false, 0);
    private static readonly Parameters X25 = new(16, 0x1021, 0xFFFF, true, true, 0xFFFF);
    private static readonly Parameters IsoHdlc = new(32, 0x04C11DB7, 0xFFFFFFFF, true, true, 0xFFFFFFFF);

    public static byte Compute8(IEnumerable<byte> source, CRC8Type type)
    {
        var parameters = type switch
        {
            CRC8Type.Basic or CRC8Type.Classic => Smbus,
            CRC8Type.Maxim => Maxim,
            CRC8Type.SaeJ1850 => SaeJ1850,
            CRC8Type.AutosarH2F => AutosarH2F,
            CRC8Type.SensirionSht4x => SensirionSht4x,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        return (byte)Compute(source, parameters);
    }

    /// <summary>Returns the canonical numerical CRC, including canonical DNP.</summary>
    public static ushort Compute16(IEnumerable<byte> source, CRC16Type type)
    {
        var parameters = type switch
        {
            CRC16Type.Basic or CRC16Type.Classic => Arc,
            CRC16Type.Modbus => Modbus,
            CRC16Type.CCITTxModem => Xmodem,
            CRC16Type.DNP => Dnp,
            CRC16Type.CCITTFalse => CcittFalse,
            CRC16Type.X25 => X25,
            CRC16Type.MCRF4XX => Mcrf4xx,
            CRC16Type.USB => Usb,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        return (ushort)Compute(source, parameters);
    }

    public static uint Compute32(IEnumerable<byte> source, CRC32Type type)
    {
        Parameters parameters = type switch
        {
            CRC32Type.Basic or CRC32Type.Classic => IsoHdlc,
            CRC32Type.Mpeg2 => Mpeg2,
            CRC32Type.Bzip2 => Bzip2,
            CRC32Type.AutosarP4 => AutosarP4,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        return Compute(source, parameters);
    }

    // Compatibility adapter, deliberately outside the standards oracle.
    // Mythosia currently swaps DNP's canonical result before native packing.
    public static ushort ToLegacyDnpRepresentation(ushort canonical)
        => (ushort)((canonical << 8) | (canonical >> 8));

    private static uint Compute(IEnumerable<byte> source, Parameters parameters)
    {
        uint register = parameters.Initial;
        uint highestBit = 1u << (parameters.Width - 1);
        uint mask = parameters.Width == 32 ? uint.MaxValue : (1u << parameters.Width) - 1;

        foreach (byte value in source)
        {
            uint input = parameters.ReflectInput ? Reflect(value, 8) : value;
            for (int inputBit = 7; inputBit >= 0; inputBit--)
            {
                uint feedback = ((register & highestBit) >> (parameters.Width - 1))
                    ^ ((input >> inputBit) & 1);
                register = (register << 1) & mask;
                if (feedback != 0)
                    register ^= parameters.Polynomial;
            }
        }

        if (parameters.ReflectOutput)
            register = Reflect(register, parameters.Width);

        return (register ^ parameters.XorOutput) & mask;
    }

    private static uint Reflect(uint value, int width)
    {
        uint reflected = 0;
        for (int bit = 0; bit < width; bit++)
        {
            reflected = (reflected << 1) | (value & 1);
            value >>= 1;
        }
        return reflected;
    }

    private readonly record struct Parameters(
        int Width, uint Polynomial, uint Initial,
        bool ReflectInput, bool ReflectOutput, uint XorOutput);
}

#pragma warning restore CS0618
