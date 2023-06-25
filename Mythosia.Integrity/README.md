HashAlgorithm is moved Mythosia.Security.Cryptography namespace.
Please download Mythosia.Security.Cryptography to use it.

# CRC
Please see https://www.lammertbies.nl/comm/info/crc-calculation?crc=8005&method=hex
Please see https://crccalc.com/

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

1. Using with polymorphism
```c#
// if you want to use polymorphism, you can do as below.

using Mythosia;
using Mythosia.Integrity;

string contentToEncrypt = "test123456";

// select the one from below
// MD4 also support to use as below.
HashAlgorithm hashAlgorithm = SHA1.Create();
HashAlgorithm hashAlgorithm = new MD2();		// only use new MD2(); don't use MD2.Create();
HashAlgorithm hashAlgorithm = new MD4();		// only use new MD4(); don't use MD4.Create();
HashAlgorithm hashAlgorithm = MD5.Create();

// compute hash value using a selected hash algorithm for contentToEncrypt.
hashAlgorithm.ComputeHash(contentToEncrypt.ToUTF8Array());
```