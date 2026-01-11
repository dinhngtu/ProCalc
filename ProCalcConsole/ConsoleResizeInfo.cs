namespace ProCalcConsole;

readonly struct ConsoleResizeInfo(short x, short y) {
    public int Width => x;
    public int Height => y;
}
