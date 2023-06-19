# Seed
Please see https://seed.kisa.or.kr/kisa/algorithm/EgovSeedInfo.do

The above site includes an English manual.

The IV value is {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}.


1. Encryption example
```c#
// You can set this value to whatever you want. (It must be a 16 byte array)
byte[] SeedKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5 };

var dataEncrypted = "test".ToUTF8Array().EncryptWithSeed(SeedKey);
```

2. Decryption example
```c#
// The SeedKey must same as the SeedKey used when encryption.
byte[] SeedKey = new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5 };

var dataDecrypted = dataEncrypted.DecryptWithSeed(SeedKey);
```


# AES

1. Encryption example
```c#
var dataEncrypted = "test123456".ToUTF8Array().EncryptAES("0123456789ABCDEF", "ABCDEF0123456789");
```

2. Decryption example
```c#
var dataDecrypted = dataEncrypted.DecryptAES("0123456789ABCDEF", "ABCDEF0123456789").ToUTF8String();
```


