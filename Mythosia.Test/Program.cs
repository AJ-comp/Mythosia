using Mythosia;
using Mythosia.Integrity;
using Mythosia.Test;
using static Mythosia.Integrity.ExtensionForCRC16;
using static Mythosia.Integrity.ExtensionForCRC32;
using static Mythosia.Integrity.ExtensionForCRC8;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

List<byte> test = new List<byte>() { 10, 16, 15, 30, 45, 65 };
Console.WriteLine(test.ToUnPrefixedHexString());


IntegrityTest integrityTest = new IntegrityTest();
integrityTest.StartTest();
