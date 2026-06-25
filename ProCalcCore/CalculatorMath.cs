using System.Buffers.Binary;
using System.Numerics;

namespace ProCalcCore;

public static class CalculatorMath {
    public static int ByteCount(object value) {
        return value switch {
            Int128 => 16,
            long => 8,
            int => 4,
            short => 2,
            sbyte => 1,
            _ => throw new InvalidCastException(),
        };
    }

    public static UInt128 ToUInt128Unsigned(object value) {
        return value switch {
            Int128 val128 => UInt128.CreateTruncating(val128),
            long val64 => UInt128.CreateTruncating(ulong.CreateTruncating(val64)),
            int val32 => UInt128.CreateTruncating(uint.CreateTruncating(val32)),
            short val16 => UInt128.CreateTruncating(ushort.CreateTruncating(val16)),
            sbyte val8 => UInt128.CreateTruncating(byte.CreateTruncating(val8)),
            _ => throw new InvalidOperationException(),
        };
    }

    public static T ToCalculatorType<T, U>(U value, out bool truncated)
        where T : ISignedNumber<T>, IMinMaxValue<T>
        where U : INumberBase<U> {
        var full = UInt128.CreateTruncating(value);
        var limit = UInt128.CreateTruncating(T.MaxValue);
        truncated = (full > (limit * 2 + 1)) && (full < (UInt128.MaxValue - limit));
        return T.CreateTruncating(value);
    }

    public static T ToCalculatorTypeTruncating<T>(object value) where T : INumberBase<T> {
        return value switch {
            Int128 x => T.CreateTruncating(x),
            long x => T.CreateTruncating(x),
            int x => T.CreateTruncating(x),
            short x => T.CreateTruncating(x),
            sbyte x => T.CreateTruncating(x),
            UInt128 x => T.CreateTruncating(x),
            ulong x => T.CreateTruncating(x),
            uint x => T.CreateTruncating(x),
            ushort x => T.CreateTruncating(x),
            byte x => T.CreateTruncating(x),
            _ => throw new InvalidCastException(),
        };
    }

    public static U ToCalculatorTypeUnsigned<T, U>(T x)
        where T : INumberBase<T>
        where U : INumberBase<U> {
        return x switch {
            Int128 => U.CreateTruncating(UInt128.CreateTruncating(x)),
            long => U.CreateTruncating(ulong.CreateTruncating(x)),
            int => U.CreateTruncating(uint.CreateTruncating(x)),
            short => U.CreateTruncating(ushort.CreateTruncating(x)),
            sbyte => U.CreateTruncating(byte.CreateTruncating(x)),
            _ => throw new InvalidCastException(),
        };
    }

    public static bool UnsignedLess<T>(T a, T b) where T : INumberBase<T> {
        return a switch {
            Int128 => UInt128.CreateTruncating(a) < UInt128.CreateTruncating(b),
            long => ulong.CreateTruncating(a) < ulong.CreateTruncating(b),
            int => uint.CreateTruncating(a) < uint.CreateTruncating(b),
            short => ushort.CreateTruncating(a) < ushort.CreateTruncating(b),
            sbyte => byte.CreateTruncating(a) < byte.CreateTruncating(b),
            _ => throw new InvalidCastException(),
        };
    }

    public static bool UnsignedMultiplyOverflows<T>(T a, T b) where T : INumberBase<T> {
        var la = ToUInt128Unsigned(a);
        var lb = ToUInt128Unsigned(b);
        UInt128 result;
        try {
            result = checked(la * lb);
        } catch (OverflowException) {
            return true;
        }
        return a switch {
            Int128 => false,
            long => result > UInt128.CreateTruncating(ulong.MaxValue),
            int => result > UInt128.CreateTruncating(uint.MaxValue),
            short => result > UInt128.CreateTruncating(ushort.MaxValue),
            sbyte => result > UInt128.CreateTruncating(byte.MaxValue),
            _ => throw new InvalidCastException(),
        };
    }

    public static T UnsignedDivide<T>(T a, T b) where T : INumberBase<T> {
        var la = ToUInt128Unsigned(a);
        var lb = ToUInt128Unsigned(b);
        return T.CreateTruncating(la / lb);
    }

    public static T ByteSwap<T>(T x) where T : INumberBase<T> {
        return x switch {
            Int128 v => T.CreateTruncating(BinaryPrimitives.ReverseEndianness(v)),
            long v => T.CreateTruncating(BinaryPrimitives.ReverseEndianness(v)),
            int v => T.CreateTruncating(BinaryPrimitives.ReverseEndianness(v)),
            short v => T.CreateTruncating(BinaryPrimitives.ReverseEndianness(v)),
            sbyte v => T.CreateTruncating(BinaryPrimitives.ReverseEndianness(v)),
            _ => throw new InvalidCastException(),
        };
    }
}
