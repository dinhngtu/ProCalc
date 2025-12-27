using ProCalcCore;

namespace ProCalcTests;

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
        calc.DoBinaryOp(BinaryOperation.Add);
        Assert.Equal(30, (int)calc.Peek().Object);
        Assert.Equal(1, calc.Count);
    }

    [Fact]
    public void TestSubtract() {
        var calc = new RPNCalculator<int>();
        Push(calc, 30);
        Push(calc, 10);
        calc.DoBinaryOp(BinaryOperation.Subtract);
        // 30 - 10 = 20. RPN: 30 10 -
        // First popped is 'b' (10), second is 'a' (30). Result = a - b.
        Assert.Equal(20, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestMultiply() {
        var calc = new RPNCalculator<int>();
        Push(calc, 5);
        Push(calc, 6);
        calc.DoBinaryOp(BinaryOperation.Multiply);
        Assert.Equal(30, (int)calc.Peek().Object);
    }

    [Fact]
    public void TestDivide() {
        var calc = new RPNCalculator<int>();
        Push(calc, 20);
        Push(calc, 4);
        calc.DoBinaryOp(BinaryOperation.Divide);
        Assert.Equal(5, (int)calc.Peek().Object);
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
        calc.DoBinaryOp(BinaryOperation.And);
        Assert.Equal(0b1000, (int)calc.Peek().Object); // 8
    }

    [Fact]
    public void TestShiftLeft() {
        var calc = new RPNCalculator<int>();
        Push(calc, 1);
        Push(calc, 2); // shift amount
        calc.DoBinaryOp(BinaryOperation.ShiftLeft);
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
        // Roll(1) -> Rotate(1) -> PopFront(3) and PushBack(3)
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
        // Roll(-1) -> Rotate(-1) -> PopBack(1) and PushFront(1)
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
        var calc32 = new RPNCalculator<int>();
        calc32.Push(0x12345678, null, null);

        var calc8 = calc32.Into<sbyte>(signExtend: true);
        Assert.Equal(0x78, (sbyte)calc8.Peek().Object);
    }
}
