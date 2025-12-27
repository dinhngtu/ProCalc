namespace ProCalcCore;

public enum BinaryOperation {
    Add,
    Subtract,
    Multiply,
    Divide,
    Remainder,
    And,
    Or,
    Xor,
    ShiftLeft,
    ShiftRight,
    ShiftRightArithmetic,
    RotateLeft,
    RotateRight,
}

public enum UnaryOperation {
    Not,
    MaskLeft,
    MaskRight,
    Popcount,
    CountLeadingZeroes,
    CountTrailingZeroes,
}

public enum StackOperation {
    Drop,
    Rotate,
    Extract,
    Swap,
    SetComment,
    SwapComment,
}
