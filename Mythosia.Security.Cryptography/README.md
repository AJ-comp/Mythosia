# Seed
Please see https://seed.kisa.or.kr/kisa/algorithm/EgovSeedInfo.do

The above site includes an English manual.

The IV value is {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}.


1. Example
```c#
// You can set this value to whatever you want. (It must be a 16 byte array)
byte[] SeedKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5 };

var dataEncrypted = "test".ToUTF8Array().EncryptSEED(SeedKey);	// encrypt with SEED the "test"
var dataDecrypted = dataEncrypted.DecryptSEED(SeedKey);		// decrypt with SEED

var stringDecrypted = dataDecrypted.ToUTF8String();		// if you want to convert to string, here the stringDecrypted will be "test"
```


# AES

1. Example
```c#
using Mythosia.Security.Cryptography;

var key = KeyGenerator.GenerateAES128Key();
var iv = KeyGenerator.GenerateAES128IV();

var dataEncrypted = "test123456".ToUTF8Array().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" string  (if you pass AES256 key then run as AES256)
var dataDecrypted = dataEncrypted.DecryptAES(key, iv);		// decrypt with AES128

var stringDecrypted = dataDecrypted.ToUTF8String();		// if you want to convert to string, here the stringDecrypted will be "test123456"
```


# 3DES

1. Example
```c#
using Mythosia.Security.Cryptography;

var key = KeyGenerator.Generate3DESKey();
var iv = KeyGenerator.Generate3DESIV();

var dataEncrypted = "test123456".ToUTF8Array().Encrypt3DES(key, iv);		// encrypt with 3DES the "test123456" string
var dataDecrypted = dataEncrypted.Decrypt3DES(key, iv);		// decrypt with 3DES

var stringDecrypted = dataDecrypted.ToUTF8String();		// if you want to convert to string, here the stringDecrypted will be "test123456"
```


# Application

Application example
```c#
// if you have a Mythosia.Integrity library, you can do as below.

using Mythosia.Integrity;
using Mythosia.Security.Cryptography;

var key = KeyGenerator.GenerateAES128Key();
var iv = KeyGenerator.GenerateAES128IV();

var dataEncrypted = "test123456".ToUTF8Array().WithCheckSum8().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" + checksum8
var dataEncrypted = "test123456".ToUTF8Array().WithCRC8().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" + crc8
var dataEncrypted = "test123456".ToUTF8Array().WithCRC16().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" + crc16
var dataEncrypted = "test123456".ToUTF8Array().WithCRC32().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" + crc32
```
