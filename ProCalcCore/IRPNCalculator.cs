namespace ProCalcCore;

public interface IRPNCalculator {
    int WordBytes { get; }
    int Count { get; }
    void Push(object value, string? comment, string? altComment);
    void Push(IStackEntry entry);
    IStackEntry Peek();
    void Clear();
    void DoBinaryOp(BinaryOperation op);
    void DoUnaryOp(UnaryOperation op);
    void DoStackOp(StackOperation op, int? input);
    IEnumerable<IStackEntry> GetStackItems(int max = int.MaxValue);
    IRPNCalculator ConvertTo(Type type, bool signExtend);
}
