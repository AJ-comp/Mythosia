using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mythosia.Security.Cryptography
{
    internal class Shake128
    {
        // Round constants for faster computation
        private static readonly ulong[] keccak_RC =
        {
            0x0000000000000001, 0x0000000000008082, 0x800000000000808a,
            0x8000000080008000, 0x000000000000808b, 0x0000000080000001,
            0x8000000080008081, 0x8000000000008009, 0x000000000000008a,
            0x0000000000000088, 0x0000000080008009, 0x000000008000000a,
            0x000000008000808b, 0x800000000000008b, 0x8000000000008089,
            0x8000000000008003, 0x8000000000008002, 0x8000000000000080,
            0x000000000000800a, 0x800000008000000a, 0x8000000080008081,
            0x8000000000008080, 0x0000000080000001, 0x8000000080008008
        };

        private static int Index(int x, int y)
        {
            return 5 * y + x;
        }

        private static ulong Rot64L(ulong ac, int l)
        {
            l %= 64;
            return (ac << l) | (ac >> (64 - l));
        }

        private static ulong Rot64R(ulong ac, int l)
        {
            l %= 64;
            return (ac >> l) | (ac << (64 - l));
        }

        private static void PrintState(ulong[] A)
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Console.Write($"{A[Index(j, i)]:x18} ");
                }
                Console.WriteLine();
            }
        }

        private static void Theta(ulong[] A)
        {
            ulong[] C = new ulong[5];
            for (int x = 0; x < 5; x++)
            {
                C[x] = A[Index(x, 0)] ^ A[Index(x, 1)] ^ A[Index(x, 2)] ^ A[Index(x, 3)] ^ A[Index(x, 4)];
            }

            ulong[] D = new ulong[5];
            for (int x = 0; x < 5; x++)
            {
                D[x] = C[(x + 4) % 5] ^ Rot64L(C[(x + 1) % 5], 1);
            }

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    A[Index(x, y)] ^= D[x];
                }
            }
        }


        private static void Rho(ulong[] A)
        {
            ulong[] Ap = new ulong[25];
            Ap[0] = A[0];

            int x = 1, y = 0;
            for (int t = 0; t <= 23; t++)
            {
                Ap[Index(x, y)] = Rot64L(A[Index(x, y)], (t + 1) * (t + 2) / 2);

                int yp = (2 * x + 3 * y) % 5;
                x = y;
                y = yp;
            }
            Array.Copy(Ap, A, 25);
        }

        private static void Pi(ulong[] A)
        {
            ulong[] Ap = new ulong[25];
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    Ap[Index(x, y)] = A[Index((x + 3 * y) % 5, x)];
                }
            }
            Array.Copy(Ap, A, 25);
        }

        private static void Chi(ulong[] A)
        {
            ulong[] Ap = new ulong[25];
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    Ap[Index(x, y)] = A[Index(x, y)] ^ ((~A[Index((x + 1) % 5, y)]) & A[Index((x + 2) % 5, y)]);
                }
            }
            Array.Copy(Ap, A, 25);
        }

        private static ulong RC(int t)
        {
            t %= 255;
            if (t == 0)
                return 1;

            ulong R = 1;

            for (int i = 1; i <= t; i++)
            {
                R <<= 1;
                R ^= (R & (1UL << 8)) >> 8;
                R ^= (R & (1UL << 4)) >> 4;
                R ^= (R & (1UL << 5)) >> 5;
                R ^= (R & (1UL << 6)) >> 6;
            }
            return R;
        }


        private static void Iota(ulong[] A, int ir)
        {
            A[0] ^= keccak_RC[ir];
        }

        private static void Rnd(ulong[] A, int ir)
        {
            Theta(A);
            Rho(A);
            Pi(A);
            Chi(A);
            Iota(A, ir);
        }

        private static void Keccakp(int nr, byte[] S)
        {
            int l = 6;
            ulong[] A = new ulong[25];
            for (int i = 0; i < 25; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    A[i] += ((ulong)S[8 * i + j]) << (8 * j);
                }
            }

            for (int ir = 12 + 2 * l - nr; ir < 12 + 2 * l; ir++)
            {
                Rnd(A, ir);
            }

            byte mask = 0xff;
            for (int i = 0; i < 25; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    S[8 * i + j] = (byte)((A[i] >> (8 * j)) & mask);
                }
            }
        }

        private static void Pad0Star1(int x, int m, IEnumerable<byte> outList)
        {
            m -= 4; // We already added the prefix
            int j = ((-m - 2) % x + x) % x;
            int l = (j - 10) / 8;
            for (int k = 1; k <= l; k++)
            {
                outList = outList.Append((byte)0);
            }
            outList = outList.Append((byte)0x80);
        }

        private static IEnumerable<byte> Sponge(IEnumerable<byte> N, int dInBytes, int r)
        {
            Pad0Star1(r, 8 * N.Count(), N);

            int n = (8 * N.Count()) / r;
            int c = 1600 - r;

            byte[] S = new byte[200];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < r / 8; j++)
                    S[j] ^= N.ElementAt(i * r / 8 + j);
                Keccakp(24, S);
            }

            List<byte> Z = new List<byte>();

            while (Z.Count < dInBytes)
            {
                Z.AddRange(S.Take(r / 8));
                Keccakp(24, S);
            }

            List<byte> outList = new List<byte>();
            outList.AddRange(Z.Take(dInBytes));

            return outList;
        }

        private static IEnumerable<byte> Keccak(int c, IEnumerable<byte> S, int d)
        {
            return Sponge(S, d, 1600 - c);
        }

        public byte[] ComputeHash(IEnumerable<byte> input, int d)
        {
            byte pad = 0x1f;
            input = input.Append(pad);

            return Keccak(256, input, d).ToArray();
        }

    }
}
