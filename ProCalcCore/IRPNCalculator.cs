namespace ProCalcCore;

public interface IRPNCalculator {
    int WordBytes { get; }
    int Count { get; }
    void Push(object value, string? comment, string? altComment);
    void Push(IStackEntry entry);
    IStackEntry Peek();
    void Clear();
    ResultFlags DoBinaryOp(BinaryOperation op, bool carryIn, ResultFlags prevFlags);
    void DoUnaryOp(UnaryOperation op);
    void DoStackOp(StackOperation op, int? input);
    IEnumerable<IStackEntry> GetStackItems(int max = int.MaxValue);
    IRPNCalculator ConvertTo(Type type, bool signExtend);
    IStackEntry? ParseEntry(ReadOnlySpan<char> input, IntegerFormat format, out char commentChar);
}
