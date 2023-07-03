using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Mythosia
{
    public static class StreamExtension
    {
        /*******************************************************************************/
        /// <summary>
        /// Serializes an object using the Marshal class. If the object implements the ICustomMarshal interface,
        /// the custom serialization method is invoked. Otherwise, the object is serialized using the Marshal class.
        /// </summary>
        /// <param name="data">The object to be serialized.</param>
        /// <returns>A byte array representing the serialized object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the 'data' parameter is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the 'data' object does not implement the ICustomMarshal interface
        /// or the serialization using Marshal fails.</exception>
        /*******************************************************************************/
        public static byte[] SerializeUsingMarshal(this object data)
        {
            if (data is ICustomMarshal)
                return (data as ICustomMarshal).Serialize(data).ToArray();

            int size = Marshal.SizeOf(data);

            byte[] result = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, result, 0, size);
            Marshal.FreeHGlobal(ptr);

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Deserializes a byte array into an object using the Marshal class. If the object implements the ICustomMarshal interface,
        /// the custom deserialization method is invoked. Otherwise, the byte array is copied to an unmanaged memory buffer,
        /// and the buffer is used to populate the object through the PtrToStructure method of the Marshal class.
        /// </summary>
        /// <param name="to">The object to be populated with deserialized data.</param>
        /// <param name="from">The byte array containing the serialized object.</param>
        /// <exception cref="ArgumentNullException">Thrown if the 'to' parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the 'from' parameter is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the 'to' object does not implement the ICustomMarshal interface
        /// or the deserialization using Marshal fails.</exception>
        /*******************************************************************************/
        public static void DeSerializeUsingMarshal(this object to, IEnumerable<byte> from)
        {
            var cFrom = from.ToArray();
            if (to is ICustomMarshal) (to as ICustomMarshal).Deserialize(cFrom);
            else
            {
                IntPtr buffer = Marshal.AllocHGlobal(cFrom.Length);

                Marshal.Copy(cFrom, 0, buffer, cFrom.Length);
                Marshal.PtrToStructure(buffer, to);
                Marshal.FreeHGlobal(buffer);
            }
        }


        /**********************************************/
        /// <summary>
        /// 크기가 대상과 정확하게 맞을때만 역직렬화를 수행합니다
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /**********************************************/
        /*
        public static void DeSerializeCorrectly(byte[] from, object to)
        {
            int size = (to is IFixedSize) ? (to as IFixedSize).Size : Marshal.SizeOf(to);
            if (from.Length != size)
                throw new InvalidOperationException($"from size: [{from.Length}]  to size: [{size}]");

            DeSerialize(from, to);
        }
        */
    }
}
