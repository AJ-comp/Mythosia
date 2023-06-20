using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mythosia.Security.Cryptography
{
    public static class ExtensionForAES
    {
        /***************************************************************************/
        /// <summary>
        /// Encrypts the provided data using AES algorithm with the specified key and initialization vector (IV).
        /// </summary>
        /// <param name="toEncryptData">The data to encrypt.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector (IV) used for encryption.</param>
        /// <param name="aes">The Aes object used for encryption.</param>
        /// <returns>The encrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> EncryptAES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv, Aes aes)
        {
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            var inputBytes = toEncryptData.ToArray();
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
        /// Encrypts the provided data using AES algorithm with the specified key and initialization vector (IV).
        /// </summary>
        /// <param name="toEncryptData">The data to encrypt.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector (IV) used for encryption.</param>
        /// <param name="cipherMode">The cipher mode used for encryption.</param>
        /// <param name="paddingMode">The padding mode used for encryption.</param>
        /// <returns>The encrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> EncryptAES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv, 
                                                                        CipherMode cipherMode = CipherMode.CBC, PaddingMode paddingMode = PaddingMode.PKCS7)
        {
            using var aes = Aes.Create();

            aes.Key = key.ToArray();
            aes.IV = iv.ToArray();
            aes.Mode = cipherMode;
            aes.Padding = paddingMode;

            return EncryptAES(toEncryptData, key, iv, aes);
        }


        /***************************************************************************/
        /// <summary>
        /// Encrypts the provided data using AES algorithm with the specified key, initialization vector (IV), and padding mode.
        /// </summary>
        /// <param name="toEncryptData">The data to encrypt.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector (IV) used for encryption.</param>
        /// <param name="paddingMode">The padding mode used for encryption.</param>
        /// <returns>The encrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> EncryptAES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv, 
                                                                        PaddingMode paddingMode)
        {
            return EncryptAES(toEncryptData, key, iv, CipherMode.CBC, paddingMode);
        }


        /***************************************************************************/
        /// <summary>
        /// Decrypts the provided encrypted data using the specified AES instance.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for decryption.</param>
        /// <param name="aes">The AES instance initialized with the appropriate key and IV.</param>
        /// <returns>The decrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> DecryptAES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv, Aes aes)
        {
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            var cipherText = encryptedData.ToArray();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            return decryptedBytes;

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
        /// Decrypts the provided encrypted data using AES algorithm with the specified key, initialization vector (IV), cipher mode, and padding mode.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for decryption.</param>
        /// <param name="cipherMode">The cipher mode used for decryption (default: CBC).</param>
        /// <param name="paddingMode">The padding mode used for decryption (default: PKCS7).</param>
        /// <returns>The decrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> DecryptAES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                        CipherMode cipherMode = CipherMode.CBC, PaddingMode paddingMode = PaddingMode.PKCS7)
        {
            using var aes = Aes.Create();

            aes.Key = key.ToArray();
            aes.IV = iv.ToArray();
            aes.Mode = cipherMode;
            aes.Padding = paddingMode;

            return DecryptAES(encryptedData, key, iv, aes);
        }


        /***************************************************************************/
        /// <summary>
        /// Decrypts the provided encrypted data using the specified AES parameters.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for decryption.</param>
        /// <param name="paddingMode">The padding mode used during decryption.</param>
        /// <returns>The decrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> DecryptAES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                        PaddingMode paddingMode)
        {
            return DecryptAES(encryptedData, key, iv, CipherMode.CBC, paddingMode);
        }
    }
}
