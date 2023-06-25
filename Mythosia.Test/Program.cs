using Mythosia;
using Mythosia.Integrity;
using Mythosia.Test;
using Mythosia.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;


IntegrityTest integrityTest = new();
integrityTest.StartTest();

EnumerableTest enumTest = new();
enumTest.StartTest(new List<byte>() { 10, 16, 15, 30, 45, 65 });

CryptographyTest cryptographyTest = new();
cryptographyTest.StartTest("test12345");

SymmetricAlgorithm symmetricAlgorithm = Aes.Create();
var key = KeyGenerator.GenerateAES128Key();
var iv = KeyGenerator.GenerateAES128IV();

var encrypted = symmetricAlgorithm.Encrypt("test123456".ToUTF8Array(), key, iv);
Console.WriteLine(symmetricAlgorithm.Decrypt(encrypted, key, iv).ToUTF8String());