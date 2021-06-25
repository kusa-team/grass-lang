using System;
using System.Collections.Generic;
using System.Text;
namespace grasslang.OwoVMCompiler
{
    class IndexItem
    {
        public string Name;
        public byte Type;
        public UInt64 Data;
        public IndexItem(string name, byte type, UInt64 data)
        {
            this.Name = name;
            this.Type = type;
            this.Data = data;
        }
        public byte[] CompileToArray()
        {
            List<byte> result = new List<byte>();
            // key
            UInt32 nameLength = Convert.ToUInt32(Name.Length);
            result.AddRange(BitConverter.GetBytes(nameLength));
            result.AddRange(Encoding.Default.GetBytes(Name));
            // value
            result.Add(Type); // type
            result.Add((byte)0); // flags(default 0)
            result.AddRange(BitConverter.GetBytes(Data)); // data
            return result.ToArray();
        }
    }
}
