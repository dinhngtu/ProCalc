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

    public static T ToCalculatorTypeTruncating<T>(object value) where T : IBinaryInteger<T>, IMinMaxValue<T> {
        return value switch {
            Int128 s => T.CreateTruncating(s),
            long s => T.CreateTruncating(s),
            int s => T.CreateTruncating(s),
            short s => T.CreateTruncating(s),
            sbyte s => T.CreateTruncating(s),
            UInt128 u => T.CreateTruncating(u),
            ulong u => T.CreateTruncating(u),
            uint u => T.CreateTruncating(u),
            ushort u => T.CreateTruncating(u),
            byte u => T.CreateTruncating(u),
            _ => throw new InvalidCastException(),
        };
    }

    public static U ToCalculatorTypeUnsigned<T, U>(T x)
        where T : IBinaryInteger<T>, IMinMaxValue<T>
        where U : IBinaryInteger<U>, IMinMaxValue<U> {
        return x switch {
            Int128 => U.CreateTruncating(UInt128.CreateTruncating(x)),
            long => U.CreateTruncating(ulong.CreateTruncating(x)),
            int => U.CreateTruncating(uint.CreateTruncating(x)),
            short => U.CreateTruncating(ushort.CreateTruncating(x)),
            sbyte => U.CreateTruncating(byte.CreateTruncating(x)),
            _ => throw new InvalidCastException(),
        };
    }
}
