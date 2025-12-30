using System.Numerics;

namespace ProCalcCore;

public class RPNCalculator<T> : IRPNCalculator
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T> {
    public const int DefaultCapacity = 256;

    readonly Deque<StackEntry<T>> _stack;

    public RPNCalculator(int capacity = DefaultCapacity) {
        _stack = new(capacity);
    }

    internal RPNCalculator(IEnumerable<StackEntry<T>> values, int capacity = DefaultCapacity) {
        _stack = new(capacity);
        foreach (var value in values) {
            _stack.PushFront(value);
        }
    }

    public int WordBytes => T.Zero.GetByteCount();

    public int Count => _stack.Count;

    public void Push(object value, string? comment, string? altComment) {
        _stack.PushFront(new StackEntry<T>() {
            Value = IntConverter.ToCalculatorTypeTruncating<T>(value),
            Comment = comment,
            AltComment = altComment,
        });
    }

    public void Push(IStackEntry value) {
        _stack.PushFront(new StackEntry<T>() {
            Value = IntConverter.ToCalculatorTypeTruncating<T>(value.Object),
            Comment = value.Comment,
            AltComment = value.AltComment,
        });
    }

    public IStackEntry Peek() {
        if (_stack.Count > 0)
            return _stack.PeekFront();
        else
            throw new InvalidOperationException("Not enough operands");
    }

    public void Clear() {
        _stack.Clear();
    }

    T AlignUp(T value, T bits) {
        var mask = MaskRight(int.CreateChecked(bits));
        return (value + mask) & ~mask;
    }

    T AlignDown(T value, T bits) {
        var mask = MaskRight(int.CreateChecked(bits));
        return value & ~mask;
    }

    public void DoBinaryOp(BinaryOperation op) {
        if (_stack.Count < 2)
            throw new InvalidOperationException("Not enough operands");

        var b = _stack.PopFront();
        var a = _stack.PopFront();
        T result;

        try {
            result = op switch {
                BinaryOperation.Add => a.Value + b.Value,
                BinaryOperation.Subtract => a.Value - b.Value,
                BinaryOperation.Multiply => a.Value * b.Value,
                BinaryOperation.Divide => a.Value / b.Value,
                BinaryOperation.Remainder => a.Value % b.Value,
                BinaryOperation.And => a.Value & b.Value,
                BinaryOperation.Or => a.Value | b.Value,
                BinaryOperation.Xor => a.Value ^ b.Value,
                BinaryOperation.ShiftLeft => a.Value << int.CreateChecked(b.Value),
                BinaryOperation.ShiftRight => a.Value >>> int.CreateChecked(b.Value),
                BinaryOperation.ShiftRightArithmetic => a.Value >> int.CreateChecked(b.Value),
                BinaryOperation.RotateLeft => T.RotateLeft(a.Value, int.CreateChecked(b.Value)),
                BinaryOperation.RotateRight => T.RotateRight(a.Value, int.CreateChecked(b.Value)),
                BinaryOperation.AlignUp => AlignUp(a.Value, b.Value),
                BinaryOperation.AlignDown => AlignDown(a.Value, b.Value),
                _ => throw new NotSupportedException(nameof(op)),
            };
        }
        catch {
            _stack.PushFront(a);
            _stack.PushFront(b);
            throw;
        }
        _stack.PushFront(new StackEntry<T>() {
            Value = result,
            Comment = a.Comment,
            AltComment = b.Comment,
        });
    }

    T MaskLeft(int val) {
        return ((T.One << val) - T.One) << (WordBytes * 8 - val);
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
                UnaryOperation.Not => ~val.Value,
                UnaryOperation.MaskLeft => MaskLeft(int.CreateChecked(val.Value)),
                UnaryOperation.MaskRight => MaskRight(int.CreateChecked(val.Value)),
                UnaryOperation.PopCount => T.PopCount(val.Value),
                UnaryOperation.CountLeadingZeroes => T.LeadingZeroCount(val.Value),
                UnaryOperation.CountTrailingZeroes => T.TrailingZeroCount(val.Value),
                _ => throw new NotSupportedException(nameof(op)),
            };
        }
        catch {
            _stack.PushFront(val);
            throw;
        }

        _stack.PushFront(new StackEntry<T>() {
            Value = result,
            Comment = val.Comment,
            AltComment = val.AltComment,
        });
    }

    void Drop(int count) {
        for (int i = 0; i < count && _stack.Count > 0; i++)
            _stack.PopFront();
    }

    void Roll(int count) {
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

        var temp = new List<StackEntry<T>>(lifoIndex - 1);
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

    void Pick(int lifoIndex) {
        _stack.PushFront(new StackEntry<T>() {
            Value = _stack[-lifoIndex].Value,
            Comment = null,
            AltComment = null,
        });
    }

    void SetComment(int lifoIndex, string? comment) {
        var newValue = _stack[-lifoIndex];
        newValue.AltComment = newValue.Comment;
        newValue.Comment = comment;
        _stack[-lifoIndex] = newValue;
    }

    void SwapComment(int lifoIndex) {
        var newValue = _stack[-lifoIndex];
        (newValue.Comment, newValue.AltComment) = (newValue.AltComment, newValue.Comment);
        _stack[-lifoIndex] = newValue;
    }

    public void DoStackOp(StackOperation op, int? input) {
        if (_stack.Count < 1)
            throw new InvalidOperationException("Not enough operands");

        int val = 0;
        StackEntry<T> saved = default;
        if (input != null)
            val = input.Value;
        else
            saved = _stack.PopFront();
        try {
            if (input == null)
                val = int.CreateChecked(saved.Value);
            switch (op) {
                case StackOperation.Drop:
                    Drop(val);
                    break;
                case StackOperation.Roll:
                    Roll(val);
                    break;
                case StackOperation.Extract:
                    Extract(val);
                    break;
                case StackOperation.Swap:
                    Swap(val);
                    break;
                case StackOperation.Pick:
                    Pick(val);
                    break;
                case StackOperation.SetComment:
                    if (input != null)
                        throw new InvalidOperationException("Requires argument from stack");
                    SetComment(val, saved.Comment);
                    break;
                case StackOperation.SwapComment:
                    SwapComment(val);
                    break;
            }
        }
        catch {
            if (input == null)
                _stack.PushFront(saved);
            throw;
        }
    }

    public IEnumerable<IStackEntry> GetStackItems(int max = int.MaxValue) {
        return _stack.EnumerateLifo(max).Cast<IStackEntry>();
    }

    public RPNCalculator<U> Into<U>(bool signExtend = false)
        where U : struct, IBinaryInteger<U>, IMinMaxValue<U> {
        if (signExtend)
            return new RPNCalculator<U>(_stack.Select(x => new StackEntry<U>() {
                Value = U.CreateTruncating(x.Value),
                Comment = x.Comment,
                AltComment = x.AltComment,
            }), _stack.Capacity);
        else
            return new RPNCalculator<U>(_stack.Select(x => new StackEntry<U>() {
                Value = IntConverter.ToCalculatorTypeUnsigned<T, U>(x.Value),
                Comment = x.Comment,
                AltComment = x.AltComment,
            }), _stack.Capacity);
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
