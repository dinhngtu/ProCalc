namespace ProCalcCore;

public interface IRPNCalculator {
    int WordBytes { get; }
    int Count { get; }
    void Push(object value);
    object Peek();
    void Clear();
    void DoBinaryOp(BinaryOperation op);
    void DoUnaryOp(UnaryOperation op);
    void DoStackOp(StackOperation op, int? input);
    IEnumerable<object> GetStackItems(int max = int.MaxValue);
    IRPNCalculator ConvertTo(Type type, bool signExtend);
}
