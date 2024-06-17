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
        private static IEnumerable<byte> Encrypt(this SymmetricAlgorithm symmertric, IEnumerable<byte> toEncryptData)
        {
            ICryptoTransform encryptor = symmertric.CreateEncryptor(symmertric.Key, symmertric.IV);

            var inputBytes = toEncryptData.AsOrToArray();
            return encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            /*
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(toEncryptData.ToArray(), 0, toEncryptData.Count());
                cs.FlushFinalBlock();
                encrypted.AddRange(ms.ToArray());
            }

            return encrypted;
             */
        }


        /***************************************************************************/
        /// <summary>
        /// Encrypts the specified data using the provided symmetric algorithm, key, and IV.
        /// </summary>
        /// <param name="symmertric">The symmetric algorithm to use for encryption.</param>
        /// <param name="toEncryptData">The data to encrypt.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector used for encryption.</param>
        /// <returns>The encrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> Encrypt(this SymmetricAlgorithm symmertric, IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv)
        {
            symmertric.Key = key.AsOrToArray();
            symmertric.IV = iv.AsOrToArray();

            return symmertric.Encrypt(toEncryptData);
        }




        private static IEnumerable<byte> Decrypt(this SymmetricAlgorithm symmertric, IEnumerable<byte> encryptedData)
        {
            ICryptoTransform decryptor = symmertric.CreateDecryptor(symmertric.Key, symmertric.IV);

            var cipherText = encryptedData.AsOrToArray();
            var decrypted = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            return decrypted;

            /*
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (var ms = new MemoryStream(encryptedData.ToArray()))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                byte[] decryptedBytes = new byte[encryptedData.Count()];
                int decryptedByteCount = cs.Read(decryptedBytes, 0, decryptedBytes.Length);
                decrypted.AddRange(decryptedBytes.Take(decryptedByteCount));
            }

            return decrypted;
            */
        }


        /***************************************************************************/
        /// <summary>
        /// Decrypts the encrypted data using the specified symmetric algorithm, key, and initialization vector (IV).
        /// </summary>
        /// <param name="symmetric">The symmetric algorithm used for decryption.</param>
        /// <param name="encryptedData">The encrypted data to be decrypted.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for decryption.</param>
        /// <returns>The decrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> Decrypt(this SymmetricAlgorithm symmertric, IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv)
        {
            symmertric.Key = key.ToArray();
            symmertric.IV = iv.ToArray();

            return symmertric.Decrypt(encryptedData);
        }


        /***************************************************************************/
        /// <summary>
        /// Encrypts the specified data using the provided symmetric algorithm (such as AES) with the given key and IV.
        /// </summary>
        /// <param name="toEncryptData">The data to be encrypted.</param>
        /// <param name="key">The encryption key used for encryption.</param>
        /// <param name="iv">The initialization vector (IV) used for encryption.</param>
        /// <param name="symmetric">The symmetric algorithm instance to perform encryption.</param>
        /// <returns>The encrypted data as a sequence of bytes.</returns>
        /***************************************************************************/
        internal static IEnumerable<byte> EncryptSymmetric(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv, SymmetricAlgorithm symmertric)
            => symmertric.Encrypt(toEncryptData, key, iv);


        /***************************************************************************/
        /// <summary>
        /// Decrypts the specified encrypted data using the provided symmetric algorithm (such as AES) with the given key and IV.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to be decrypted.</param>
        /// <param name="key">The encryption key used for decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for decryption.</param>
        /// <param name="symmetric">The symmetric algorithm instance to perform decryption.</param>
        /// <returns>The decrypted data as a sequence of bytes.</returns>
        /***************************************************************************/
        internal static IEnumerable<byte> DecryptSymmetric(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv, SymmetricAlgorithm symmertric)
            => symmertric.Decrypt(encryptedData, key, iv);


        public static IEnumerable<byte> EncryptSEED(this IEnumerable<byte> data, IEnumerable<byte> seedKey, bool cbcPad = true)
            => SEED.Encrypt(data.AsOrToArray(), seedKey.AsOrToArray(), cbcPad);

        public static IEnumerable<byte> DecryptSEED(this IEnumerable<byte> data, IEnumerable<byte> seedKey, bool cbcPad = true)
            => SEED.Decrypt(data.AsOrToArray(), seedKey.AsOrToArray(), cbcPad);
    }
}
