using ProCalcCore;
using System.Text;

namespace ProCalcConsole;

class DisplayPage {
    readonly ProgramConfig _config;
    readonly object _initialValue;
    readonly Dictionary<(int Row, int Col), int> _bitCells = [];
    object _value;
    int? _clicked = null;
    bool _exit = false;

    public DisplayPage(ProgramConfig config, object initialValue) {
        _config = config;
        _initialValue = initialValue;
        _value = initialValue;
    }

    public object Value => _value;

    void FormatBinaryFancyRow(StringBuilder sb, UInt128 val, int startBit, int count, int row) {
        var col = 0;
        for (int i = count - 1; i >= 0; i--) {
            if (row >= 0) {
                _bitCells[(row, col)] = startBit + i;
            }
            sb.Append(((val >> (startBit + i)) & 1) != 0 ? '1' : '0');
            col++;
            sb.Append(' ');
            col++;
            if (i % 4 == 0) {
                sb.Append(' ');
                col++;
                if (i % 8 == 0) {
                    sb.Append("  ");
                    col += 2;
                }
            }
        }
    }

    void FormatBinaryFancy(StringBuilder sb, object value, int byteCount, int firstRow) {
        var bit = byteCount * 8;
        var val = CalculatorMath.ToUInt128Unsigned(value);
        var row = firstRow;

        while (bit >= 32) {
            sb.Append($"    {bit - 4,3}      {bit - 8,3}        {bit - 12,3}      {bit - 16,3}");
            sb.Append($"        {bit - 20,3}      {bit - 24,3}        {bit - 28,3}      {bit - 32,3}\n");
            row++;
            FormatBinaryFancyRow(sb, val, bit - 32, 32, row + 1);
            bit -= 32;
            if (bit > 0) {
                sb.Append("\n\n");
                row += 2;
            }
        }
        if (bit >= 16) {
            sb.Append($"    {bit - 4,-3}      {bit - 8,3}        {bit - 9,3}      {bit - 16,3}\n");
            row++;
            FormatBinaryFancyRow(sb, val, bit - 16, 16, row + 1);
            bit -= 16;
            if (bit > 0) {
                sb.Append("\n\n");
                row += 2;
            }
        }
        if (bit >= 8) {
            sb.Append($"    {bit - 4,3}      {bit - 8,3}\n");
            row++;
            bit -= 8;
            FormatBinaryFancyRow(sb, val, bit - 8, 8, row + 1);
            if (bit > 0) {
                sb.Append("\n\n");
            }
        }
    }

    // (object) cast is actually necessary here to prevent auto narrowing of the switch to Int128
#pragma warning disable IDE0004
    static object ToggleBit(object value, int bit) {
        return value switch {
            Int128 v => (object)(v ^ (Int128.One << bit)),
            long v => (object)(v ^ (1L << bit)),
            int v => (object)(v ^ (1 << bit)),
            short v => (object)(short)(v ^ (1 << bit)),
            sbyte v => (object)(sbyte)(v ^ (1 << bit)),
            _ => throw new InvalidCastException(),
        };
    }
#pragma warning restore IDE0004

    void Refresh() {
        var sb = new StringBuilder(1024);

        Console.Clear();
        ConsoleEx.Write("DISPLAY");
        ConsoleEx.Write("=======");

        ConsoleEx.Write("");
        ConsoleEx.Write("Hex:");
        sb.Clear();
        Numerics.FormatValueRaw(
            sb,
            _value,
            IntegerFormat.Hexadecimal,
            false,
            4,
            _config.PaddingMode,
            _config.Upper);
        ConsoleEx.Write(sb.ToString());

        ConsoleEx.Write("");
        ConsoleEx.Write("Dec (unsigned):");
        sb.Clear();
        Numerics.FormatValueRaw(
            sb,
            _value,
            IntegerFormat.Decimal,
            false,
            3,
            _config.PaddingMode,
            _config.Upper);
        ConsoleEx.Write(sb.ToString());
        ConsoleEx.Write("Dec (signed):");
        sb.Clear();
        Numerics.FormatValueRaw(
            sb,
            _value,
            IntegerFormat.Decimal,
            true,
            3,
            _config.PaddingMode,
            _config.Upper);
        ConsoleEx.Write(sb.ToString());

        ConsoleEx.Write("");
        ConsoleEx.Write("Oct:");
        sb.Clear();
        Numerics.FormatValueRaw(
            sb,
            _value,
            IntegerFormat.Octal,
            _config.Signed,
            3,
            _config.PaddingMode,
            _config.Upper);
        ConsoleEx.Write(sb.ToString());

        ConsoleEx.Write("");
        ConsoleEx.Write("Bin (Enter to save):");
        _bitCells.Clear();
        sb.Clear();
        FormatBinaryFancy(sb, _value, CalculatorMath.ByteCount(_value), Console.CursorTop);
        foreach (var line in sb.ToString().Split('\n'))
            ConsoleEx.Write(line);

        if (_clicked is int clicked) {
            ConsoleEx.Write("");
            ConsoleEx.Write("Clicked bit value:");
            sb.Clear();
            Numerics.FormatValueRaw(
                sb,
                Int128.One << clicked,
                IntegerFormat.Hexadecimal,
                false,
                4,
                _config.PaddingMode,
                _config.Upper);
            if (_config.ShowStackBase) {
                sb.Insert(0, "0x");
            }
            ConsoleEx.Write(sb.ToString());
        }
    }

    public void Run() {
        Refresh();
        while (!_exit) {
            var cin = ConsoleEx.ReadConsoleInput();
            switch (cin) {
                case ConsoleKeyInfo key:
                    HandleKey(key);
                    break;
                case ConsoleMouseClickInfo click:
                    HandleClick(click);
                    break;
                case ConsoleResizeInfo:
                    Refresh();
                    break;
            }
        }
    }

    void HandleKey(ConsoleKeyInfo key) {
        try {
            switch (key.Key) {
                case ConsoleKey.Z when key.Modifiers == ConsoleModifiers.Control:
                    _value = _initialValue;
                    Refresh();
                    break;
                case ConsoleKey.P when key.Modifiers == ConsoleModifiers.None:
                    new DisplayPage2(_config, _value).Run();
                    _value = _initialValue;
                    _exit = true;
                    break;
                case ConsoleKey.Enter when key.Modifiers == ConsoleModifiers.None:
                    _exit = true;
                    break;
                default:
                    _value = _initialValue;
                    _exit = true;
                    break;
            }
        } catch {
        }
    }

    void HandleClick(ConsoleMouseClickInfo click) {
        if (click.Modifiers != ConsoleModifiers.None)
            return;

        if (_bitCells.TryGetValue((click.Y, click.X), out var bit)) {
            if (click.IsButtonPressed(0))
                _value = ToggleBit(_value, bit);
            if (click.IsButtonPressed(0) || click.IsButtonPressed(-1)) {
                _clicked = bit;
                Refresh();
            }
        }
    }
}
