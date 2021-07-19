using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grasslang.OwoVMCompiler
{
    public record Register(byte index, ImmediateType type = ImmediateType.Int32);
    public enum ImmediateType
    {
        Uint64, Uint32, Uint16, Uint8, Int64, Int32, Int16, Int8, Float, Double,
        Unknown
    }
    public record Immediate(ImmediateType type, string value);
    public class CodeBuffer
    {
        public List<byte> Buffer = new List<byte>();
        public int Length => Buffer.Count;
        public int LengthIncluded = 0;
        public int LengthExcluded => Length - LengthIncluded;
        public void Emit(byte a)
        {
            Buffer.Add(a);
        }
        public void Emit(byte[] a)
        {
            Buffer.AddRange(a);
        }
        public void Emit(string a)
        {
            Emit(Encoding.Default.GetBytes(a));
        }
        public void Emit(UInt64 a)
        {
            Buffer.AddRange(BitConverter.GetBytes(a));
        }
        public void Emit(Int64 a)
        {
            Buffer.AddRange(BitConverter.GetBytes(a));
        }
        public void Emit(Register a)
        {
            Emit(a.index);
        }
        public void Emit(Immediate a)
        {
            if (a.type is ImmediateType.Uint64
                or ImmediateType.Uint32
                or ImmediateType.Uint16
                or ImmediateType.Uint8)
            {
                Emit(ulong.Parse(a.value));
            } else if (a.type is ImmediateType.Int64
                or ImmediateType.Int32
                or ImmediateType.Int16
                or ImmediateType.Int8)
            {
                Emit(long.Parse(a.value));
            } else if (a.type is ImmediateType.Double)
            {
                Emit(BitConverter.GetBytes(double.Parse(a.value)));
            } else if (a.type is ImmediateType.Float)
            {
                Emit(BitConverter.GetBytes(double.Parse(a.value)));
            }
        }
        public void Emit(CodeBuffer a)
        {
            LengthIncluded += a.Length;
            Buffer.AddRange(a.Buffer);
        }
        public byte[] Build()
        {
            return Buffer.ToArray();
        }

        private static Dictionary<ImmediateType, byte> typeBitsMap = new Dictionary<ImmediateType, byte>
        {
            {ImmediateType.Int8, 0b0100},
            {ImmediateType.Int16, 0b0100},
            {ImmediateType.Int32, 0b0100},
            {ImmediateType.Int64, 0b0100},
            {ImmediateType.Uint8, 0b0000},
            {ImmediateType.Uint16, 0b0000},
            {ImmediateType.Uint32, 0b0000},
            {ImmediateType.Uint64, 0b0000},

            {ImmediateType.Float, 0b1000},
            {ImmediateType.Double, 0b1100},
            {ImmediateType.Unknown, 0b0000 }
        };
        private static byte GetTypeBits(ImmediateType type, bool isImm = false)
        {
            byte raw = typeBitsMap[type];
            if (raw == 0) return raw;
            raw = (byte)(raw | (isImm ? 0b0010 : 0b0001));
            return raw;
        }

        public void EmitMov<T>(Register reg, T val)
        {
            Emit((byte)0x0D); // opcode: mov
            Emit(reg); // op1
            if(val is Register op2reg)
            {
                Emit(op2reg); // op2
                Emit((byte)((GetTypeBits(op2reg.type) << 4) | GetTypeBits(op2reg.type))); // opext
            } else if(val is Immediate op2imm)
            {
                Emit((byte)0); // op2
                Emit((byte)((GetTypeBits(op2imm.type) << 4) | GetTypeBits(op2imm.type, true))); // opext
                Emit(op2imm);
            }
        }
        public void EmitCall(string name)
        {
            Emit((byte)0x0A); // opcode: call
            Emit((byte)0x00); // op1
            Emit((byte)0x00); // op2
            Emit((byte)0x00); // opext
            Emit(name);
            Emit((byte)0x00); // end of string
        }
        public void EmitVMCall()
        {
            Emit(new byte[] { 0x0E, 0, 0, 0 });
        }

        public void EmitString(Register reg, string str)
        {
            Emit((byte)0x12); // opcode: const string
            Emit(reg); // op1
            Emit(0); // op2
            Emit(0); // opext
            Emit(Convert.ToUInt64(str.Length)); // string length
            Emit(str); // string
        }

        public void EmitRet()
        {
            Emit((byte)0x0B); // opcode: ret
            Emit(0); // op1
            Emit(0); // op2
            Emit(0); // opext
        }

        public void EmitBrfalse(Register reg, Immediate target)
        {
            Emit((byte)0x10); // opcode: brfalse
            Emit(reg); // op1
            Emit((byte)0x00); // op2
            Emit(0); // opext
            Emit(target);
        }
    }
}
