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
    PopCount,
    CountLeadingZeroes,
    CountTrailingZeroes,
}

public enum StackOperation {
    Drop,
    Roll,
    Extract,
    Swap,
    Pick,
    SetComment,
    SwapComment,
}
