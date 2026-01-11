using ProCalcCore;

namespace ProCalcTests;

public class TestIntConverter {
    [Fact]
    public void ToUInt128_FromSignedTypes_PreservesBitPatternOfSourceType_ZeroExtendsTo128() {
        // The implementation of ToUInt128 casts to the unsigned type of the SAME size first, 
        // effectively treating the signed value as unsigned bits, and then zero-extends to 128 bits.

        // sbyte: -1 (0xFF) -> byte (0xFF) -> UInt128 (255)
        Assert.Equal((UInt128)255, IntConverter.ToUInt128((sbyte)-1));

        // short: -1 (0xFFFF) -> ushort (0xFFFF) -> UInt128 (65535)
        Assert.Equal((UInt128)65535, IntConverter.ToUInt128((short)-1));

        // int: -1 (0xFFFFFFFF) -> uint (0xFFFFFFFF) -> UInt128 (uint.MaxValue)
        Assert.Equal((UInt128)uint.MaxValue, IntConverter.ToUInt128(-1));

        // long: -1 -> ulong -> UInt128 (ulong.MaxValue)
        Assert.Equal((UInt128)ulong.MaxValue, IntConverter.ToUInt128((long)-1));
    }

    [Fact]
    public void ToUInt128_FromInt128_CastsDirectly() {
        // Int128 => UInt128.CreateTruncating(val128)
        // -1 (all ones in 128 bits) -> UInt128.MaxValue
        Assert.Equal(UInt128.MaxValue, IntConverter.ToUInt128((Int128)(-1)));
    }

    [Fact]
    public void ToCalculatorTypeTruncating_SignExtends_WhenTargetIsLarger() {
        // Standard casts usually sign-extend signed types.

        // sbyte -1 -> Int128 -1
        Assert.Equal((Int128)(-1), IntConverter.ToCalculatorTypeTruncating<Int128>((sbyte)-1));

        // int -1 -> long -1
        Assert.Equal(-1, IntConverter.ToCalculatorTypeTruncating<long>(-1));
    }

    [Fact]
    public void ToCalculatorTypeTruncating_Truncates_WhenTargetIsSmaller() {
        // int 257 (0x101) -> byte (0x01)
        Assert.Equal((byte)1, IntConverter.ToCalculatorTypeTruncating<byte>(257));

        // long -> int
        long largeVal = (long)int.MaxValue + 1; // 0x80000000 (which is int.MinValue if interpreted as signed 32-bit)
        // int.MinValue is -2147483648
        Assert.Equal(int.MinValue, IntConverter.ToCalculatorTypeTruncating<int>(largeVal));
    }

    [Fact]
    public void ToCalculatorTypeTruncating_SignedToUnsigned_BehavesLikeCast() {
        // sbyte -1 -> UInt128.MaxValue (because standard cast sign-extends to target width then treats as unsigned? 
        // or does it treat bits as unsigned? 
        // T.CreateTruncating(sbyte) typically does `(T)value`.
        // (UInt128)(sbyte)-1 is UInt128.MaxValue.
        Assert.Equal(UInt128.MaxValue, IntConverter.ToCalculatorTypeTruncating<UInt128>((sbyte)-1));
    }

    [Fact]
    public void ToCalculatorTypeUnsigned_InterpretsSourceBitsAsUnsigned_ThenConverts() {
        // This method: sbyte => U.CreateTruncating(byte.CreateTruncating(x))
        // So sbyte -1 (0xFF) -> byte 255 -> Target Type (255)

        // Target Int128
        Assert.Equal((Int128)255, IntConverter.ToCalculatorTypeUnsigned<sbyte, Int128>(-1));

        // Target UInt128
        Assert.Equal((UInt128)255, IntConverter.ToCalculatorTypeUnsigned<sbyte, UInt128>(-1));

        // short -1 (0xFFFF) -> ushort 65535 -> Target
        Assert.Equal((Int128)65535, IntConverter.ToCalculatorTypeUnsigned<short, Int128>(-1));

        // int -1 -> uint.MaxValue -> Target
        Assert.Equal((Int128)uint.MaxValue, IntConverter.ToCalculatorTypeUnsigned<int, Int128>(-1));

        // long -1 -> ulong.MaxValue -> Target
        Assert.Equal((Int128)ulong.MaxValue, IntConverter.ToCalculatorTypeUnsigned<long, Int128>(-1));
    }

    [Fact]
    public void TestInvalidTypes() {
        Assert.ThrowsAny<Exception>(() => IntConverter.ToUInt128(1.5f));
        Assert.ThrowsAny<Exception>(() => IntConverter.ToCalculatorTypeTruncating<int>("string"));
    }

    [Fact]
    public void TestUnsignedInputs() {
        // Byte -> Int
        Assert.Equal(255, IntConverter.ToCalculatorTypeTruncating<int>((byte)255));

        // UShort -> Int
        Assert.Equal(65535, IntConverter.ToCalculatorTypeTruncating<int>((ushort)65535));

        // UInt -> Long
        Assert.Equal(uint.MaxValue, IntConverter.ToCalculatorTypeTruncating<long>(uint.MaxValue));

        // ULong -> Int128
        Assert.Equal((Int128)ulong.MaxValue, IntConverter.ToCalculatorTypeTruncating<Int128>(ulong.MaxValue));

        // UInt128 -> Int128
        Assert.Equal((Int128)100, IntConverter.ToCalculatorTypeTruncating<Int128>((UInt128)100));
    }

    [Fact]
    public void TestTruncation() {
        var _ = IntConverter.ToCalculatorType<short, UInt128>(32767, out bool truncated);
        Assert.False(truncated);

        _ = IntConverter.ToCalculatorType<short, UInt128>(32768, out truncated);
        Assert.False(truncated);

        _ = IntConverter.ToCalculatorType<short, UInt128>(65535, out truncated);
        Assert.False(truncated);

        _ = IntConverter.ToCalculatorType<short, UInt128>(65536, out truncated);
        Assert.True(truncated);

        _ = IntConverter.ToCalculatorType<short, UInt128>(UInt128.CreateTruncating(-32768), out truncated);
        Assert.False(truncated);

        _ = IntConverter.ToCalculatorType<short, UInt128>(UInt128.CreateTruncating(-32769), out truncated);
        Assert.True(truncated);
    }
}
