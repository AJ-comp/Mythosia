# CRC
Please see https://www.lammertbies.nl/comm/info/crc-calculation?crc=8005&method=hex
Please see https://crccalc.com/


```c#
using Mythosia.Integrity;

var data = Encoding.ASCII.GetBytes("123456789");

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