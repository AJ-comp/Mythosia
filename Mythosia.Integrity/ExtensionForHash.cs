using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Mythosia.Integrity
{
    public enum IVHashType
    {
        SHA1,
        SHA256,
        SHA384,
        SHA512,
        MD5,
    }

    public static class ExtensionForHash
    {
        /// <summary>
        /// Computes the hash of a byte sequence using the specified Integrity Verification (IV) hash algorithm type.
        /// </summary>
        /// <param name="data">The byte sequence to compute the IV hash for.</param>
        /// <param name="type">The IV hash algorithm type to use (e.g., SHA1, SHA256, SHA384, SHA512, MD5).</param>
        /// <returns>The computed IV hash as a byte sequence.</returns>
        /// <exception cref="NotImplementedException">Thrown if the specified IV hash algorithm type is not implemented.</exception>
        public static IEnumerable<byte> IVHashCode(this IEnumerable<byte> data, IVHashType type = IVHashType.SHA1)
        {
            if (type == IVHashType.SHA1) return data.GetSHA1();
            else if (type == IVHashType.SHA256) return data.GetSHA256();
            else if (type == IVHashType.SHA384) return data.GetSHA384();
            else if (type == IVHashType.SHA512) return data.GetSHA512();
            else if (type == IVHashType.MD5) return data.GetMD5();
            else throw new NotImplementedException();
        }


        /// <summary>
        /// Computes the hash of a byte sequence and appends it to the original byte sequence as an Integrity Verification (IV) code.
        /// </summary>
        /// <param name="data">The original byte sequence.</param>
        /// <param name="type">The IV hash algorithm type to use (e.g., SHA1, SHA256, SHA384, SHA512, MD5).</param>
        /// <returns>A new byte sequence with the original data followed by the computed IV hash.</returns>
        /// <exception cref="NotImplementedException">Thrown if the specified IV hash algorithm type is not implemented.</exception>
        public static IEnumerable<byte> WithIVHashCode(this IEnumerable<byte> data, IVHashType type = IVHashType.SHA1)
        {
            List<byte> result = new List<byte>();
            result.AddRange(data);

            if (type == IVHashType.SHA1) result.AddRange(data.GetSHA1());
            else if (type == IVHashType.SHA256) result.AddRange(data.GetSHA256());
            else if (type == IVHashType.SHA384) result.AddRange(data.GetSHA384());
            else if (type == IVHashType.SHA512) result.AddRange(data.GetSHA512());
            else if (type == IVHashType.MD5) result.AddRange(data.GetMD5());
            else throw new NotImplementedException();

            return result;
        }


        private static IEnumerable<byte> GetSHA1(this IEnumerable<byte> data)
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(data.ToArray());
        }

        private static IEnumerable<byte> GetSHA256(this IEnumerable<byte> data)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(data.ToArray());
        }

        public static IEnumerable<byte> GetSHA384(this IEnumerable<byte> data)
        {
            using var sha384 = SHA384.Create();
            return sha384.ComputeHash(data.ToArray());
        }

        public static IEnumerable<byte> GetSHA512(this IEnumerable<byte> data)
        {
            using var sha512 = SHA512.Create();
            return sha512.ComputeHash(data.ToArray());
        }


        public static IEnumerable<byte> GetMD5(this IEnumerable<byte> data)
        {
            using var md5 = MD5.Create();
            return md5.ComputeHash(data.ToArray());
        }





    }
}
