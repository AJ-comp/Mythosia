using Mythosia.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mythosia.Test
{
    public class CryptographyTest
    {
        [Fact]
        public void LEATest()
        {
            LEA lea = new LEA();

            var key = new List<byte>()
            {
                0x0f, 0x1e, 0x2d, 0x3c, 0x4b, 0x5a, 0x69, 0x78,
                0x87, 0x96, 0xa5, 0xb4, 0xc3, 0xd2, 0xe1, 0xf0
            };

            var data = new List<byte>()
            {
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
                0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
            };

            var answer = new List<byte>()
            {
                0x9f, 0xc8, 0x4e, 0x35, 0x28, 0xc6, 0xc6, 0x18,
                0x55, 0x32, 0xc7, 0xa7, 0x04, 0x64, 0x8b, 0xfd
            };

            var encryptedData = lea.Encrypt(key.ToArray(), data.ToArray());
            Assert.True(encryptedData.SequenceEqual(answer));

            var decryptedData = lea.Decrypt(key.ToArray(), encryptedData);
            Assert.True(decryptedData.SequenceEqual(data));


        }

        public void StartTest(string contentToTest)
        {
            TestSymmetric("AES128", contentToTest, () =>
            {
                var key = KeyGenerator.GenerateAES128Key();
                var iv = KeyGenerator.GenerateAES128IV();

                var aes128Encrypted = contentToTest.ToUTF8Array().EncryptAES(key, iv);
                Console.WriteLine($"The encrypted result is {aes128Encrypted.ToDecimalString()}");
                var aes128Decrypted = aes128Encrypted.DecryptAES(key, iv).ToUTF8String();
                Console.WriteLine($"The decrypted result is {aes128Decrypted}");

                return contentToTest == aes128Decrypted ? "Success" : "Fail";
            });


            TestSymmetric("3DES", contentToTest, () =>
            {
                var key = KeyGenerator.Generate3DESKey();
                var iv = KeyGenerator.Generate3DESIV();

                var tripleDesEncrypted = contentToTest.ToUTF8Array().Encrypt3DES(key, iv);
                Console.WriteLine($"The encrypted result is {tripleDesEncrypted.ToDecimalString()}");
                var tripleDesDecrypted = tripleDesEncrypted.Decrypt3DES(key, iv).ToUTF8String();
                Console.WriteLine($"The decrypted result is {tripleDesDecrypted}");

                return contentToTest == tripleDesDecrypted ? "Success" : "Fail";
            });


            TestSymmetric("DES", contentToTest, () =>
            {
                var key = KeyGenerator.GenerateDESKey();
                var iv = KeyGenerator.GenerateDESIV();

                var tripleDesEncrypted = contentToTest.ToUTF8Array().EncryptDES(key, iv);
                Console.WriteLine($"The encrypted result is {tripleDesEncrypted.ToDecimalString()}");
                var tripleDesDecrypted = tripleDesEncrypted.DecryptDES(key, iv).ToUTF8String();
                Console.WriteLine($"The decrypted result is {tripleDesDecrypted}");

                return contentToTest == tripleDesDecrypted ? "Success" : "Fail";
            });

            TestSymmetric("SEED", contentToTest, () =>
            {
                var key = KeyGenerator.GenerateSEEDKey();
                var iv = KeyGenerator.GenerateSEEDIV();

                var seedEncrypted = contentToTest.ToUTF8Array().EncryptSEED(key);
                Console.WriteLine($"The encrypted result is {seedEncrypted.ToDecimalString()}");
                var seedDecrypted = seedEncrypted.DecryptSEED(key).ToUTF8String();
                Console.WriteLine($"The decrypted result is {seedDecrypted}");

                return contentToTest == seedDecrypted ? "Success" : "Fail";
            });
        }


        private void TestSymmetric(string name, string contentToTest, Func<string> logic)
        {
            Console.WriteLine("/".Repeat(20));
            Console.WriteLine($"Start to test {name} with {contentToTest}");

            var result = logic.Invoke();

            Console.WriteLine($"{name} Test is {result}");
            Console.WriteLine("/".Repeat(20));
        }
    }
}
