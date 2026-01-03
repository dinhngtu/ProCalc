using System;
using System.Globalization;
using System.Numerics;
using System.Text;

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

    int GetShiftAmount(T bits) {
        var amount = int.CreateChecked(bits);
        if (amount < 0)
            throw new ArgumentException("Cannot shift by negative amounts");
        return amount;
    }

    T AlignUp(T value, T bits) {
        var mask = MaskRight(GetShiftAmount(bits));
        return (value + mask) & ~mask;
    }

    T AlignDown(T value, T bits) {
        var mask = MaskRight(GetShiftAmount(bits));
        return value & ~mask;
    }

    bool GetBit(T value, int bit) {
        return ((value >> bit) & T.One) != T.Zero;
    }

    public ResultFlags DoBinaryOp(BinaryOperation op, bool carryIn, ResultFlags prevFlags) {
        if (_stack.Count < 2)
            throw new InvalidOperationException("Not enough operands");

        var b = _stack.PopFront();
        var a = _stack.PopFront();
        T result;
        ResultFlags flags = 0;

        try {
            switch (op) {
                case BinaryOperation.Add:
                case BinaryOperation.AddCarry:
                    if (op == BinaryOperation.Add)
                        carryIn = false;
                    result = a.Value + b.Value + (carryIn ? T.One : T.Zero);
                    if (IntConverter.UnsignedLess(result, a.Value) || (carryIn && result == a.Value))
                        flags = ResultFlags.Carry;
                    if (((result ^ a.Value) & (result ^ b.Value)) < T.Zero)
                        flags |= ResultFlags.Overflow;
                    break;

                case BinaryOperation.Subtract:
                case BinaryOperation.SubtractBorrow:
                    if (op == BinaryOperation.Subtract)
                        carryIn = false;
                    result = a.Value - b.Value - (carryIn ? T.One : T.Zero);
                    if (IntConverter.UnsignedLess(a.Value, result) || (carryIn && result == a.Value))
                        flags = ResultFlags.Carry;
                    if (((a.Value ^ b.Value) & (result ^ a.Value)) < T.Zero)
                        flags |= ResultFlags.Overflow;
                    break;

                case BinaryOperation.Multiply:
                    if (IntConverter.UnsignedMultiplyOverflows(a.Value, b.Value))
                        flags = ResultFlags.Carry;
                    try {
                        result = checked(a.Value * b.Value);
                    }
                    catch (OverflowException) {
                        flags |= ResultFlags.Overflow;
                        result = a.Value * b.Value;
                    }
                    break;

                case BinaryOperation.UnsignedDivide:
                    result = IntConverter.UnsignedDivide(a.Value, b.Value);
                    break;

                case BinaryOperation.SignedDivide:
                    result = a.Value / b.Value;
                    break;

                case BinaryOperation.Remainder:
                    result = a.Value % b.Value;
                    break;

                case BinaryOperation.And:
                    flags = prevFlags;
                    result = a.Value & b.Value;
                    break;
                case BinaryOperation.Or:
                    flags = prevFlags;
                    result = a.Value | b.Value;
                    break;
                case BinaryOperation.Xor:
                    flags = prevFlags;
                    result = a.Value ^ b.Value;
                    break;

                case BinaryOperation.ShiftLeft: {
                        var amount = GetShiftAmount(b.Value);
                        flags = amount switch {
                            0 => prevFlags,
                            _ when amount == WordBytes * 8 => GetBit(a.Value, 0) ?
                                ResultFlags.Carry | ResultFlags.Overflow :
                                0,
                            _ when amount > WordBytes * 8 => 0,
                            _ => GetBit(a.Value, WordBytes * 8 - amount - 1) ?
                                ResultFlags.Carry | ResultFlags.Overflow :
                                0,
                        };
                        result = amount switch {
                            _ when amount >= WordBytes * 8 => T.Zero,
                            _ => a.Value << amount,
                        };
                        break;
                    }

                case BinaryOperation.ShiftRight: {
                        var amount = GetShiftAmount(b.Value);
                        flags = amount switch {
                            0 => prevFlags,
                            _ when amount == WordBytes * 8 => GetBit(a.Value, WordBytes * 8 - 1) ?
                                ResultFlags.Carry | ResultFlags.Overflow :
                                0,
                            _ when amount > WordBytes * 8 => 0,
                            _ => GetBit(a.Value, amount - 1) ?
                                ResultFlags.Carry | ResultFlags.Overflow :
                                0,
                        };
                        result = amount switch {
                            _ when amount >= WordBytes * 8 => T.Zero,
                            _ => a.Value >>> amount,
                        };
                        break;
                    }

                case BinaryOperation.ShiftRightArithmetic: {
                        var amount = GetShiftAmount(b.Value);
                        flags = amount switch {
                            0 => prevFlags,
                            _ when amount == WordBytes * 8 => GetBit(a.Value, WordBytes * 8 - 1) ?
                                ResultFlags.Carry | ResultFlags.Overflow :
                                0,
                            _ when amount > WordBytes * 8 => 0,
                            _ => GetBit(a.Value, amount - 1) ?
                                ResultFlags.Carry | ResultFlags.Overflow :
                                0,
                        };
                        result = amount switch {
                            _ when amount >= WordBytes * 8 => T.Zero,
                            _ => a.Value >> amount,
                        };
                        break;
                    }

                case BinaryOperation.RotateLeft: {
                        var amount = GetShiftAmount(b.Value);
                        flags = amount switch {
                            0 => prevFlags,
                            _ => GetBit(a.Value, WordBytes * 8 - (amount % (WordBytes * 8)) - 1) ?
                                ResultFlags.Carry | ResultFlags.Overflow :
                                0,
                        };
                        result = T.RotateLeft(a.Value, amount);
                        break;
                    }

                case BinaryOperation.RotateRight: {
                        var amount = GetShiftAmount(b.Value);
                        flags = amount switch {
                            0 => prevFlags,
                            _ => GetBit(a.Value, (amount + WordBytes * 8 - 1) % (WordBytes * 8)) ?
                                ResultFlags.Carry | ResultFlags.Overflow :
                                0,
                        };
                        result = T.RotateRight(a.Value, amount);
                        break;
                    }

                case BinaryOperation.AlignUp:
                    result = AlignUp(a.Value, b.Value);
                    break;
                case BinaryOperation.AlignDown:
                    result = AlignDown(a.Value, b.Value);
                    break;
                default:
                    throw new NotImplementedException();
            }
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
        return flags;
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
                UnaryOperation.Pow2 => T.One << int.CreateChecked(val.Value),
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

    static UInt128 ParseOctal(IEnumerable<char> v) {
        var result = UInt128.Zero;
        var eight = UInt128.CreateTruncating(8);
        foreach (var c in v) {
            var cv = "01234567".IndexOf(c);
            if (cv < 0)
                throw new InvalidDataException("Invalid octal string");
            unchecked {
                result = result * eight + UInt128.CreateChecked(cv);
            }
        }
        return result;
    }

    public static IStackEntry? Parse(ReadOnlySpan<char> input, IntegerFormat format, out char commentChar) {
        commentChar = '\0';

        var scratch = new StringBuilder();
        scratch.Append(input);
        if (scratch.Length == 0)
            return null;

        var commentIndex = input.IndexOfAny(';', ':');
        string? comment = null;
        if (commentIndex >= 0) {
            scratch.Remove(commentIndex, scratch.Length - commentIndex);
            comment = input[(commentIndex + 1)..].Trim().ToString();
            commentChar = input[commentIndex];
        }

        scratch.Replace(" ", null);
        var realFormat = format;
        var explicitFormat = false;
        var negative = false;

        if (scratch.Length == 0)
            throw new ArgumentException("Found a comment but input is empty");
        if (scratch[0] == '-') {
            negative = true;
            scratch.Remove(0, 1);
        }
        if (scratch.Length > 2) {
            explicitFormat = true;
            switch (new string([scratch[0], scratch[1]]).ToLowerInvariant()) {
                case "0x":
                case "0h":
                    realFormat = IntegerFormat.Hexadecimal;
                    break;
                case "0n":
                    realFormat = IntegerFormat.Decimal;
                    break;
                case "0o":
                case "0t":
                    realFormat = IntegerFormat.Octal;
                    break;
                case "0y":
                case "0b" when format != IntegerFormat.Hexadecimal:
                    realFormat = IntegerFormat.Binary;
                    break;
                default:
                    explicitFormat = false;
                    break;
            }
            if (explicitFormat)
                scratch.Remove(0, 2);
        }
        if (!explicitFormat) {
            explicitFormat = true;
            switch (char.ToLowerInvariant(scratch[^1])) {
                case 'x':
                case 'h':
                    realFormat = IntegerFormat.Hexadecimal;
                    break;
                case 'n':
                    realFormat = IntegerFormat.Decimal;
                    break;
                case 'o':
                case 't':
                    realFormat = IntegerFormat.Octal;
                    break;
                case 'y':
                    realFormat = IntegerFormat.Binary;
                    break;
                default:
                    explicitFormat = false;
                    break;
            }
            if (explicitFormat)
                scratch.Remove(scratch.Length - 1, 1);
        }
        var raw = realFormat switch {
            IntegerFormat.Hexadecimal => UInt128.Parse(scratch.ToString(), NumberStyles.AllowHexSpecifier, null),
            IntegerFormat.Decimal => UInt128.Parse(scratch.ToString(), NumberStyles.None, null),
            IntegerFormat.Octal => ParseOctal(scratch.ToString()),
            IntegerFormat.Binary => UInt128.Parse(scratch.ToString(), NumberStyles.AllowBinarySpecifier, null),
            _ => throw new NotImplementedException(),
        };
        if (negative) {
            return new StackEntry<T>() {
                Value = T.CreateTruncating(-raw),
                Comment = comment,
            };
        }
        else {
            return new StackEntry<T>() {
                Value = T.CreateTruncating(raw),
                Comment = comment,
            };
        }
    }

    public IStackEntry? ParseEntry(ReadOnlySpan<char> input, IntegerFormat format, out char commentChar) {
        return Parse(input, format, out commentChar);
    }
}
