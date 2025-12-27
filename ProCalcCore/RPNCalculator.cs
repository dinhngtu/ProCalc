using System.Numerics;

namespace ProCalcCore;

public class RPNCalculator<T> : IRPNCalculator
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T> {
    readonly Deque<T> _stack = new(256);

    public RPNCalculator() { }

    public RPNCalculator(IEnumerable<T> values) {
        foreach (var value in values) {
            _stack.PushFront(value);
        }
    }

    public int WordBytes => T.Zero.GetByteCount();
    int WordBits => WordBytes * 8;

    public int Count => _stack.Count;

    public void Push(object value) {
        _stack.PushFront(IntConverter.ToCalculatorTypeTruncating<T>(value));
    }

    public object Peek() {
        if (_stack.Count > 0)
            return _stack.PeekFront();
        else
            throw new InvalidOperationException("Not enough operands");
    }

    public void Clear() {
        _stack.Clear();
    }

    public void DoBinaryOp(BinaryOperation op) {
        if (_stack.Count < 2)
            throw new InvalidOperationException("Not enough operands");

        var b = _stack.PopFront();
        var a = _stack.PopFront();
        T result;

        try {
            result = op switch {
                BinaryOperation.Add => a + b,
                BinaryOperation.Subtract => a - b,
                BinaryOperation.Multiply => a * b,
                BinaryOperation.Divide => a / b,
                BinaryOperation.Remainder => a % b,
                BinaryOperation.And => a & b,
                BinaryOperation.Or => a | b,
                BinaryOperation.Xor => a ^ b,
                BinaryOperation.ShiftLeft => a << int.CreateChecked(b),
                BinaryOperation.ShiftRight => a >>> int.CreateChecked(b),
                BinaryOperation.ShiftRightArithmetic => a >> int.CreateChecked(b),
                BinaryOperation.RotateLeft => T.RotateLeft(a, int.CreateChecked(b)),
                BinaryOperation.RotateRight => T.RotateRight(a, int.CreateChecked(b)),
                _ => throw new NotSupportedException(nameof(op)),
            };
        }
        catch {
            _stack.PushFront(a);
            _stack.PushFront(b);
            throw;
        }
        _stack.PushFront(result);
    }

    T MaskLeft(int val) {
        return ((T.One << val) - T.One) << (WordBits - val);
    }

    T MaskRight(int val) {
        return (T.One << val) - T.One;
    }

    public void DoUnaryOp(UnaryOperation op) {
        if (_stack.Count < 1)
            throw new InvalidOperationException("Not enough operands");

        var val = _stack.PopFront();
        T result;

        try {
            result = op switch {
                UnaryOperation.Not => ~val,
                UnaryOperation.MaskLeft => MaskLeft(int.CreateChecked(val)),
                UnaryOperation.MaskRight => MaskRight(int.CreateChecked(val)),
                UnaryOperation.Popcount => T.PopCount(val),
                UnaryOperation.CountLeadingZeroes => T.LeadingZeroCount(val),
                UnaryOperation.CountTrailingZeroes => T.TrailingZeroCount(val),
                _ => throw new NotSupportedException(nameof(op)),
            };
        }
        catch {
            _stack.PushFront(val);
            throw;
        }

        _stack.PushFront(result);
    }

    void Drop(int count) {
        for (int i = 0; i < count && _stack.Count > 0; i++)
            _stack.PopFront();
    }

    void Rotate(int count) {
        if (_stack.Count < 2)
            return;

        for (int i = 0; i < Math.Abs(count); i++) {
            if (count > 0)
                _stack.PushBack(_stack.PopFront());
            else
                _stack.PushFront(_stack.PopBack());
        }
    }

    void Extract(int lifoIndex) {
        if (lifoIndex <= 1)
            return;

        if (lifoIndex > _stack.Count)
            throw new IndexOutOfRangeException();

        var temp = new List<T>(lifoIndex - 1);
        for (int i = 0; i < lifoIndex - 1; i++)
            temp.Add(_stack.PopFront());

        var target = _stack.PopFront();

        for (int i = temp.Count - 1; i >= 0; i--)
            _stack.PushFront(temp[i]);

        _stack.PushFront(target);
    }

    void Swap(int lifoIndex) {
        if (lifoIndex <= 1)
            return;
        (_stack[-1], _stack[-lifoIndex]) = (_stack[-lifoIndex], _stack[-1]);
    }

    public void DoStackOp(StackOperation op, int? input) {
        if (_stack.Count < 1)
            throw new InvalidOperationException("Not enough operands");

        int val = 0;
        T saved = T.Zero;
        if (input != null)
            val = input.Value;
        else
            saved = _stack.PopFront();
        try {
            if (input == null)
                val = int.CreateChecked(saved);
            switch (op) {
                case StackOperation.Drop:
                    Drop(val);
                    break;
                case StackOperation.Rotate:
                    Rotate(val);
                    break;
                case StackOperation.Extract:
                    Extract(val);
                    break;
                case StackOperation.Swap:
                    Swap(val);
                    break;
            }
        }
        catch {
            if (input == null)
                _stack.PushFront(saved);
            throw;
        }
    }

    public IEnumerable<object> GetStackItems(int max = int.MaxValue) {
        return _stack.EnumerateLifo(max).Cast<object>();
    }

    public RPNCalculator<U> Into<U>(bool signExtend = false)
        where U : struct, IBinaryInteger<U>, IMinMaxValue<U> {
        if (signExtend)
            return new RPNCalculator<U>(_stack.Select(U.CreateTruncating));
        else
            return new RPNCalculator<U>(_stack.Select(IntConverter.ToCalculatorTypeUnsigned<T, U>));
    }

    public IRPNCalculator ConvertTo(Type type, bool signExtend) {
        if (type == typeof(Int128))
            return Into<Int128>(signExtend);
        else if (type == typeof(long))
            return Into<long>(signExtend);
        else if (type == typeof(int))
            return Into<int>(signExtend);
        else if (type == typeof(short))
            return Into<short>(signExtend);
        else if (type == typeof(sbyte))
            return Into<sbyte>(signExtend);
        else
            throw new ArgumentOutOfRangeException(nameof(type));
    }
}
