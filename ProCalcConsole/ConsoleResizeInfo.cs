using Windows.Win32;

namespace ProCalcConsole;

readonly struct ConsoleResizeInfo(short x, short y) {
    public int Width => x;
    public int Height => y;
}

readonly struct ConsoleMouseClickInfo(short x, short y, uint buttons, uint ctrl) {
    public int X => x;
    public int Y => y;
    public bool IsButtonPressed(int buttonIndex) {
        ArgumentOutOfRangeException.ThrowIfNegative(buttonIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(buttonIndex, 32);
        return buttonIndex switch {
            0 => (buttons & (1 << 0)) != 0,
            31 => (buttons & (1 << 1)) != 0,
            1 => (buttons & (1 << 2)) != 0,
            2 => (buttons & (1 << 3)) != 0,
            int i => (buttons & (1 << (i + 1))) != 0,
        };
    }
    public ConsoleModifiers Modifiers { get; } =
        ((ctrl & PInvoke.SHIFT_PRESSED) != 0 ? ConsoleModifiers.Shift : 0) |
        ((ctrl & (PInvoke.LEFT_ALT_PRESSED | PInvoke.RIGHT_ALT_PRESSED)) != 0 ? ConsoleModifiers.Alt : 0) |
        ((ctrl & (PInvoke.LEFT_CTRL_PRESSED | PInvoke.RIGHT_CTRL_PRESSED)) != 0 ? ConsoleModifiers.Control : 0);
}
