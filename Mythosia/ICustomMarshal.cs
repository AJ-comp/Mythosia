using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Mythosia
{
    internal interface ICustomMarshal
    {
        void Deserialize(byte[] from);

        /// <summary>
        /// data must not be generic type.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="index"></param>
        /// <param name="data"></param>
        public void DeserializeByUnit(byte[] from, int index, object data)
        {
            var toSize = Marshal.SizeOf(data.GetType());
            IntPtr buffer = Marshal.AllocHGlobal(toSize);
            Marshal.Copy(from, index, buffer, toSize);
            Marshal.PtrToStructure(buffer, data);
            Marshal.FreeHGlobal(buffer);
        }


        IEnumerable<byte> Serialize(object data);

        /// <summary>
        /// data must not be generic type.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public IEnumerable<byte> SerializeByUnit(object data)
        {
            int size = Marshal.SizeOf(data);

            byte[] result = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, result, 0, size);
            Marshal.FreeHGlobal(ptr);

            return result;
        }
    }
}
