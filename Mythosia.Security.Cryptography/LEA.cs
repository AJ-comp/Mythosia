using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mythosia.Security.Cryptography
{
    /// <summary>
    /// Lightweight Encryption Algorithm
    /// </summary>
    /// <remarks>https://ablog.jc-lab.net/69</remarks>
    public class LEA : SymmetricAlgorithm
    {
        public enum BlockSize
        {
            Block128 = 24,
            Block192 = 28,
            Block256 = 32
        }

        public IEnumerable<uint> RoundyKey => _roundKey;


        public LEA(BlockSize blockSize = BlockSize.Block128)
        {
            _blockSize = blockSize;
        }

        public uint Nr => (uint)_blockSize;
        private BlockSize _blockSize = BlockSize.Block128;
        private double twoPower32 => Math.Pow(2, 32);

        private static readonly uint[] _keyDelta =
        {
            0xc3efe9db,
            0x44626b02,
            0x79e27c8a,
            0x78df30ec,
            0x715ea49e,
            0xc785da0a,
            0xe04ef22a,
            0xe5c40957
        };

        private uint[] _roundKey = new uint[192];


        uint ROL(uint places, uint value)
        {
            uint valueLeftShiftPlace = value << (int)places;
            uint valueRightShiftPlace = value >> (int)(32 - places);

            return (valueLeftShiftPlace | valueRightShiftPlace);
        }

        uint ROR(uint places, uint value)
        {
            uint valueLeftShiftPlace = value >> (int)places;
            uint valueRightShiftPlace = value << (int)(32 - places);

            return (uint)(valueLeftShiftPlace | valueRightShiftPlace);
        }

        uint Plus(uint a, uint b)
        {
            return (uint)((a + b) % twoPower32);
        }

        uint Minus(uint a, uint b)
        {
            return (uint)((a - b) % twoPower32);
        }


        void CalculateRoundKey128(byte[] data)
        {
            var T = data.ToUIntArray();

            for (uint i = 0; i < 24; i++)
            {
                var debug2 = ROL((i + 1), _keyDelta[i % 4]);

                T[0] = ROL(1, Plus(T[0], ROL(i, _keyDelta[i % 4])));
                T[1] = ROL(3, Plus(T[1], ROL((i + 1), _keyDelta[i % 4])));
                T[2] = ROL(6, Plus(T[2], ROL((i + 2), _keyDelta[i % 4])));
                T[3] = ROL(11, Plus(T[3], ROL((i + 3), _keyDelta[i % 4])));

                _roundKey[i * 6 + 0] = T[0];
                _roundKey[i * 6 + 1] = T[1];
                _roundKey[i * 6 + 2] = T[2];
                _roundKey[i * 6 + 3] = T[1];
                _roundKey[i * 6 + 4] = T[3];
                _roundKey[i * 6 + 5] = T[1];
            }
        }

        void CalculateRoundKey192(byte[] content)
        {
            uint[] T = new uint[6];

            Buffer.BlockCopy(content, 0, T, 0, 24);

            for (uint i = 0; i < 28; i++)
            {
                T[0] = ROL(1, Plus(T[0], ROL(i, _keyDelta[i % 6])));
                T[1] = ROL(3, Plus(T[1], ROL((i + 1), _keyDelta[i % 6])));
                T[2] = ROL(6, Plus(T[2], ROL(i + 2, _keyDelta[i % 6])));
                T[3] = ROL(11, Plus(T[3], ROL(i + 3, _keyDelta[i % 6])));
                T[4] = ROL(13, Plus(T[4], ROL(i + 4, _keyDelta[i % 6])));
                T[5] = ROL(17, Plus(T[5], ROL(i + 5, _keyDelta[i % 6])));

                _roundKey[i * 6 + 0] = T[0];
                _roundKey[i * 6 + 1] = T[1];
                _roundKey[i * 6 + 2] = T[2];
                _roundKey[i * 6 + 3] = T[3];
                _roundKey[i * 6 + 4] = T[4];
                _roundKey[i * 6 + 5] = T[5];
            }
        }


        void CalculateRoundKey256(byte[] content)
        {
            uint[] T = new uint[8];

            Buffer.BlockCopy(content, 0, T, 0, 32);

            for (uint i = 0; i < 32; i++)
            {
                T[(6 * i + 0) % 8] = ROL(1, Plus(T[0], ROL(i, _keyDelta[i % 8])));
                T[(6 * i + 1) % 8] = ROL(3, Plus(T[1], ROL(i + 1, _keyDelta[i % 8])));
                T[(6 * i + 2) % 8] = ROL(6, Plus(T[2], ROL(i + 2, _keyDelta[i % 8])));
                T[(6 * i + 3) % 8] = ROL(11, Plus(T[3], ROL(i + 3, _keyDelta[i % 8])));
                T[(6 * i + 4) % 8] = ROL(13, Plus(T[4], ROL(i + 4, _keyDelta[i % 8])));
                T[(6 * i + 5) % 8] = ROL(17, Plus(T[5], ROL(i + 5, _keyDelta[i % 8])));

                _roundKey[i * 6 + 0] = T[(6 * i + 0) % 8];
                _roundKey[i * 6 + 1] = T[(6 * i + 1) % 8];
                _roundKey[i * 6 + 2] = T[(6 * i + 2) % 8];
                _roundKey[i * 6 + 3] = T[(6 * i + 3) % 8];
                _roundKey[i * 6 + 4] = T[(6 * i + 4) % 8];
                _roundKey[i * 6 + 5] = T[(6 * i + 5) % 8];
            }
        }


        void RoundEncrypt(uint[] Xout, uint[] Xin, uint[] RKe)
        {
            Xout[0] = ROL(9, Plus(Xin[0] ^ RKe[0], Xin[1] ^ RKe[1]));
            Xout[1] = ROR(5, Plus(Xin[1] ^ RKe[2], Xin[2] ^ RKe[3]));
            Xout[2] = ROR(3, Plus(Xin[2] ^ RKe[4], Xin[3] ^ RKe[5]));
            Xout[3] = Xin[0];
        }

        void RoundDecrypt(uint[] Xout, uint[] Xin, uint[] RKd)
        {
            Xout[0] = Xin[3];
            Xout[1] = Minus(ROR(9, Xin[0]), Xout[0] ^ RKd[0]) ^ RKd[1];
            Xout[2] = Minus(ROL(5, Xin[1]), Xout[1] ^ RKd[2]) ^ RKd[3];
            Xout[3] = Minus(ROL(3, Xin[2]), Xout[2] ^ RKd[4]) ^ RKd[5];
        }


        private void CalculateRoundKey(byte[] key)
        {
            if (_blockSize == BlockSize.Block128) CalculateRoundKey128(key);
            else if (_blockSize == BlockSize.Block192) CalculateRoundKey192(key);
            else if (_blockSize == BlockSize.Block256) CalculateRoundKey256(key);
            else throw new InvalidOperationException();
        }


        private IEnumerable<byte[]> SplitInto16ByteChunks(byte[] data)
        {
            int fullChunks = data.Length / 16;
            int lastChunkSize = data.Length % 16;

            for (int i = 0; i < fullChunks; i++)
            {
                byte[] chunk = new byte[16];
                Array.Copy(data, i * 16, chunk, 0, 16);
                yield return chunk;
            }

            if (lastChunkSize > 0)
            {
                byte[] lastChunk = new byte[lastChunkSize];
                Array.Copy(data, fullChunks * 16, lastChunk, 0, lastChunkSize);
                yield return lastChunk;
            }
        }


        private byte[] ApplyPKCS7Padding(byte[] data, int blockSize = 16)
        {
            if (blockSize <= 0 || blockSize > 256)
                throw new ArgumentOutOfRangeException("blockSize must be between 1 and 256.");

            int paddingSize = blockSize - (data.Length % blockSize);

            // // If the original data is a multiple of the block size, set the padding size to blockSize because one additional block needs to be pasted
            if (paddingSize == 0) paddingSize = blockSize;

            byte[] paddedData = new byte[data.Length + paddingSize];
            Array.Copy(data, 0, paddedData, 0, data.Length);

            for (int i = data.Length; i < paddedData.Length; i++)
            {
                paddedData[i] = (byte)paddingSize;
            }

            return paddedData;
        }


        public static IEnumerable<byte[]> AddPkcs7Padding(IEnumerable<byte[]> blocks)
        {
            List<byte[]> result = new List<byte[]>();
            result.AddRange(blocks);

            byte[] lastBlock = blocks.Last();

            if (lastBlock.Length > 16) throw new ArgumentException("Last block size exceeds 16 bytes.");

            if (lastBlock.Length == 16)
            {
                var newBlock = new byte[16];
                for (int i = 0; i < 16; i++) newBlock[i] = 16;

                result.Add(newBlock);
            }
            else if (lastBlock.Length < 16)
            {
                int paddingLength = 16 - lastBlock.Length;
                byte[] paddedBlock = new byte[16];

                Array.Copy(lastBlock, paddedBlock, lastBlock.Length);

                for (int i = lastBlock.Length; i < 16; i++)
                    paddedBlock[i] = (byte)paddingLength;

                // replace last block to padded block
                result.RemoveAt(result.Count - 1);
                result.Add(paddedBlock);
            }

            return result;
        }


        private static byte[] RemovePKCS7Padding(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data), "The input data should not be null or empty.");

            // Get the value of the last byte. This will indicate the number of padding bytes.
            int paddingLength = data[data.Length - 1];

            // Validate the padding. All padding bytes should have the value of paddingLength.
            for (int i = 0; i < paddingLength; i++)
            {
                if (data[data.Length - 1 - i] != paddingLength)
                {
                    throw new InvalidOperationException("Invalid PKCS#7 padding.");
                }
            }

            // Copy the data without the padding bytes
            byte[] result = new byte[data.Length - paddingLength];
            Array.Copy(data, 0, result, 0, data.Length - paddingLength);

            return result;
        }


        private byte[] EncryptBlock(byte[] blockData)
        {
            int size = 16 / 4;

            uint[] X_cur = new uint[size];
            uint[] X_next = new uint[size];

            for (int j = 0; j < 4; j++)
                X_cur[j] = BitConverter.ToUInt32(blockData, j * size);

            for (int i = 0; i < Nr; i++)
            {
                RoundEncrypt(X_next, X_cur, _roundKey.Skip(i * 6).Take(6).ToArray());
                Array.Copy(X_next, X_cur, size);
            }

            byte[] ciphertext = new byte[16];
            Buffer.BlockCopy(X_cur, 0, ciphertext, 0, 16);

            return ciphertext;
        }


        public byte[] Encrypt(byte[] key, byte[] plaintext)
        {
            CalculateRoundKey(key);
            List<byte> result = new List<byte>();

            var blocks = AddPkcs7Padding(SplitInto16ByteChunks(plaintext));
            foreach (var block in blocks)
            {
                result.AddRange(EncryptBlock(ApplyPKCS7Padding(block)));
            }

            return result.ToArray();
        }

        public byte[] Decrypt(byte[] key, byte[] encryptedData)
        {
            if (encryptedData.Length % 16 != 0)
                throw new ArgumentException("Encrypted data length must be a multiple of 128 bits.");

            CalculateRoundKey(key);

            uint[] X_cur = new uint[4];
            uint[] X_next = new uint[4];

            for (int j = 0; j < 4; j++)
                X_cur[j] = BitConverter.ToUInt32(encryptedData, j * 4);

            for (int i = 0; i < Nr; i++)
            {
                int basis = (int)((Nr - i - 1) * 6);   // the maximum value of Nr is 32 so long type is not need.
                RoundDecrypt(X_next, X_cur, _roundKey.Skip(basis).Take(6).ToArray());
                Array.Copy(X_next, X_cur, 4);
            }

            byte[] plaintext = new byte[16];
            Buffer.BlockCopy(X_cur, 0, plaintext, 0, 16);

            //            RemovePKCS7Padding(plaintext)

            return plaintext;
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            throw new NotImplementedException();
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            throw new NotImplementedException();
        }

        public override void GenerateIV()
        {
            throw new NotImplementedException();
        }

        public override void GenerateKey()
        {
            throw new NotImplementedException();
        }
    }


    public class LEATransform : ICryptoTransform
    {
        private readonly bool _disposed = false;
        private readonly bool _encrypting;

        private readonly byte[] _data;
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public bool CanReuseTransform => false;
        public bool CanTransformMultipleBlocks => false;
        public int InputBlockSize => 16;
        public int OutputBlockSize => 16;


        public LEATransform(byte[] key, byte[] iv, bool encrypting)
        {
            _key = key;
            _iv = iv;
            _encrypting = encrypting;
        }

        public void Dispose()
        {
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            throw new NotImplementedException();
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var data = inputBuffer.Skip(inputOffset).Take(inputCount).ToArray();

            if (_encrypting) SEED.Encrypt(data, _key);
            else SEED.Decrypt(data, _key);

            return data;
        }
    }
}
