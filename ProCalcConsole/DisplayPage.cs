using ProCalcCore;
using System.Text;

namespace ProCalcConsole;

class DisplayPage(ProgramConfig config, object value) {
    bool _exit = false;

    void FormatBinaryFancyRow(StringBuilder sb, UInt128 val, int startBit, int count) {
        for (int i = count - 1; i >= 0; i--) {
            sb.Append(((val >> (startBit + i)) & 1) != 0 ? '1' : '0');
            sb.Append(' ');
            if (i % 4 == 0) {
                sb.Append(' ');
                if (i % 8 == 0)
                    sb.Append("  ");
            }
        }
    }

    void FormatBinaryFancy(StringBuilder sb, object value, int byteCount) {
        var bit = byteCount * 8;
        var val = CalculatorMath.ToUInt128Unsigned(value);

        while (bit >= 32) {
            sb.Append($"    {bit - 4,3}      {bit - 8,3}        {bit - 12,3}      {bit - 16,3}");
            sb.Append($"        {bit - 20,3}      {bit - 24,3}        {bit - 28,3}      {bit - 32,3}\n");
            FormatBinaryFancyRow(sb, val, bit - 32, 32);
            bit -= 32;
            if (bit > 0)
                sb.Append("\n\n");
        }
        if (bit >= 16) {
            sb.Append($"    {bit - 4,-3}      {bit - 8,3}        {bit - 9,3}      {bit - 16,3}\n");
            FormatBinaryFancyRow(sb, val, bit - 16, 16);
            bit -= 16;
            if (bit > 0)
                sb.Append("\n\n");
        }
        if (bit >= 8) {
            sb.Append($"    {bit - 4,3}      {bit - 8,3}\n");
            bit -= 8;
            FormatBinaryFancyRow(sb, val, bit - 8, 8);
            if (bit > 0)
                sb.Append("\n\n");
        }
    }

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
            value,
            IntegerFormat.Hexadecimal,
            false,
            4,
            config.PaddingMode,
            config.Upper);
        ConsoleEx.Write(sb.ToString());

        ConsoleEx.Write("");
        ConsoleEx.Write("Dec (unsigned):");
        sb.Clear();
        Numerics.FormatValueRaw(
            sb,
            value,
            IntegerFormat.Decimal,
            false,
            3,
            config.PaddingMode,
            config.Upper);
        ConsoleEx.Write(sb.ToString());
        ConsoleEx.Write("Dec (signed):");
        sb.Clear();
        Numerics.FormatValueRaw(
            sb,
            value,
            IntegerFormat.Decimal,
            true,
            3,
            config.PaddingMode,
            config.Upper);
        ConsoleEx.Write(sb.ToString());

        ConsoleEx.Write("");
        ConsoleEx.Write("Oct:");
        sb.Clear();
        Numerics.FormatValueRaw(
            sb,
            value,
            IntegerFormat.Octal,
            config.Signed,
            3,
            config.PaddingMode,
            config.Upper);
        ConsoleEx.Write(sb.ToString());

        ConsoleEx.Write("");
        ConsoleEx.Write("Bin:");
        sb.Clear();
        FormatBinaryFancy(sb, value, CalculatorMath.ByteCount(value));
        foreach (var line in sb.ToString().Split('\n'))
            ConsoleEx.Write(line);
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
        try {
            if (key.Key == ConsoleKey.P && key.Modifiers == ConsoleModifiers.None) {
                new DisplayPage2(config, value).Run();
            }
        } catch {
        }
        _exit = true;
    }
}
