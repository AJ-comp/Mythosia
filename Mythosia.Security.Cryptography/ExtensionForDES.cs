using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mythosia.Security.Cryptography
{
    public static class ExtensionForDES
    {
        /***************************************************************************/
        /// <summary>
        /// Encrypts the provided data using 3DES (TripleDES) algorithm with the specified key and initialization vector (IV).
        /// </summary>
        /// <param name="toEncryptData">The data to encrypt.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector (IV) used for encryption.</param>
        /// <param name="tripleDES">The TripleDES object used for encryption.</param>
        /// <returns>The encrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> Encrypt3DES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv, TripleDES tripleDES)
            => toEncryptData.EncryptSymmetric(key, iv, tripleDES);


        /***************************************************************************/
        /// <summary>
        /// Encrypts the specified data using TripleDES encryption algorithm with the given key, IV, cipher mode, and padding mode.
        /// </summary>
        /// <param name="toEncryptData">The data to encrypt.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector used for encryption.</param>
        /// <param name="cipherMode">The cipher mode used for encryption (default: CipherMode.CBC).</param>
        /// <param name="paddingMode">The padding mode used for encryption (default: PaddingMode.PKCS7).</param>
        /// <returns>The encrypted data.</returns>
        /// /***************************************************************************/
        public static IEnumerable<byte> Encrypt3DES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                            CipherMode cipherMode = CipherMode.CBC, PaddingMode paddingMode = PaddingMode.PKCS7)
        {
            using var tripleDES = TripleDES.Create();

            tripleDES.Key = key.ToArray();
            tripleDES.IV = iv.ToArray();
            tripleDES.Mode = cipherMode;
            tripleDES.Padding = paddingMode;

            return Encrypt3DES(toEncryptData, key, iv, tripleDES);
        }


        /***************************************************************************/
        /// <summary>
        /// Encrypts the specified data using TripleDES encryption algorithm with the given key, IV, and padding mode.
        /// The default cipher mode used is CipherMode.CBC.
        /// </summary>
        /// <param name="toEncryptData">The data to encrypt.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector used for encryption.</param>
        /// <param name="paddingMode">The padding mode used for encryption.</param>
        /// <returns>The encrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> Encrypt3DES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                            PaddingMode paddingMode)
            => Encrypt3DES(toEncryptData, key, iv, CipherMode.CBC, paddingMode);



        /***************************************************************************/
        /// <summary>
        /// Decrypts the provided encrypted data using 3DES (TripleDES) algorithm with the specified key and initialization vector (IV).
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for decryption.</param>
        /// <param name="tripleDES">The TripleDES object used for decryption.</param>
        /// <returns>The decrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> Decrypt3DES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv, TripleDES tripleDES)
            => encryptedData.DecryptSymmetric(key, iv, tripleDES);


        /***************************************************************************/
        /// <summary>
        /// Decrypts the specified encrypted data using TripleDES decryption algorithm with the given key, IV, cipher mode, and padding mode.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="iv">The initialization vector used for decryption.</param>
        /// <param name="cipherMode">The cipher mode used for decryption (default: CipherMode.CBC).</param>
        /// <param name="paddingMode">The padding mode used for decryption (default: PaddingMode.PKCS7).</param>
        /// <returns>The decrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> Decrypt3DES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                            CipherMode cipherMode = CipherMode.CBC, PaddingMode paddingMode = PaddingMode.PKCS7)
        {
            using var tripleDES = TripleDES.Create();

            tripleDES.Key = key.ToArray();
            tripleDES.IV = iv.ToArray();
            tripleDES.Mode = cipherMode;
            tripleDES.Padding = paddingMode;

            return Decrypt3DES(encryptedData, key, iv, tripleDES);
        }



        /***************************************************************************/
        /// <summary>
        /// Decrypts the specified encrypted data using TripleDES decryption algorithm with the given key, IV, and padding mode.
        /// The default cipher mode used is CipherMode.CBC.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="iv">The initialization vector used for decryption.</param>
        /// <param name="paddingMode">The padding mode used for decryption.</param>
        /// <returns>The decrypted data.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> Decrypt3DES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                            PaddingMode paddingMode)
            => Decrypt3DES(encryptedData, key, iv, CipherMode.CBC, paddingMode);






        /***************************************************************************/
        /// <summary>
        /// Encrypts the specified data using the Data Encryption Standard (DES) algorithm with the provided DES instance.
        /// </summary>
        /// <param name="toEncryptData">The data to be encrypted.</param>
        /// <param name="key">The encryption key used for DES encryption.</param>
        /// <param name="iv">The initialization vector (IV) used for DES encryption.</param>
        /// <param name="des">The DES instance used for encryption.</param>
        /// <returns>The encrypted data as a sequence of bytes.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> EncryptDES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv, DES des)
            => toEncryptData.EncryptSymmetric(key, iv, des);


        /***************************************************************************/
        /// <summary>
        /// Encrypts the specified data using the Data Encryption Standard (DES) algorithm.
        /// </summary>
        /// <param name="toEncryptData">The data to be encrypted.</param>
        /// <param name="key">The encryption key used for DES encryption.</param>
        /// <param name="iv">The initialization vector (IV) used for DES encryption.</param>
        /// <param name="cipherMode">The cipher mode used for DES encryption (default: CipherMode.CBC).</param>
        /// <param name="paddingMode">The padding mode used for DES encryption (default: PaddingMode.PKCS7).</param>
        /// <returns>The encrypted data as a sequence of bytes.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> EncryptDES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                        CipherMode cipherMode = CipherMode.CBC, PaddingMode paddingMode = PaddingMode.PKCS7)
        {
            using var des = DES.Create();

            des.Key = key.ToArray();
            des.IV = iv.ToArray();
            des.Mode = cipherMode;
            des.Padding = paddingMode;

            return EncryptDES(toEncryptData, key, iv, des);
        }


        /***************************************************************************/
        /// <summary>
        /// Encrypts the specified data using the Data Encryption Standard (DES) algorithm with the specified padding mode.
        /// </summary>
        /// <param name="toEncryptData">The data to be encrypted.</param>
        /// <param name="key">The encryption key used for DES encryption.</param>
        /// <param name="iv">The initialization vector (IV) used for DES encryption.</param>
        /// <param name="paddingMode">The padding mode used for DES encryption.</param>
        /// <returns>The encrypted data as a sequence of bytes.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> EncryptDES(this IEnumerable<byte> toEncryptData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                            PaddingMode paddingMode)
            => EncryptDES(toEncryptData, key, iv, CipherMode.CBC, paddingMode);


        /***************************************************************************/
        /// <summary>
        /// Decrypts the specified encrypted data using the Data Encryption Standard (DES) algorithm with the provided DES instance.
        /// </summary>
        /// <param name="encryptedData">The data to be decrypted.</param>
        /// <param name="key">The encryption key used for DES decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for DES decryption.</param>
        /// <param name="des">The DES instance used for decryption.</param>
        /// <returns>The decrypted data as a sequence of bytes.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> DecryptDES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv, DES des)
            => encryptedData.DecryptSymmetric(key, iv, des);


        /***************************************************************************/
        /// <summary>
        /// Decrypts the specified encrypted data using the Data Encryption Standard (DES) algorithm with the provided key, IV, cipher mode, and padding mode.
        /// </summary>
        /// <param name="encryptedData">The data to be decrypted.</param>
        /// <param name="key">The encryption key used for DES decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for DES decryption.</param>
        /// <param name="cipherMode">The cipher mode used for DES decryption (default: CipherMode.CBC).</param>
        /// <param name="paddingMode">The padding mode used for DES decryption (default: PaddingMode.PKCS7).</param>
        /// <returns>The decrypted data as a sequence of bytes.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> DecryptDES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                        CipherMode cipherMode = CipherMode.CBC, PaddingMode paddingMode = PaddingMode.PKCS7)
        {
            using var des = DES.Create();

            des.Key = key.ToArray();
            des.IV = iv.ToArray();
            des.Mode = cipherMode;
            des.Padding = paddingMode;

            return DecryptDES(encryptedData, key, iv, des);
        }


        /***************************************************************************/
        /// <summary>
        /// Decrypts the specified encrypted data using the Data Encryption Standard (DES) algorithm with the provided key, IV, 
        /// and padding mode, assuming the default cipher mode of CBC.
        /// </summary>
        /// <param name="encryptedData">The data to be decrypted.</param>
        /// <param name="key">The encryption key used for DES decryption.</param>
        /// <param name="iv">The initialization vector (IV) used for DES decryption.</param>
        /// <param name="paddingMode">The padding mode used for DES decryption.</param>
        /// <returns>The decrypted data as a sequence of bytes.</returns>
        /***************************************************************************/
        public static IEnumerable<byte> DecryptDES(this IEnumerable<byte> encryptedData, IEnumerable<byte> key, IEnumerable<byte> iv,
                                                                        PaddingMode paddingMode)
            => DecryptDES(encryptedData, key, iv, CipherMode.CBC, paddingMode);
    }
}
