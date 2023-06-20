using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Mythosia.Security.Cryptography
{
    public class KeyGenerator
    {
        private static IEnumerable<byte> GenerateKey(int keySize)
        {
            byte[] key = new byte[keySize / 8];

            using var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(key);

            return key;
        }

        /******************************************************/
        /// <summary>
        /// Generates a random key for AES-128 encryption.
        /// </summary>
        /// <returns>The generated AES-128 key.</returns>
        /******************************************************/
        public static IEnumerable<byte> GenerateAES128Key() => GenerateKey(128);


        /******************************************************/
        /// <summary>
        /// Generates a random key for AES-192 encryption.
        /// </summary>
        /// <returns>The generated AES-192 key.</returns>
        /******************************************************/
        public static IEnumerable<byte> GenerateAES192Key() => GenerateKey(192);


        /******************************************************/
        /// <summary>
        /// Generates a random key for AES-256 encryption.
        /// </summary>
        /// <returns>The generated AES-256 key.</returns>
        /******************************************************/
        public static IEnumerable<byte> GenerateAES256Key() => GenerateKey(256);


        /******************************************************/
        /// <summary>
        /// Generates a random IV (Initialization Vector) for AES-128 encryption.
        /// </summary>
        /// <returns>The generated AES-128 IV.</returns>
        /******************************************************/
        public static IEnumerable<byte> GenerateAES128IV() => GenerateKey(128);


        /******************************************************/
        /// <summary>
        /// Generates a random IV (Initialization Vector) for AES-192 encryption.
        /// </summary>
        /// <returns>The generated AES-192 IV.</returns>
        /******************************************************/
        public static IEnumerable<byte> GenerateAES192IV() => GenerateKey(192);


        /******************************************************/
        /// <summary>
        /// Generates a random IV (Initialization Vector) for AES-256 encryption.
        /// </summary>
        /// <returns>The generated AES-256 IV.</returns>
        /******************************************************/
        public static IEnumerable<byte> GenerateAES256IV() => GenerateKey(256);


        /******************************************************/
        /// <summary>
        /// Generates a random key for 3DES (TripleDES) encryption.
        /// </summary>
        /// <returns>The generated 3DES key.</returns>
        /******************************************************/
        public static IEnumerable<byte> Generate3DESKey() => GenerateKey(192);


        /******************************************************/
        /// <summary>
        /// Generates a random IV (Initialization Vector) for 3DES (TripleDES) encryption.
        /// </summary>
        /// <returns>The generated 3DES IV.</returns>
        /******************************************************/
        public static IEnumerable<byte> Generate3DESIV() => GenerateKey(64);
    }
}
