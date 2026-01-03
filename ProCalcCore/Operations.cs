namespace ProCalcCore;

public enum BinaryOperation {
    Add,
    AddCarry,
    Subtract,
    SubtractBorrow,
    Multiply,
    UnsignedDivide,
    SignedDivide,
    Remainder,
    And,
    Or,
    Xor,
    ShiftLeft,
    ShiftRight,
    ShiftRightArithmetic,
    RotateLeft,
    RotateLeftCarry,
    RotateRight,
    RotateRightCarry,
    AlignUp,
    AlignDown,
}

public enum UnaryOperation {
    Not,
    MaskLeft,
    MaskRight,
    PopCount,
    CountLeadingZeroes,
    CountTrailingZeroes,
    Pow2,
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
