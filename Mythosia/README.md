# Mythosia
This project supports custom functions that are not directly provided by .NET as extension methods so that they can be conveniently used within the project. <br/>
The extensions supported by this project include the following. <br/>

```c#
using Mythosia;

// Example for string extension
var data = "12345".ToDefaultArray(); // Equal with Encoding.Default.GetBytes("12345");
var data = "12345".ToASCIIArray(); // Equal with Encoding.ASCII.GetBytes("12345");
var data = "=".Repeat(10); // data is "=========="

// Example for numeric (byte, short, int, etc...) extension
var result = 56.IsInRange(0, 100);  // result is true 
var result = 56.IsInRange(0, 30);   // result is false
var data = 56000000.ToSIPrefix();   // data is "56 M"
var data = ((double)423.42031).ConvertEndian();	// change endian

// Example for Enumerable extension
new List<byte> test = new List<byte>(){0xff, 0xab, 0x01, 0x00, 0xee};
var result = test.ToUnPrefixedHexString();	// result is "ff ab 01 00 ee"
var result = test.ToPrefixedHexString();		// result is "0xffab0100ee"
var result = test.ToEncodedString(Encoding.GetEncoding("ISO-8859-1"));		// convert string as "ISO-8859-1" format
var result = test.ToASCIIString()	// equal with Encoding.ASCII.GetString(test.ToArray(), 0, test.Count());