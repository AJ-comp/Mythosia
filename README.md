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

