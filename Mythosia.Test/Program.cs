using Mythosia;
using Mythosia.Integrity;
using Mythosia.Test;
using Mythosia.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;


IntegrityTest integrityTest = new IntegrityTest();
integrityTest.StartTest();

EnumerableTest enumTest = new EnumerableTest();
enumTest.StartTest(new List<byte>() { 10, 16, 15, 30, 45, 65 });

var key = KeyGenerator.GenerateAES128Key();
var iv = KeyGenerator.GenerateAES128IV();

var result = "test123456".ToUTF8Array().EncryptAES(key, iv);
Console.WriteLine(result.DecryptAES(key, iv).ToUTF8String());

key = KeyGenerator.Generate3DESKey();
iv = KeyGenerator.Generate3DESIV();

var desResult = "test1234".ToUTF8Array().Encrypt3DES(key, iv, CipherMode.ECB, PaddingMode.Zeros);
Console.WriteLine(desResult.Decrypt3DES(key, iv, CipherMode.ECB, PaddingMode.Zeros).ToUTF8String());
