using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia.Test
{
    [DataContract]
    public class SerializeTest
    {
        [DataMember]
        public byte Value { get; set; }


        public int Size() => Marshal.SizeOf(this);


        public IEnumerable<byte> Serialize()
        {
            /*
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                DataContractSerializer serializer = new DataContractSerializer(this.GetType());
                serializer.WriteObject(memoryStream, this);
                memoryStream.Position = 0;
                return reader.ReadToEnd();
            }
            */

            throw new NotImplementedException();
        }
    }
}
