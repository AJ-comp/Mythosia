using System.ComponentModel;
using Xunit;

namespace Mythosia.Test
{
    public class EnumerableTest
    {
        public enum CarBrand
        {
            [Description("Mercedes")] Benz = 0,
            [Description("Bayerische Motoren Werke AG")] BMW,
        }

        private List<byte> testList = new List<byte>() { 10, 16, 15, 30, 45, 65 };


        [Fact]
        public void BinaryStringTest()
        {
            Assert.True(testList.ToBinaryString(BinaryPartitionSize.None) == "000010100001000000001111000111100010110101000001");
            Assert.True(testList.ToBinaryString(BinaryPartitionSize.Bit2) == "00 00 10 10 00 01 00 00 00 00 11 11 00 01 11 10 00 10 11 01 01 00 00 01");
            Assert.True(testList.ToBinaryString(BinaryPartitionSize.HalfByte) == "0000 1010 0001 0000 0000 1111 0001 1110 0010 1101 0100 0001");
            Assert.True(testList.ToBinaryString(BinaryPartitionSize.Byte) == "00001010 00010000 00001111 00011110 00101101 01000001");

            Assert.True(testList.ToBinaryString(BinaryPartitionSize.None, "0b") == "0b000010100001000000001111000111100010110101000001");
            Assert.True(testList.ToBinaryString(BinaryPartitionSize.None, "0b", "p") == "0b000010100001000000001111000111100010110101000001p");
            Assert.True(testList.ToBinaryString(BinaryPartitionSize.Byte, "0b") == "0b00001010 0b00010000 0b00001111 0b00011110 0b00101101 0b01000001");
            Assert.True(testList.ToBinaryString(BinaryPartitionSize.Byte, "0b", "p") == "0b00001010p 0b00010000p 0b00001111p 0b00011110p 0b00101101p 0b01000001p");
        }


        [Fact]
        public void HexStringTest()
        {
            Assert.True(testList.ToPrefixedHexString() == "0x0a100f1e2d41");
            Assert.True(testList.ToPrefixedHexString(true) == "0x0a 0x10 0x0f 0x1e 0x2d 0x41");
            Assert.True(testList.ToUnPrefixedHexString() == "0a 10 0f 1e 2d 41");
            Assert.True(testList.ToUnPrefixedHexString("") == "0a100f1e2d41");

            Assert.True(testList.ToHexStringL(HexPartitionSize.None) == "0a100f1e2d41");
            Assert.True(testList.ToHexStringL(HexPartitionSize.Byte) == "0a 10 0f 1e 2d 41");
            Assert.True(testList.ToHexStringL(HexPartitionSize.Byte2) == "0a10 0f1e 2d41");
            Assert.True(testList.ToHexStringU(HexPartitionSize.Byte2) == "0A10 0F1E 2D41");

            Assert.True(testList.ToHexStringL(HexPartitionSize.None, "0x") == "0x0a100f1e2d41");
            Assert.True(testList.ToHexStringL(HexPartitionSize.Byte, "0x") == "0x0a 0x10 0x0f 0x1e 0x2d 0x41");
            Assert.True(testList.ToHexStringL(HexPartitionSize.Byte2, "0x") == "0x0a10 0x0f1e 0x2d41");
            Assert.True(testList.ToHexStringL(HexPartitionSize.Byte2, "", "h") == "0a10h 0f1eh 2d41h");
            Assert.True(testList.ToHexStringU(HexPartitionSize.Byte2, "0X") == "0X0A10 0X0F1E 0X2D41");

            int test = -32768;
            Assert.True(test.ToUnPrefixedHexString() == "ff ff 80 00");

            short shortTest = -32768;
            Assert.True(shortTest.ToUnPrefixedHexString() == "80 00");
        }


        [Fact]
        public void ToNumericTest()
        {
            List<byte> test = new List<byte>() { 10, 16, 15, 30, 45, 65, 90, 32 };
            List<byte> test2 = new List<byte>() { 10, 16, 15, 30, 45, 65, 90, 32, 74 };

            var ushortBigEndianAnswer = new List<ushort>() { 0x0a10, 0x0f1e, 0x2d41, 0x5a20 };
            var ushortLittleAnswer = new List<ushort>() { 0x100a, 0x1e0f, 0x412d, 0x205a };
            var ushortLittleAnswer2 = new List<ushort>() { 0x100a, 0x1e0f, 0x412d, 0x205a, 0x004a };

            var uintBigAnswer = new List<uint>() { 0x0a100f1e, 0x2d415a20 };
            var uintLittleAnswer = new List<uint>() { 0x1e0f100a, 0x205a412d };
            var uintLittleAnswer2 = new List<uint>() { 0x1e0f100a, 0x205a412d, 0x0000004a };

            var ulongBigAnswer = new List<ulong>() { 0x0a100f1e2d415a20 };
            var ulongLittleAnswer = new List<ulong>() { 0x205a412d1e0f100a };
            var ulongLittleAnswer2 = new List<ulong>() { 0x205a412d1e0f100a, 0x000000000000004a };

            if (BitConverter.IsLittleEndian)
            {
                Assert.True(test.ToUShortArray().SequenceEqual(ushortLittleAnswer));
                Assert.True(test.ToUIntArray().SequenceEqual(uintLittleAnswer));
                Assert.True(test.ToULongArray().SequenceEqual(ulongLittleAnswer));
                Assert.True(test2.ToUShortArray().SequenceEqual(ushortLittleAnswer2));
                Assert.True(test2.ToUIntArray().SequenceEqual(uintLittleAnswer2));
                Assert.True(test2.ToULongArray().SequenceEqual(ulongLittleAnswer2));
            }
        }


        [Fact]
        public void EnumTest()
        {
            var enumValue = "Bayerische Motoren Werke AG".GetEnumFromDescription<CarBrand>();

            Assert.True(enumValue == CarBrand.BMW);
            Assert.True(enumValue.ToString() == "BMW");
            Assert.True(1.ToEnum<CarBrand>() == CarBrand.BMW);
        }
    }
}
