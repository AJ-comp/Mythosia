using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mythosia;

namespace Mythosia.Security.Cryptography
{
    public static class ExtensionForCrypto
    {
        public static IEnumerable<byte> EncryptSEED(this IEnumerable<byte> data, 
                                                                                IEnumerable<byte> seedKey, 
                                                                                bool cbcPad = true)
            => SEED.Encrypt(data.ToArray(), seedKey.ToArray(), cbcPad);

        public static IEnumerable<byte> DecryptSEED(this IEnumerable<byte> data, 
                                                                                IEnumerable<byte> seedKey,
                                                                                bool cbcPad = true)
            => SEED.Decrypt(data.ToArray(), seedKey.ToArray(), cbcPad);


        public static IEnumerable<byte> EncryptAES(this IEnumerable<byte> data, string key, string iv)
        {
            List<byte> encrypted = new List<byte>();

            using Aes aes = Aes.Create();
            aes.Key = key.ToUTF8Array();
            aes.IV = iv.ToUTF8Array();

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data.ToArray(), 0, data.Count());
                cs.FlushFinalBlock();
                encrypted.AddRange(ms.ToArray());
            }

            return encrypted;
        }


        public static IEnumerable<byte> DecryptAES(this IEnumerable<byte> encryptedData, string key, string iv)
        {
            List<byte> decrypted = new List<byte>();

            using Aes aes = Aes.Create();
            aes.Key = key.ToUTF8Array();
            aes.IV = iv.ToUTF8Array();

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (var ms = new System.IO.MemoryStream(encryptedData.ToArray()))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                byte[] decryptedBytes = new byte[encryptedData.Count()];
                int decryptedByteCount = cs.Read(decryptedBytes, 0, decryptedBytes.Length);
                decrypted.AddRange(decryptedBytes.Take(decryptedByteCount));
            }

            return decrypted;
        }
    }
}
