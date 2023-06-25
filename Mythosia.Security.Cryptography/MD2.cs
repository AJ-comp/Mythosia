using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Mythosia.Security.Cryptography
{
    // refer to https://nickthecrypt.medium.com/cryptography-hash-method-md2-message-digest-2-step-by-step-explanation-made-easy-with-python-10faa2e35e85
    // refer to https://www.sololearn.com/Discuss/2905790/solved-md2-algorithm-on-python-cannot-spot-a-problem

    public class MD2 : HashAlgorithm
    {
        private static readonly byte[] S = new byte[]
        {
            41, 46, 67, 201, 162, 216, 124, 1, 61, 54, 84, 161, 236, 240, 6, 19,
            98, 167, 5, 243, 192, 199, 115, 140, 152, 147, 43, 217, 188, 76, 130, 202,
            30, 155, 87, 60, 253, 212, 224, 22, 103, 66, 111, 24, 138, 23, 229, 18,
            190, 78, 196, 214, 218, 158, 222, 73, 160, 251, 245, 142, 187, 47, 238, 122,
            169, 104, 121, 145, 21, 178, 7, 63, 148, 194, 16, 137, 11, 34, 95, 33,
            128, 127, 93, 154, 90, 144, 50, 39, 53, 62, 204, 231, 191, 247, 151, 3,
            255, 25, 48, 179, 72, 165, 181, 209, 215, 94, 146, 42, 172, 86, 170, 198,
            79, 184, 56, 210, 150, 164, 125, 182, 118, 252, 107, 226, 156, 116, 4, 241,
            69, 157, 112, 89, 100, 113, 135, 32, 134, 91, 207, 101, 230, 45, 168, 2,
            27, 96, 37, 173, 174, 176, 185, 246, 28, 70, 97, 105, 52, 64, 126, 15,
            85, 71, 163, 35, 221, 81, 175, 58, 195, 92, 249, 206, 186, 197, 234, 38,
            44, 83, 13, 110, 133, 40, 132, 9, 211, 223, 205, 244, 65, 129, 77, 82,
            106, 220, 55, 200, 108, 193, 171, 250, 36, 225, 123, 8, 12, 189, 177, 74,
            120, 136, 149, 139, 227, 99, 232, 109, 233, 203, 213, 254, 59, 0, 29, 57,
            242, 239, 183, 14, 102, 88, 208, 228, 166, 119, 114, 248, 235, 117, 75, 10,
            49, 68, 80, 180, 143, 237, 31, 26, 219, 153, 141, 51, 159, 17, 131, 20
        };

        private const int BLOCK_SIZE = 16;
        private byte[] _source;

        public override void Initialize()
        {
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _source = array.Copy();
        }

        protected override byte[] HashFinal()
        {
            // step 1: completing input
            int padding_pat = BLOCK_SIZE - (_source.Length % BLOCK_SIZE);
            byte[] padding = new byte[padding_pat];
            for (int i = 0; i < padding_pat; i++)
            {
                padding[i] = (byte)padding_pat;
            }
            byte[] paddedMsg = new byte[_source.Length + padding.Length];
            Array.Copy(_source, paddedMsg, _source.Length);
            Array.Copy(padding, 0, paddedMsg, _source.Length, padding.Length);

            var data = paddedMsg;

            //            int padding = 16 - (data.Length % 16 == 0 ? 16 : data.Length % 16);
            //            Array.Resize(ref data, data.Length + padding);

            byte l = 0;
            byte[] checksum = new byte[BLOCK_SIZE];
//            int blocks = (int)Math.Ceiling((double)data.Length / BLOCK_SIZE);
            int blocks = data.Length / BLOCK_SIZE;

            for (int i = 0; i < blocks; i++)
            {
                for (int j = 0; j < BLOCK_SIZE; j++)
                {
                    byte index = (byte)(data[i * BLOCK_SIZE + j] ^ l);
                    l = (byte)(S[index] ^ checksum[j]);
                    checksum[j] = l;
                }
            }

            Array.Resize(ref data, data.Length + checksum.Length);
            Array.Copy(checksum, 0, data, data.Length - checksum.Length, checksum.Length);
            blocks += 1;

            // step 3: initialize buffer
            byte[] md_digest = new byte[48];

            // step 4: calculate hash
            for (int i = 0; i < blocks; i++)
            {
                for (int j = 0; j < BLOCK_SIZE; j++)
                {
                    md_digest[BLOCK_SIZE + j] = data[i * BLOCK_SIZE + j];
                    md_digest[2 * BLOCK_SIZE + j] = (byte)(md_digest[BLOCK_SIZE + j] ^ md_digest[j]);
                }

                int checktmp = 0;
                for (int j = 0; j < 18; j++)
                {
                    for (int k = 0; k < 48; k++)
                    {
                        checktmp = md_digest[k] ^ S[checktmp];
                        md_digest[k] = (byte)checktmp;
                    }
                    checktmp = (checktmp + j) % 256;
                }
            }

            return md_digest.Take(16).ToArray();
        }
    }
}
