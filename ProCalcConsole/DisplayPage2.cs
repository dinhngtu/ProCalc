using ProCalcCore;
using System.Text;

namespace ProCalcConsole;

class DisplayPage2(ProgramConfig config, object value) {
    bool _exit = false;

    static void WriteWrappedAtSeparator(ReadOnlySpan<char> text, int width) {
        const string separator = " ; ";

        if (width <= 0) {
            ConsoleEx.Write(text);
            return;
        }

        while (text.Length > width) {
            var searchLength = Math.Min(width + separator.Length, text.Length);
            var breakIndex = text[..searchLength].LastIndexOf(separator);

            if (breakIndex <= 0) {
                ConsoleEx.Write(text[..width]);
                text = text[width..];
                continue;
            }

            ConsoleEx.Write(text[..breakIndex]);
            text = text[(breakIndex + separator.Length)..];
        }

        if (text.Length > 0)
            ConsoleEx.Write(text);
    }

    void Refresh() {
        var bin = CalculatorMath.ToUInt128Unsigned(value);
        var sb = new StringBuilder();

        Console.Clear();
        ConsoleEx.Write("DISPLAY 2");
        ConsoleEx.Write("=========");

        bool first = true;
        for (int i = 127; i >= 0; i--) {
            var pos = UInt128.One << i;
            if ((bin & pos) != UInt128.Zero) {
                if (!first) {
                    sb.Append(" ; ");
                }
                var num = new StringBuilder();
                Numerics.FormatValueRaw(num, unchecked((Int128)pos), IntegerFormat.Hexadecimal, false, 4, PaddingMode.None, config.Upper);
                sb.Append(num);
                first = false;
            }
        }
        if (!first) {
            ConsoleEx.Write("");
            ConsoleEx.Write("Bits:");
            WriteWrappedAtSeparator(sb.ToString(), Console.WindowWidth);
        }

        ConsoleEx.Write("");
        switch (value) {
            case long v: {
                    double f = BitConverter.Int64BitsToDouble(v);
                    ConsoleEx.Write("Double:");
                    ConsoleEx.Write(f.ToString(config.Upper ? "R" : "r"));
                    break;
                }
            case int v: {
                    float f = BitConverter.Int32BitsToSingle(v);
                    ConsoleEx.Write("Single:");
                    ConsoleEx.Write(f.ToString(config.Upper ? "R" : "r"));
                    break;
                }
            case short v: {
                    Half f = BitConverter.Int16BitsToHalf(v);
                    ConsoleEx.Write("Half:");
                    ConsoleEx.Write(f.ToString(config.Upper ? "R" : "r"));
                    break;
                }
            default:
                break;
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
                case ConsoleResizeInfo:
                    Refresh();
                    break;
            }
        }
    }

    void HandleKey(ConsoleKeyInfo key) {
        _exit = true;
    }
}
