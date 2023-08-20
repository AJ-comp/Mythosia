using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Mythosia.Integrity.CRC;
using Mythosia.Security.Cryptography;
using System.Linq;
using Xunit;
using static Mythosia.Integrity.Checksum.ExtensionForChecksum;
using static Mythosia.Integrity.CRC.ExtensionForCRC16;
using static Mythosia.Integrity.CRC.ExtensionForCRC32;
using static Mythosia.Integrity.CRC.ExtensionForCRC8;

namespace Mythosia.Test
{
    public class Checksum8TestType
    {
        public Checksum8Type Type { get; }
        public IEnumerable<byte> Content => _content;
        public byte CRC { get; }


        private List<byte> _content = new List<byte>();

        public Checksum8TestType(Checksum8Type type, IEnumerable<byte> content, byte crc)
        {
            Type = type;
            CRC = crc;
            _content.AddRange(content);
        }
    }

    public class CRC8TestType
    {
        public CRC8Type Type { get; }
        public IEnumerable<byte> Content => _content;
        public byte CRC { get; }


        private List<byte> _content = new List<byte>();

        public CRC8TestType(CRC8Type type, IEnumerable<byte> content, byte crc)
        {
            Type = type;
            _content.AddRange(content);
            CRC = crc;
        }
    }

    public class CRC16TestType
    {
        public CRC16Type Type { get; }
        public IEnumerable<byte> Content => _content;
        public ushort CRC { get; }


        private List<byte> _content = new List<byte>();

        public CRC16TestType(CRC16Type type, IEnumerable<byte> content, ushort crc)
        {
            Type = type;
            _content.AddRange(content);
            CRC = crc;
        }
    }

    public class CRC32TestType
    {
        public CRC32Type Type { get; }
        public IEnumerable<byte> Content => _content;
        public uint CRC { get; }


        private List<byte> _content = new List<byte>();

        public CRC32TestType(CRC32Type type, IEnumerable<byte> content, uint crc)
        {
            Type = type;
            _content.AddRange(content);
            CRC = crc;
        }
    }

    public class IntegrityTest
    {
        IEnumerable<byte> testValue1 { get; } = "123456789".ToASCIIArray();
        IEnumerable<byte> testValue2 { get; } = "123456789lsdfilksdfksfsopfsdIcanfslfeafysfe".ToASCIIArray();
        IEnumerable<byte> testValue3 { get; } = "123456789lsdfilksdfks123FfelsdfiADSFsafkSAFSasdfefadsMLEIRP".ToASCIIArray();
        IEnumerable<byte> testValue4 { get; } = new List<byte>() { 0x04, 0x00, 0x00, 0x82, 0xA7, 0x03, 0x44, 0x4A, 0x4C, 0x55, 0x32, 0x39, 0x30, 0x30, 0x36, 0x35, 0x30 };


        [Fact]
        public void Checksum8Test()
        {
            var classicQ1 = new Checksum8TestType(Checksum8Type.Xor, testValue1, 0x31);
            var classicQ2 = new Checksum8TestType(Checksum8Type.Xor, testValue2, 0x01);
            var classicQ3 = new Checksum8TestType(Checksum8Type.Xor, testValue3, 0x48);

            Checksum8TestCore(classicQ1, classicQ2, classicQ3);

            var nmeaQ1 = new Checksum8TestType(Checksum8Type.NMEA, testValue1, 0x31);
            var nmeaQ2 = new Checksum8TestType(Checksum8Type.NMEA, testValue2, 0x01);
            var nmeaQ3 = new Checksum8TestType(Checksum8Type.NMEA, testValue3, 0x48);

            Checksum8TestCore(nmeaQ1, nmeaQ2, nmeaQ3);

            var modulo256Q1 = new Checksum8TestType(Checksum8Type.Modulo256, testValue1, 0xdd);
            var modulo256Q2 = new Checksum8TestType(Checksum8Type.Modulo256, testValue2, 0xdf);
            var modulo256Q3 = new Checksum8TestType(Checksum8Type.Modulo256, testValue3, 0xf4);

            Checksum8TestCore(modulo256Q1, modulo256Q2, modulo256Q3);

            var twosCompQ1 = new Checksum8TestType(Checksum8Type.TwosComplement, testValue1, 0x23);
            var twosCompQ2 = new Checksum8TestType(Checksum8Type.TwosComplement, testValue2, 0x21);
            var twosCompQ3 = new Checksum8TestType(Checksum8Type.TwosComplement, testValue3, 0x0c);

            Checksum8TestCore(twosCompQ1, twosCompQ2, twosCompQ3);
        }

        [Fact]
        public void CRC8Test()
        {
            var classicQ1 = new CRC8TestType(CRC8Type.Classic, testValue1, 0xf4);
            var classicQ2 = new CRC8TestType(CRC8Type.Classic, testValue2, 0x32);
            var classicQ3 = new CRC8TestType(CRC8Type.Classic, testValue3, 0x95);

            CRC8TestCore(classicQ1, classicQ2, classicQ3);

            var maximQ1 = new CRC8TestType(CRC8Type.Maxim, testValue1, 0xA1);
            var maximQ2 = new CRC8TestType(CRC8Type.Maxim, testValue2, 0xD9);
            var maximQ3 = new CRC8TestType(CRC8Type.Maxim, testValue3, 0xD1);

            CRC8TestCore(maximQ1, maximQ2, maximQ3);
        }

        [Fact]
        public void CRC16Test()
        {
            var classicQ1 = new CRC16TestType(CRC16Type.Classic, testValue1, 0xBB3D);
            var classicQ2 = new CRC16TestType(CRC16Type.Classic, testValue2, 0x8476);
            var classicQ3 = new CRC16TestType(CRC16Type.Classic, testValue3, 0xE9FB);

            CRC16TestCore(classicQ1, classicQ2, classicQ3);

            var modbusQ1 = new CRC16TestType(CRC16Type.Modbus, testValue1, 0x4B37);
            var modbusQ2 = new CRC16TestType(CRC16Type.Modbus, testValue2, 0xD035);
            var modbusQ3 = new CRC16TestType(CRC16Type.Modbus, testValue3, 0xD7DE);

            CRC16TestCore(modbusQ1, modbusQ2, modbusQ3);

            var dnpQ1 = new CRC16TestType(CRC16Type.DNP, testValue1, 0x82EA);
            var dnpQ2 = new CRC16TestType(CRC16Type.DNP, testValue2, 0x903E);
            var dnpQ3 = new CRC16TestType(CRC16Type.DNP, testValue3, 0xE9F7);

            CRC16TestCore(dnpQ1, dnpQ2, dnpQ3);

            var ccitt_xmodemQ1 = new CRC16TestType(CRC16Type.CCITTxModem, testValue1, 0x31C3);
            var ccitt_xmodemQ2 = new CRC16TestType(CRC16Type.CCITTxModem, testValue2, 0x2C24);
            var ccitt_xmodemQ3 = new CRC16TestType(CRC16Type.CCITTxModem, testValue3, 0xC8B6);
            var ccitt_xmodemQ4 = new CRC16TestType(CRC16Type.CCITTxModem, testValue4, 0xea00);

            CRC16TestCore(ccitt_xmodemQ1, ccitt_xmodemQ2, ccitt_xmodemQ3, ccitt_xmodemQ4);
        }

        [Fact]
        public void CRC32Test()
        {
            var classicQ1 = new CRC32TestType(CRC32Type.Classic, testValue1, 0xCBF43926);
            var classicQ2 = new CRC32TestType(CRC32Type.Classic, testValue2, 0x53673587);
            var classicQ3 = new CRC32TestType(CRC32Type.Classic, testValue3, 0x0C0ABFEF);

            CRC32TestCore(classicQ1, classicQ2, classicQ3);
        }



        private void Checksum8TestCore(params Checksum8TestType[] testTypes)
        {
            foreach (var testType in testTypes)
            {
                var testCRC = testType.CRC;
                var withCRC = Sum(testType.Content, testCRC.ToByteArray());

                Assert.True(testType.Content.Checksum8(testType.Type) == testCRC);
                Assert.True(testType.Content.WithChecksum8(testType.Type).SequenceEqual(withCRC));

                var crc = new Checksum8(testType.Type);

                Assert.True(TestOnInstance(crc, testType.Content, testCRC.ToByteArray()));
            }
        }


        private void CRC8TestCore(params CRC8TestType[] testTypes)
        {
            foreach (var testType in testTypes)
            {
                var testCRC = testType.CRC;
                var withCRC = Sum(testType.Content, testCRC.ToByteArray());

                Assert.True(testType.Content.CRC8(testType.Type) == testCRC);
                Assert.True(testType.Content.WithCRC8(testType.Type).SequenceEqual(withCRC));

                var crc = new CRC8(testType.Type);

                Assert.True(TestOnInstance(crc, testType.Content, testCRC.ToByteArray()));
            }
        }


        private void CRC16TestCore(params CRC16TestType[] testTypes)
        {
            foreach(var testType in testTypes)
            {
                var testCRC = testType.CRC;
                var withCRC = Sum(testType.Content, testCRC.ToByteArray());

                Assert.True(testType.Content.CRC16(testType.Type) == testCRC);
                Assert.True(testType.Content.WithCRC16(testType.Type).SequenceEqual(withCRC));

                var crc = new CRC16(testType.Type);

                Assert.True(TestOnInstance(crc, testType.Content, testCRC.ToByteArray()));
            }
        }

        private void CRC32TestCore(params CRC32TestType[] testTypes)
        {
            foreach (var testType in testTypes)
            {
                var testCRC = testType.CRC;
                var withCRC = Sum(testType.Content, testCRC.ToByteArray());

                Assert.True(testType.Content.CRC32(testType.Type) == testCRC);
                Assert.True(testType.Content.WithCRC32(testType.Type).SequenceEqual(withCRC));

                var crc = new CRC32(testType.Type);

                Assert.True(TestOnInstance(crc, testType.Content, testCRC.ToByteArray()));
            }
        }


        private IEnumerable<byte> Sum(IEnumerable<byte> data1, IEnumerable<byte> data2)
        {
            var result = new List<byte>();
            result.AddRange(data1);
            result.AddRange(data2);

            return result;
        }


        private bool TestOnInstance(ErrorDetection errDetection, IEnumerable<byte> data, IEnumerable<byte> crcValue)
        {
            // compute test
            if (!errDetection.Compute(data).SequenceEqual(crcValue)) return false;

            // encode test
            var temp = new List<byte>(data);
            temp.AddRange(crcValue);
            var encode = errDetection.Encode(data);
            if (!encode.SequenceEqual(temp)) return false;

            // decode test
            return errDetection.Decode(encode).SequenceEqual(data);
        }
    }
}
