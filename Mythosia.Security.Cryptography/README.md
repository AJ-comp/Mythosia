# Seed
Please see https://seed.kisa.or.kr/kisa/algorithm/EgovSeedInfo.do

The above site includes an English manual.

The IV value is {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}.


1. Example
```c#
var key = KeyGenerator.GenerateSEEDKey();

var dataEncrypted = "test".ToUTF8Array().EncryptSEED(key);	// encrypt with SEED the "test"
var dataDecrypted = dataEncrypted.DecryptSEED(key);		// decrypt with SEED

var stringDecrypted = dataDecrypted.ToUTF8String();		// if you want to convert to string, here the stringDecrypted will be "test"
```


# AES

1. Example
```c#
using Mythosia;
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
using Mythosia;
using Mythosia.Security.Cryptography;

var key = KeyGenerator.Generate3DESKey();
var iv = KeyGenerator.Generate3DESIV();

var dataEncrypted = "test123456".ToUTF8Array().Encrypt3DES(key, iv);		// encrypt with 3DES the "test123456" string
var dataDecrypted = dataEncrypted.Decrypt3DES(key, iv);		// decrypt with 3DES

var stringDecrypted = dataDecrypted.ToUTF8String();		// if you want to convert to string, here the stringDecrypted will be "test123456"
```


# DES

1. Example
```c#
using Mythosia;
using Mythosia.Security.Cryptography;

var key = KeyGenerator.GenerateDESKey();
var iv = KeyGenerator.GenerateDESIV();

var dataEncrypted = "test123456".ToUTF8Array().EncryptDES(key, iv);		// encrypt with DES the "test123456" string
var dataDecrypted = dataEncrypted.DecryptDES(key, iv);		// decrypt with DES

var stringDecrypted = dataDecrypted.ToUTF8String();		// if you want to convert to string, here the stringDecrypted will be "test123456"
```


# SHA, MD2, MD4, MD5
Please see https://emn178.github.io/online-tools/sha1.html

```c#
using Mythosia;
using Mythosia.Security.Cryptography;

// Example for SHA1
var sha = data.IVHashCode();
var dataWithSha = data.WithIVHashCode();

// Example for SHA256
var sha256 = data.IVHashCode(IVHashType.SHA256);
var dataWithSha256 = data.WithIVHashCode(IVHashType.SHA256);

// Example for SHA384
var sha384 = data.IVHashCode(IVHashType.SHA384);
var dataWithSha384 = data.WithIVHashCode(IVHashType.SHA384);

// Example for SHA512
var sha512 = data.IVHashCode(IVHashType.SHA512);
var dataWithSha512 = data.WithIVHashCode(IVHashType.SHA512);

// Example for MD2
var md2 = data.IVHashCode(IVHashType.MD2);
var dataWithMD2 = data.WithIVHashCode(IVHashType.MD2);

// Example for MD4
var md4 = data.IVHashCode(IVHashType.MD4);
var dataWithMD4 = data.WithIVHashCode(IVHashType.MD4);

// Example for MD5
var md5 = data.IVHashCode(IVHashType.MD5);
var dataWithMD5 = data.WithIVHashCode(IVHashType.MD5);
```



# Application

1. Using SymmetricAlgorithm with polymorphism
```c#
using Mythosia;
using Mythosia.Security.Cryptography;

string contentToEncrypt = "test123456";

// select the one from below
// SEED algorithm is not supported yet but will be supported soon
SymmetricAlgorithm symmetricAlgorithm = Aes.Create();
SymmetricAlgorithm symmetricAlgorithm = TripleDES.Create();
SymmetricAlgorithm symmetricAlgorithm = DES.Create();

// en-decryption with selected algorithm
var encrypted = symmetricAlgorithm.Encrypt(contentToEncrypt.ToUTF8Array(), key, iv);
var decrypted = symmetricAlgorithm.Decrypt(encrypted, key, iv).ToUTF8String();
```

2. Using HashAlgorithm with polymorphism
```c#
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

3. Using with CRC
```c#
// if you have a Mythosia.Integrity library, you can do as below.

using Mythosia;
using Mythosia.Integrity;
using Mythosia.Security.Cryptography;

var contentToEncrypt = "test123456".ToUTF8Array();
var key = KeyGenerator.GenerateAES128Key();
var iv = KeyGenerator.GenerateAES128IV();

var dataEncrypted = contentToEncrypt.WithCheckSum8().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" + checksum8
var dataEncrypted = contentToEncrypt.WithCRC8().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" + crc8
var dataEncrypted = contentToEncrypt.WithCRC16().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" + crc16
var dataEncrypted = contentToEncrypt.WithCRC32().EncryptAES(key, iv);		// encrypt with AES128 the "test123456" + crc32
```
