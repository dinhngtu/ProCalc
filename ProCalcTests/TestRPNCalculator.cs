using ProCalcCore;

namespace ProCalcTests;

class MockStackEntry : IStackEntry {
    public required object Object { get; set; }
    public string? Comment { get; set; }
    public string? AltComment { get; set; }
}

public class TestRPNCalculator {
    static void Push(RPNCalculator<int> calc, int value) {
        calc.Push(value, null, null);
    }

    [Fact]
    public void TestPushAndPeek() {
        var calc = new RPNCalculator<int>();
        Push(calc, 42);
        var top = calc.Peek();
        Assert.Equal(42, (int)top.Object);
    }

    [Fact]
    public void TestAdd() {
        var calc = new RPNCalculator<int>();
        Push(calc, 10);
        Push(calc, 20);
        calc.DoBinaryOp(BinaryOperation.Add, false, 0);
        Assert.Equal(30, (int)calc.Peek().Object);
        Assert.Equal(1, calc.Count);
    }

    [Fact]
    public void TestSubtract() {
        var calc = new RPNCalculator<int>();
        Push(calc, 30);
        Push(calc, 10);
        calc.DoBinaryOp(BinaryOperation.Subtract, false, 0);
        // 30 - 10 = 20. RPN: 30 10 -
        // First popped is 'b' (10), second is 'a' (30). Result = a - b.
        Assert.Equal(20, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestMultiply() {
        var calc = new RPNCalculator<int>();
        Push(calc, 5);
        Push(calc, 6);
        calc.DoBinaryOp(BinaryOperation.Multiply, false, 0);
        Assert.Equal(30, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestUnsignedDivide() {
        var calc = new RPNCalculator<int>();
        Push(calc, -1);
        Push(calc, 2);
        calc.DoBinaryOp(BinaryOperation.UnsignedDivide, false, 0);
        Assert.Equal(int.MaxValue, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestSignedDivide() {
        var calc = new RPNCalculator<int>();
        Push(calc, 4);
        Push(calc, -2);
        calc.DoBinaryOp(BinaryOperation.SignedDivide, false, 0);
        Assert.Equal(-2, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestStackDrop() {
        var calc = new RPNCalculator<int>();
        Push(calc, 1);
        Push(calc, 2);
        calc.DoStackOp(StackOperation.Drop, 1);
        Assert.Equal(1, calc.Count);
        Assert.Equal(1, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestStackSwap() {
        var calc = new RPNCalculator<int>();
        Push(calc, 1);
        Push(calc, 2);
        // Swap(2) swaps top (index 1) with index 2
        calc.DoStackOp(StackOperation.Swap, 2);
        Assert.Equal(1, (int)calc.Peek().Object); // Top should be 1 now

        calc.DoStackOp(StackOperation.Drop, 1);
        Assert.Equal(2, (int)calc.Peek().Object); // Next should be 2
    }

    [Fact]
    public void TestBitwiseAnd() {
        var calc = new RPNCalculator<int>();
        Push(calc, 0b1100); // 12
        Push(calc, 0b1010); // 10
        calc.DoBinaryOp(BinaryOperation.And, false, 0);
        Assert.Equal(0b1000, (int)calc.Peek().Object); // 8
    }

    [Fact]
    public void TestShiftLeft() {
        var calc = new RPNCalculator<int>();
        Push(calc, 1);
        Push(calc, 2); // shift amount
        calc.DoBinaryOp(BinaryOperation.ShiftLeft, false, 0);
        Assert.Equal(4, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestStackRoll() {
        var calc = new RPNCalculator<int>();
        Push(calc, 1);
        Push(calc, 2);
        Push(calc, 3);
        // Stack: [3, 2, 1] (top to bottom)
        calc.DoStackOp(StackOperation.Roll, 1);
        // Roll(1) -> Roll(1) -> PopFront(3) and PushBack(3)
        // Stack: [2, 1, 3]
        Assert.Equal(2, (int)calc.Peek().Object);
        calc.DoStackOp(StackOperation.Drop, 1);
        Assert.Equal(1, (int)calc.Peek().Object);
        calc.DoStackOp(StackOperation.Drop, 1);
        Assert.Equal(3, (int)calc.Peek().Object);

        // Roll(-1)
        calc.Clear();
        Push(calc, 1);
        Push(calc, 2);
        Push(calc, 3);
        // Stack: [3, 2, 1]
        calc.DoStackOp(StackOperation.Roll, -1);
        // Roll(-1) -> Roll(-1) -> PopBack(1) and PushFront(1)
        // Stack: [1, 3, 2]
        Assert.Equal(1, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestStackExtract() {
        var calc = new RPNCalculator<int>();
        Push(calc, 1);
        Push(calc, 2);
        Push(calc, 3);
        // Stack: [3, 2, 1]
        calc.DoStackOp(StackOperation.Extract, 3);
        // Extract(3) moves 3rd item (1) to top
        // Stack: [1, 3, 2]
        Assert.Equal(1, (int)calc.Peek().Object);
        calc.DoStackOp(StackOperation.Drop, 1);
        Assert.Equal(3, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestStackPick() {
        var calc = new RPNCalculator<int>();
        Push(calc, 1);
        Push(calc, 2);
        Push(calc, 3);
        // Stack: [3, 2, 1]
        calc.DoStackOp(StackOperation.Pick, 2);
        // Pick(2) copies 2nd item (2) to top
        // Stack: [2, 3, 2, 1]
        Assert.Equal(2, (int)calc.Peek().Object);
        Assert.Equal(4, calc.Count);
    }

    [Fact]
    public void TestStackSetComment() {
        var calc = new RPNCalculator<int>();
        Push(calc, 1); // Index 2 after pop
        Push(calc, 2); // Index 1 after pop
        calc.Push(2, "new comment", null); // Top, value 2

        calc.DoStackOp(StackOperation.SetComment, null);

        // Stack should be [2, 1("new comment")]
        Assert.Equal(2, calc.Count);
        calc.DoStackOp(StackOperation.Drop, 1);
        var entry = calc.Peek();
        Assert.Equal(1, (int)entry.Object);
        Assert.Equal("new comment", entry.Comment);
    }

    [Fact]
    public void TestStackSwapComment() {
        var calc = new RPNCalculator<int>();
        calc.Push(1, "A", "B");
        calc.DoStackOp(StackOperation.SwapComment, 1);
        var entry = calc.Peek();
        Assert.Equal("B", entry.Comment);
        Assert.Equal("A", entry.AltComment);
    }

    [Fact]
    public void TestConversionSignExtend() {
        var calc8 = new RPNCalculator<sbyte>();
        calc8.Push(-1, null, null); // 0xFF

        var calc32 = calc8.Into<int>(signExtend: true);
        Assert.Equal(-1, (int)calc32.Peek().Object);
        Assert.Equal(0xFFFFFFFFu, (uint)(int)calc32.Peek().Object);
    }

    [Fact]
    public void TestConversionZeroExtend() {
        var calc8 = new RPNCalculator<sbyte>();
        calc8.Push(-1, null, null); // 0xFF

        var calc32 = calc8.Into<int>(signExtend: false);
        Assert.Equal(255, (int)calc32.Peek().Object);
        Assert.Equal(0x000000FFu, (uint)(int)calc32.Peek().Object);
    }

    [Fact]
    public void TestConversionTruncate() {
        var calc = new RPNCalculator<long>();
        calc.Push(0x1_0000_0000L, null, null); // Larger than int

        var converted = (RPNCalculator<int>)calc.ConvertTo(typeof(int), false);
        var res = converted.Peek();

        Assert.Equal(0, (int)res.Object);
    }

    [Fact]
    public void TestUnaryOpsCoverage() {
        var calc = new RPNCalculator<int>();
        calc.Push(0b11110000, null, null);

        // Not
        calc.DoUnaryOp(UnaryOperation.Not);
        Assert.Equal(~0b11110000, (int)calc.Peek().Object);
        calc.Clear();

        // PopCount
        calc.Push(0b11110000, null, null);
        calc.DoUnaryOp(UnaryOperation.PopCount);
        Assert.Equal(4, (int)calc.Peek().Object);
        calc.Clear();

        // CountLeadingZeroes
        calc.Push(0xF0, null, null);
        calc.DoUnaryOp(UnaryOperation.CountLeadingZeroes);
        Assert.Equal(24, (int)calc.Peek().Object);
        calc.Clear();

        // CountTrailingZeroes
        calc.Push(0xF0, null, null);
        calc.DoUnaryOp(UnaryOperation.CountTrailingZeroes);
        Assert.Equal(4, (int)calc.Peek().Object);
        calc.Clear();

        // MaskLeft(1) -> 0x80000000
        calc.Push(1, null, null);
        calc.DoUnaryOp(UnaryOperation.MaskLeft);
        Assert.Equal(int.MinValue, (int)calc.Peek().Object);
        calc.Clear();

        // MaskRight(1) -> 1
        calc.Push(1, null, null);
        calc.DoUnaryOp(UnaryOperation.MaskRight);
        Assert.Equal(1, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestExceptionSafety() {
        // Binary Op Exception (Divide by Zero)
        var calc = new RPNCalculator<int>();
        calc.Push(10, null, null);
        calc.Push(0, null, null);
        Assert.ThrowsAny<Exception>(() => calc.DoBinaryOp(BinaryOperation.UnsignedDivide, false, 0));
        Assert.ThrowsAny<Exception>(() => calc.DoBinaryOp(BinaryOperation.SignedDivide, false, 0));
        // Verify stack restored
        Assert.Equal(0, (int)calc.Peek().Object);
        calc.DoStackOp(StackOperation.Drop, 1);
        Assert.Equal(10, (int)calc.Peek().Object);

        // Unary Op Exception (Overflow in arg conversion)
        var calc128 = new RPNCalculator<Int128>();
        calc128.Push(Int128.MaxValue, null, null);
        Assert.ThrowsAny<Exception>(() => calc128.DoUnaryOp(UnaryOperation.MaskLeft));
        // Verify stack restored
        Assert.Equal(Int128.MaxValue, (Int128)calc128.Peek().Object);

        // Stack Op Exception (Extract out of range)
        calc.Clear();
        calc.Push(100, null, null);
        Assert.ThrowsAny<Exception>(() => calc.DoStackOp(StackOperation.Extract, null));
        // Verify stack restored
        Assert.Equal(100, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestConvertToException() {
        var calc = new RPNCalculator<int>();
        Assert.ThrowsAny<Exception>(() => calc.ConvertTo(typeof(float), false));
    }

    [Fact]
    public void TestPushInterface() {
        var calc = new RPNCalculator<int>();
        IStackEntry entry = new MockStackEntry { Object = 42, Comment = "c", AltComment = "ac" };
        calc.Push(entry);
        var top = calc.Peek();
        Assert.Equal(42, (int)top.Object);
        Assert.Equal("c", top.Comment);
    }

    [Fact]
    public void TestPeekEmpty() {
        var calc = new RPNCalculator<int>();
        Assert.ThrowsAny<Exception>(() => calc.Peek());
    }

    [Fact]
    public void TestAddCarry() {
        // Use byte for easy carry testing
        var calc = new RPNCalculator<sbyte>();

        // 255 + 1 = 0, Carry: 1
        calc.Push(255, null, null);
        calc.Push(1, null, null);
        var flags = calc.DoBinaryOp(BinaryOperation.AddCarry, false, 0);
        Assert.Equal(0, (sbyte)calc.Peek().Object);
        Assert.Equal(ResultFlags.Carry, flags);

        // 255 + 0 + carryIn(true) = 0, Carry: 1
        calc.Clear();
        calc.Push(255, null, null);
        calc.Push(0, null, null);
        flags = calc.DoBinaryOp(BinaryOperation.AddCarry, true, 0);
        Assert.Equal(0, (sbyte)calc.Peek().Object);
        Assert.Equal(ResultFlags.Carry, flags);

        // 255 + 255 + carryIn(true) = 255, Carry: 1
        calc.Clear();
        calc.Push(255, null, null);
        calc.Push(255, null, null);
        flags = calc.DoBinaryOp(BinaryOperation.AddCarry, true, 0);
        Assert.Equal(-1, (sbyte)calc.Peek().Object);
        Assert.Equal(ResultFlags.Carry, flags);

        // 127 + 1 = 128, Carry: 0 (but signed overflow if it were sbyte)
        calc.Clear();
        calc.Push(127, null, null);
        calc.Push(1, null, null);
        flags = calc.DoBinaryOp(BinaryOperation.AddCarry, false, 0);
        Assert.Equal(-128, (sbyte)calc.Peek().Object);
        Assert.Equal(ResultFlags.Overflow, flags);
    }

    [Fact]
    public void TestSubtractBorrow() {
        var calc = new RPNCalculator<sbyte>();

        // 0 - 1 = 255, Carry (Borrow): 1
        calc.Push(0, null, null);
        calc.Push(1, null, null);
        var flags = calc.DoBinaryOp(BinaryOperation.SubtractBorrow, false, 0);
        Assert.Equal(-1, (sbyte)calc.Peek().Object);
        Assert.Equal(ResultFlags.Carry, flags);

        // 0 - 0 - borrowIn(true) = 255, Carry (Borrow): 1
        calc.Clear();
        calc.Push(0, null, null);
        calc.Push(0, null, null);
        flags = calc.DoBinaryOp(BinaryOperation.SubtractBorrow, true, 0);
        Assert.Equal(-1, (sbyte)calc.Peek().Object);
        Assert.Equal(ResultFlags.Carry, flags);

        // 1 - 0 - borrowIn(true) = 0, Carry (Borrow): 0
        calc.Clear();
        calc.Push(1, null, null);
        calc.Push(0, null, null);
        flags = calc.DoBinaryOp(BinaryOperation.SubtractBorrow, true, 0);
        Assert.Equal(0, (sbyte)calc.Peek().Object);
        Assert.Equal(0, (int)flags);

        // 0 - 255 - borrowIn(true) = 0, Carry (Borrow): 1
        calc.Clear();
        calc.Push(0, null, null);
        calc.Push(255, null, null);
        flags = calc.DoBinaryOp(BinaryOperation.SubtractBorrow, true, 0);
        Assert.Equal(0, (sbyte)calc.Peek().Object);
        Assert.Equal(ResultFlags.Carry, flags);
    }

    [Fact]
    public void TestAddOverflow() {
        var calc = new RPNCalculator<sbyte>();

        // 127 + 1 = -128, Overflow: 1, Carry: 0
        calc.Push((sbyte)127, null, null);
        calc.Push((sbyte)1, null, null);
        var flags = calc.DoBinaryOp(BinaryOperation.Add, false, 0);
        Assert.Equal(-128, (sbyte)calc.Peek().Object);
        Assert.True(flags.HasFlag(ResultFlags.Overflow));
        Assert.False(flags.HasFlag(ResultFlags.Carry));

        // -128 + -1 = 127, Overflow: 1, Carry: 1
        calc.Clear();
        calc.Push((sbyte)-128, null, null);
        calc.Push((sbyte)-1, null, null);
        flags = calc.DoBinaryOp(BinaryOperation.Add, false, 0);
        Assert.Equal(127, (sbyte)calc.Peek().Object);
        Assert.True(flags.HasFlag(ResultFlags.Overflow));
        Assert.True(flags.HasFlag(ResultFlags.Carry));
    }
}
