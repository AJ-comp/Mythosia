# Mythosia
This project supports custom functions that are not directly provided by .NET as extension methods so that they can be conveniently used within the project. <br/>
The extensions supported by this project include the following. <br/>


## To string or To Byte array
```c#
using Mythosia;

var data = "=".Repeat(10); // data is "=========="

var data = "12345".ToDefaultArray(); // Equal with Encoding.Default.GetBytes("12345");
var data = "12345".ToASCIIArray(); // Equal with Encoding.ASCII.GetBytes("12345");
var data = "12345".ToUTF8Array(); // Equal with Encoding.UTF8.GetBytes("12345");
var data = "12345".ToUTF32Array(); // Equal with Encoding.UTF32.GetBytes("12345");

IEnumerable<byte> data = new List<byte>() { 0x45, 0x46, 0x47, 0x48, 0x49 };
var result = data.AsOrToArray(); // if data is byte[] then return data (O(1)) else return data.ToArray() (O(n));

data.ToDefaultString(); // Equal with Encoding.Default.GetString(data.AsOrToByteArray(), 0, data.Count());
data.ToASCIIString(); // Equal with Encoding.ASCII.GetString(data.AsOrToByteArray(), 0, data.Count());
data.ToUTF8String(); // Equal with Encoding.UTF8.GetString(data.AsOrToByteArray(), 0, data.Count());
data.ToUTF32String(); // Equal with Encoding.UTF32.GetString(data.AsOrToByteArray(), 0, data.Count());

```


## To numeric string (sbyte, byte, short, ushort, int, uint, float, double)
```c#
using Mythosia;

var result = 56.IsInRange(0, 100);  // result is true 
var result = 56.IsInRange(0, 30);   // result is false

// see https://m.blog.naver.com/alluck/220931066035
var data = 56000000.ToSIPrefix();   // data is "56 M"  (find the unit that can present the best simple)
var data = 56000000.ToSIPrefix(SIPrefixUnit.Kilo); // data is "56000 k"
var data = 56000000.ToSIPrefix(SIPrefixUnit.Giga, 5); // data is "0.056 G"  (the second param means the maximum number of digits after decimal point)

var data = 1.4235123.ToSIPrefix();  // data is "1.42"  (find the unit that can present the best simple)
var data = 1.4235123.ToSIPrefix(SIPrefixUnit.Mili);  // data is "1423.51 m"
var data = 1.4235123.ToSIPrefix(SIPrefixUnit.Mili, 5);  // data is "1423.5123 m" 
var data = 1.4235123.ToSIPrefix(SIPrefixUnit.Micro);  // data is "1423512.3 u"

var data = 423.42031.HostToNetworkEndian();	// change endian (host to big)
var data = 234.52.ToByteArray();	// Equal with BitConverter.GetBytes(234.52);
```


## To Numeric array (ushort, uint, ulong)
```c#
using Mythosia;

List<byte> test = new List<byte>() { 10, 16, 15, 30, 45, 65, 90, 32 };

// if system endian is little endian then result is 0x100a, 0x1e0f, 0x412d, 0x205a
// if system endian is big endian then result is 0x0a10, 0x0f1e, 0x2d41, 0x5a20
var result = test.ToUShortArray();

// if system endian is little endian then result is 0x1e0f100a, 0x205a412d
// if system endian is big endian then result is 0x0a100f1e, 0x2d415a20
var result = test.ToUIntArray();

// if system endian is little endian then result is 0x205a412d1e0f100a
// if system endian is big endian then result is 0x0a100f1e2d415a20
var result = test.ToULongArray();

```


## Notation
```c#
using Mythosia;

private List<byte> test = new List<byte>() { 10, 16, 15, 30, 45, 65 };

// binary
test.ToBinaryString();  // result is "000010100001000000001111000111100010110101000001"
test.ToBinaryString(BinaryPartitionSize.Bit2) // result is "00 00 10 10 00 01 00 00 00 00 11 11 00 01 11 10 00 10 11 01 01 00 00 01"
test.ToBinaryString(BinaryPartitionSize.HalfByte) // result is "0000 1010 0001 0000 0000 1111 0001 1110 0010 1101 0100 0001"
test.ToBinaryString(BinaryPartitionSize.Byte) // result is "00001010 00010000 00001111 00011110 00101101 01000001"
test.ToBinaryString(BinaryPartitionSize.Byte, "0b") // result is "0b00001010 0b00010000 0b00001111 0b00011110 0b00101101 0b01000001"

// hex
var result = test.ToHexStringL();  // result is  "0a100f1e2d41"
var result = test.ToHexStringL(HexPartitionSize.Byte);  // result is "0a 10 0f 1e 2d 41
var result = test.ToHexStringL(HexPartitionSize.Byte2); // result is "0a10 0f1e 2d41"

var result = test.ToHexStringU(HexPartitionSize.Byte2);  // result is "0A10 0F1E 2D41"
var result = test.ToHexStringU(HexPartitionSize.Byte2, "0x");  // result is "0x0A10 0x0F1E 0x2D41"
var result = test.ToHexStringU(HexPartitionSize.Byte2, "", "h");  // result is "0A10h 0F1Eh 2D41h"

```


## Enumerable (included ConcurrentBag)
```c#
var result = test.ToEncodedString(Encoding.GetEncoding("ISO-8859-1"));		// convert string as "ISO-8859-1" format
var result = test.ToASCIIString();	// equal with Encoding.ASCII.GetString(test.ToArray(), 0, test.Count());

var result = test.IndexOf(new List(){ 0xab, 0x01 });	// return the index that subsequence is finded.
test.AddExceptNull(item);					// add item if item is not null

new List<byte> newItems = new List<bye>(){ 0x01, 0x02 };
test.AddRangeParallel(newItems);    // add items as parallel
```


## Delegate
```c#
using Mythosia;

// If you have a function that success or fails according to condition as below.
bool WireConnect()
{
    // Check whether the wire is connected
    // If connected to wire return true else false.
}


// Here you may want to call the function to repeat to specified timeout.
// Then you can solute just by calling the function "RetryIfFailed" as below. 
void Test()
{
    var func = WireConnect;             // you need c# 10.0
    bool result = func.RetryIfFailed(30000);      // Call WireConnect function to repeat while a maximum of 30,000 ms (the 30s) or until success

    if(result) Console.WriteLine("Success");
    else Console.WriteLine("Failed");
}

```

## Stream
```c#
using Mythosia;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class TestMarshalClass
{
    public int a;
    public int b;
}

TestMarshalClass test = new();

var serializeData = test.SerializeUsingMarshal();   // Serialize
test.DeSerializeUsingMarshal(serializeData);    // Deserialize to test

```

## Enum
```c#
using Mythosia;

public enum CarBrand
{
    [Description("Mercedes")] Benz = 0,
    [Description("Bayerische Motoren Werke AG")] BMW,
}

CarBrand test = CarBrand.Benz;
var value = test.ToDescription();   // value is "Mercedes"
var enum = value.GetEnumFromDescription<CarBrand>();   // enum is CarBrand.Benz

int carBrand = 1;
var result = carBrand.ToEnum<CarBrand>();   // result is CarBrand.BMW
var result = "BMW".GetEnumFromName<CarBrand>();  // result is CarBrand.BMW

```