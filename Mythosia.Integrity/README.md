HashAlgorithm is moved Mythosia.Security.Cryptography namespace.
Please download Mythosia.Security.Cryptography to use it.

# CRC
Please see 
https://www.lammertbies.nl/comm/info/crc-calculation?crc=8005&method=hex
https://crccalc.com/

```c#
using Mythosia;
using Mythosia.Integrity;

var data = "123456789".ToASCIIArray();

// Example for CRC 8
var crc = data.CRC8();
var dataWithCRC = data.WithCRC8();

// Example for CRC 8 MAXIM
var crc = data.CRC8(CRC8Type.Maxim);
var dataWithCRC = data.WithCRC8(CRC8Type.Maxim);


// Example for CRC 16
var crc = data.CRC16();
var dataWithCRC = data.WithCRC16();

// Example for CRC 16 (modbus)
var crc = data.CRC16(CRC16Type.Modbus);
var dataWithCRC = data.WithCRC16(CRC16Type.Modbus);

// Example for CRC 16 CCITT (xModem)
var crc = data.CRC16(CRC16Type.CCITTxModem);
var dataWithCRC = data.WithCRC16(CRC16Type.CCITTxModem);

// Example for CRC 16 DNP
var crc = data.CRC16(CRC16Type.DNP);
var dataWithCRC = data.WithCRC16(CRC16Type.DNP);

// Example for CRC 32
var crc = data.CRC32();
var dataWithCRC = data.WithCRC32();
```


# Checksum
Please see
https://www.scadacore.com/tools/programming-calculators/online-checksum-calculator/

```c#
using Mythosia;
using Mythosia.Integrity;

var data = "123456789".ToASCIIArray();

// Example for Checksum8
var checksum = data.CheckSum8(CheckSum8Type.Xor);
var dataWithChecksum = data.WithCheckSum8(CheckSum8Type.Xor);

var checksum = data.CheckSum8(CheckSum8Type.Modulo256);
var dataWithChecksum = data.WithCheckSum8(CheckSum8Type.Modulo256);

var checksum = data.CheckSum8(CheckSum8Type.TwosComplement);
var dataWithChecksum = data.WithCheckSum8(CheckSum8Type.TwosComplement);

var checksum = data.CheckSum8(CheckSum8Type.NMEA);
var dataWithChecksum = data.WithCheckSum8(CheckSum8Type.NMEA);
```



# Application

if you want to use the polymorphism, you can use is as below.

```c#
using Mythosia;
using Mythosia.Integrity;

var content = "123456789".ToASCIIArray();

ErrorDetection errDetection = new Checksum8();
var checksum = errDetection.Compute(content);
var encodeData = errDetection.Encode(content);			// encodeData is content + checksum
var decodeData = errDetection.Decode(encodeData);		// decodeData is equal with content

errDetection = new Checksum8(Checksum8Type.Modulo256);	// change to the modulo256 checksum
checksum = errDetection.Compute();
var encodeData = errDetection.Encode(content);			// encodeData is content + checksum
var decodeData = errDetection.Decode(encodeData);		// decodeData is equal with content

errDetection = new CRC8();							// change to the CRC8
var crc = errDetection.Compute();
var encodeData = errDetection.Encode(content);			// encodeData is content + crc
var decodeData = errDetection.Decode(encodeData);		// decodeData is equal with content

errDetection = new CRC16();							// change to the CRC16
crc = errDetection.Compute();
var encodeData = errDetection.Encode(content);			// encodeData is content + crc
var decodeData = errDetection.Decode(encodeData);		// decodeData is equal with content

errDetection = new CRC32();							// change to the CRC32
crc = errDetection.Compute();
var encodeData = errDetection.Encode(content);			// encodeData is content + crc
var decodeData = errDetection.Decode(encodeData);		// decodeData is equal with content

```