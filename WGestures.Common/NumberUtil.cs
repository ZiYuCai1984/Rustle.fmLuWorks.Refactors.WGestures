using System.Runtime.InteropServices;

namespace WGestures.Common
{
    [StructLayout(LayoutKind.Explicit)]
    public struct HiLoWord
    {
        [FieldOffset(0)] public uint Number;

        [FieldOffset(0)] public ushort Low;

        [FieldOffset(2)] public ushort High;

        public HiLoWord(uint number)
            : this()
        {
            Number = number;
        }

        public static implicit operator uint(HiLoWord val)
        {
            return val.Number;
        }

        public static implicit operator HiLoWord(uint val)
        {
            return new HiLoWord(val);
        }
    }
}
