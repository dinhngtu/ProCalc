namespace ProCalcConsole;

[Flags]
enum RefreshFlags : uint {
    None = 0,
    Status = 1,
    Stack = 2,
    Input = 4,
    Clear = 0x80000000,
    Screen = 0x7fffffff,
}
