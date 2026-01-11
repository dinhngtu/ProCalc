using System.Numerics;

namespace ProCalcCore;

public static class IntConverter {
    public static UInt128 ToUInt128(object value) {
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
        where T : INumberBase<T>, IMinMaxValue<T>, ISignedNumber<T>
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
        if (typeof(T) == typeof(Int128)) {
            return UInt128.CreateTruncating(a) < UInt128.CreateTruncating(b);
        }
        else if (typeof(T) == typeof(long)) {
            return ulong.CreateTruncating(a) < ulong.CreateTruncating(b);
        }
        else if (typeof(T) == typeof(int)) {
            return uint.CreateTruncating(a) < uint.CreateTruncating(b);
        }
        else if (typeof(T) == typeof(short)) {
            return ushort.CreateTruncating(a) < ushort.CreateTruncating(b);
        }
        else if (typeof(T) == typeof(sbyte)) {
            return byte.CreateTruncating(a) < byte.CreateTruncating(b);
        }
        else {
            throw new InvalidCastException();
        }
    }

    public static bool UnsignedMultiplyOverflows<T>(T a, T b) where T : INumberBase<T> {
        var la = ToUInt128(a);
        var lb = ToUInt128(b);
        UInt128 result;
        try {
            result = checked(la * lb);
        }
        catch (OverflowException) {
            return true;
        }
        if (typeof(T) == typeof(Int128)) {
            return false;
        }
        else if (typeof(T) == typeof(long)) {
            return result > UInt128.CreateTruncating(ulong.MaxValue);
        }
        else if (typeof(T) == typeof(int)) {
            return result > UInt128.CreateTruncating(uint.MaxValue);
        }
        else if (typeof(T) == typeof(short)) {
            return result > UInt128.CreateTruncating(ushort.MaxValue);
        }
        else if (typeof(T) == typeof(sbyte)) {
            return result > UInt128.CreateTruncating(byte.MaxValue);
        }
        else {
            throw new InvalidCastException();
        }
    }

    public static T UnsignedDivide<T>(T a, T b) where T : INumberBase<T> {
        var la = ToUInt128(a);
        var lb = ToUInt128(b);
        return T.CreateTruncating(la / lb);
    }
}
