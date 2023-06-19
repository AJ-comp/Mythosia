using Mythosia;
using Mythosia.Integrity;
using Mythosia.Test;
using Mythosia.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

List<byte> test = new List<byte>() { 10, 16, 15, 30, 45, 65 };
Console.WriteLine(test.ToUnPrefixedHexString());


IntegrityTest integrityTest = new IntegrityTest();
integrityTest.StartTest();


var result = "test123456".ToUTF8Array().EncryptAES("0123456789ABCDEF", "ABCDEF0123456789");
Console.WriteLine(result.DecryptAES("0123456789ABCDEF", "ABCDEF0123456789").ToUTF8String());
