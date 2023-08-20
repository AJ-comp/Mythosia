using Mythosia;
using Mythosia.Collections;
using Mythosia.Integrity.CRC;
using Mythosia.Security.Cryptography;
using Mythosia.Test;


//AttributeTest attributeTest = new AttributeTest();
//await attributeTest.StartTest();

CircularQueue<byte> q = new CircularQueue<byte>(3);

q.Enqueue(0x0a);
q.Enqueue(0x30);
q.Enqueue(0x1f);
q.Enqueue(0x22);
q.Enqueue(0x25);
var data = q.WithCRC16();

StreamTest streamTest = new();
streamTest.StartTest();

/*
SymmetricAlgorithm symm = new SEED();
var key = KeyGenerator.GenerateSEEDKey();
var iv = KeyGenerator.GenerateSEEDIV();
var enc = symm.Encrypt("test12345".ToUTF8Array(), key, iv);
symm.Decrypt(enc, key, iv);
*/

/*
var shake = new Shake128();
var outPut = shake.ComputeHash("test12345".ToUTF8Array(), 128);
*/

CryptographyTest cryptographyTest = new();
cryptographyTest.StartTest("test12345");