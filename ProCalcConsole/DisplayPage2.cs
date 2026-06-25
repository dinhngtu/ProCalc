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
            var separatorIndex = text[..searchLength].LastIndexOf(separator);
            var breakIndex = separatorIndex;
            var nextIndex = separatorIndex + separator.Length;

            var dashIndex = text[..Math.Min(width, text.Length)].LastIndexOf('-');
            if (dashIndex >= 0 && dashIndex + 1 > breakIndex) {
                breakIndex = dashIndex + 1;
                nextIndex = breakIndex;
            }

            if (breakIndex <= 0) {
                ConsoleEx.Write(text[..width]);
                text = text[width..];
                continue;
            }

            ConsoleEx.Write(text[..breakIndex]);
            text = text[nextIndex..];
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

        bool foundBits = false;
        int first = -1, last = -1;
        var num = new StringBuilder();
        for (int i = 127; i >= -1; i--) {
            if (i >= 0 && (bin & (UInt128.One << i)) != UInt128.Zero) {
                if (first < 0) {
                    first = i;
                }
                last = i;
            } else {
                if (last >= 0) {
                    if (foundBits) {
                        sb.Append(" ; ");
                    }

                    num.Clear();
                    Numerics.FormatValueRaw(num, Int128.One << first, IntegerFormat.Hexadecimal, false, 4, PaddingMode.None, config.Upper);
                    sb.AppendFormat("{0}({1})", num, first);

                    if (last != first) {
                        sb.Append('-');
                        num.Clear();
                        Numerics.FormatValueRaw(num, Int128.One << last, IntegerFormat.Hexadecimal, false, 4, PaddingMode.None, config.Upper);
                        sb.AppendFormat("{0}({1})", num, last);
                    }

                    foundBits = true;
                }
                first = last = -1;
            }
        }
        if (foundBits) {
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
