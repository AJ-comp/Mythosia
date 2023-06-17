using Mythosia;
using Mythosia.Integrity;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

List<byte> test = new List<byte>() { 10, 16, 15, 30, 45, 65 };
Console.WriteLine(test.ToUnPrefixedHexString());
