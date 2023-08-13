using System;
using System.Collections.Generic;
using System.Text;

namespace Mythosia.Integrity.HammingCode
{
    internal class HammingCode
    {
        public static void Main()
        {
            int[] input = { 0, 1, 0, 1 }; // 4비트의 입력 데이터
            int[] encodedData = Encode(input); // 인코딩된 데이터

            Console.WriteLine("Encoded Data: ");
            foreach (int bit in encodedData)
            {
                Console.Write(bit);
            }
            Console.WriteLine();

            int[] receivedData = { 0, 1, 0, 1, 1, 0, 0 }; // 수신된 데이터
            int[] decodedData = Decode(receivedData); // 디코딩된 데이터

            Console.WriteLine("Decoded Data: ");
            foreach (int bit in decodedData)
            {
                Console.Write(bit);
            }
            Console.WriteLine();
        }

        // Hamming(7,4) 인코딩 함수
        public static int[] Encode(int[] input)
        {
            int[] encodedData = new int[7];

            encodedData[2] = input[0];
            encodedData[4] = input[1];
            encodedData[5] = input[2];
            encodedData[6] = input[3];

            encodedData[0] = CalculateParityBit(encodedData, new int[] { 2, 4, 6 });
            encodedData[1] = CalculateParityBit(encodedData, new int[] { 2, 5, 6 });
            encodedData[3] = CalculateParityBit(encodedData, new int[] { 4, 5, 6 });

            return encodedData;
        }

        // Hamming(7,4) 디코딩 함수
        public static int[] Decode(int[] receivedData)
        {
            int[] decodedData = new int[4];

            int[] parityBits = new int[3];
            parityBits[0] = CalculateParityBit(receivedData, new int[] { 0, 2, 4, 6 });
            parityBits[1] = CalculateParityBit(receivedData, new int[] { 1, 2, 5, 6 });
            parityBits[2] = CalculateParityBit(receivedData, new int[] { 3, 4, 5, 6 });

            int errorPosition = parityBits[0] * 1 + parityBits[1] * 2 + parityBits[2] * 4;

            if (errorPosition != 0)
            {
                Console.WriteLine("Error detected at position: " + errorPosition);
                receivedData[errorPosition - 1] = receivedData[errorPosition - 1] ^ 1; // 에러 비트 토글
            }

            decodedData[0] = receivedData[2];
            decodedData[1] = receivedData[4];
            decodedData[2] = receivedData[5];
            decodedData[3] = receivedData[6];

            return decodedData;
        }

        // 패리티 비트 계산 함수
        public static int CalculateParityBit(int[] data, int[] positions)
        {
            int parityBit = 0;

            foreach (int position in positions)
            {
                parityBit = parityBit ^ data[position];
            }

            return parityBit;
        }
    }
}
