using Mythosia.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia.Test
{
    public class CryptographyTest
    {
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
