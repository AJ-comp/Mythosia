using Mythosia.Integrity;
using Mythosia.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mythosia.Integrity.ExtensionForChecksum;
using static Mythosia.Integrity.ExtensionForCRC16;
using static Mythosia.Integrity.ExtensionForCRC32;
using static Mythosia.Integrity.ExtensionForCRC8;

namespace Mythosia.Test
{
    internal class IntegrityTest
    {
        public void StartTest()
        {
            IEnumerable<byte> testValue1 = "123456789".ToASCIIArray();
            IEnumerable<byte> testValue2 = "123456789lsdfilksdfksfsopfsdIcanfslfeafysfe".ToASCIIArray();
            IEnumerable<byte> testValue3 = "123456789lsdfilksdfks123FfelsdfiADSFsafkSAFSasdfefadsMLEIRP".ToASCIIArray();

            Console.WriteLine(testValue1.WithCheckSum8().ToUnPrefixedHexString());
            Console.WriteLine(testValue2.WithCheckSum8(CheckSum8Type.Modulo256).ToUnPrefixedHexString());
            Console.WriteLine(testValue3.WithCheckSum8(CheckSum8Type.TwosComplement).ToUnPrefixedHexString());

            TestCRC8(CRC8Type.Basic, testValue1, testValue2, testValue3);
//            TestCRC8(CRC8Type.CDMA2000, testValue1, testValue2, testValue3);
            TestCRC8(CRC8Type.Maxim, testValue1, testValue2, testValue3);
            TestCRC16(CRC16Type.Basic, testValue1, testValue2, testValue3);
            TestCRC16(CRC16Type.Modbus, testValue1, testValue2, testValue3);
            TestCRC16(CRC16Type.CCITTxModem, testValue1, testValue2, testValue3);
            TestCRC16(CRC16Type.DNP, testValue1, testValue2, testValue3);
            TestCRC32(CRC32Type.Basic, testValue1, testValue2, testValue3);

            TestIV(IVHashType.SHA1, testValue1, testValue2, testValue3);
            TestIV(IVHashType.SHA256, testValue1, testValue2, testValue3);
            TestIV(IVHashType.SHA384, testValue1, testValue2, testValue3);
            TestIV(IVHashType.SHA512, testValue1, testValue2, testValue3);
            TestIV(IVHashType.MD2, testValue1, testValue2, testValue3);
            TestIV(IVHashType.MD4, testValue1, testValue2, testValue3);
            TestIV(IVHashType.MD5, testValue1, testValue2, testValue3);
        }


        void TestCRC8(CRC8Type type, params IEnumerable<byte>[] testValues)
        {
            foreach (var testValue in testValues)
            {
                Console.WriteLine("------------------------------------------------------------------");
                Console.WriteLine($"CRC8 [{type}] test for source [{testValue.ToUnPrefixedHexString()}]");
                Console.WriteLine($"crc is 0x{testValue.CRC8(type).ToUnPrefixedHexString()}");
                Console.WriteLine($"source + crc is 0x{testValue.WithCRC8(type).ToUnPrefixedHexString()}");
                Console.WriteLine("------------------------------------------------------------------");
            }
        }

        void TestCRC16(CRC16Type type, params IEnumerable<byte>[] testValues)
        {
            foreach (var testValue in testValues)
            {
                Console.WriteLine("------------------------------------------------------------------");
                Console.WriteLine($"CRC16 [{type}] test for source [{testValue.ToUnPrefixedHexString()}]");
                Console.WriteLine($"crc is 0x{testValue.CRC16(type).ToUnPrefixedHexString()}");
                Console.WriteLine($"source + crc is 0x{testValue.WithCRC16(type).ToUnPrefixedHexString()}");
                Console.WriteLine("------------------------------------------------------------------");
            }
        }

        void TestCRC32(CRC32Type type, params IEnumerable<byte>[] testValues)
        {
            foreach (var testValue in testValues)
            {
                Console.WriteLine("------------------------------------------------------------------");
                Console.WriteLine($"CRC32 [{type}] test for source [{testValue.ToUnPrefixedHexString()}]");
                Console.WriteLine($"crc is 0x{testValue.CRC32(type).ToUnPrefixedHexString()}");
                Console.WriteLine($"source + crc is 0x{testValue.WithCRC32(type).ToUnPrefixedHexString()}");
                Console.WriteLine("------------------------------------------------------------------");
            }
        }


        void TestIV(IVHashType type, params IEnumerable<byte>[] testValues)
        {
            foreach (var testValue in testValues)
            {
                Console.WriteLine("------------------------------------------------------------------");
                Console.WriteLine($"[{type}] test for source [{testValue.ToUnPrefixedHexString()}]");
                Console.WriteLine($"{type} is {testValue.IVHashCode(type).ToUnPrefixedHexString()}");
//                Console.WriteLine($"source + crc is 0x{testValue.WithCRC32(type).ToUnPrefixedHexString()}");
                Console.WriteLine("------------------------------------------------------------------");
            }
        }


        void TestCheckSum8(params IEnumerable<byte>[] testValues)
        {
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine($"CheckSum8 [CheckSum8 xor] test");
            foreach (var item in testValues)
            {
                Console.WriteLine($"test result 0x{item.CheckSum8(CheckSum8Type.Xor).ToUnPrefixedHexString()}");
            }
            Console.WriteLine("------------------------------------------------------------------");

            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine($"CheckSum8 [CheckSum8 modulo-256] test");
            foreach (var item in testValues)
            {
                Console.WriteLine($"test result 0x{item.CheckSum8(CheckSum8Type.Modulo256).ToUnPrefixedHexString()}");
            }
            Console.WriteLine("------------------------------------------------------------------");

            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine($"CheckSum8 [CheckSum8 2's complement] test");
            foreach (var item in testValues)
            {
                Console.WriteLine($"test result 0x{item.CheckSum8(CheckSum8Type.TwosComplement).ToUnPrefixedHexString()}");
            }
            Console.WriteLine("------------------------------------------------------------------");
        }
    }
}
