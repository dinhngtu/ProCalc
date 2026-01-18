using ProCalcCore;
using System.Text;

namespace ProCalcConsole;

class DisplayPage(ProgramConfig config) {
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
            sb.Append($"{bit - 1,-3}          {bit - 8,3}    {bit - 9,-3}          {bit - 16,3}    ");
            sb.Append($"{bit - 17,-3}          {bit - 24,3}    {bit - 25,-3}          {bit - 32,3}\n");
            FormatBinaryFancyRow(sb, val, bit - 32, 32);
            sb.Append("\n\n");
            bit -= 32;
        }
        if (bit >= 16) {
            sb.Append($"{bit - 1,-3}          {bit - 8,3}    {bit - 9,-3}          {bit - 16,3}\n");
            FormatBinaryFancyRow(sb, val, bit - 16, 16);
            sb.Append("\n\n");
            bit -= 16;
        }
        if (bit >= 8) {
            sb.Append($"{bit - 1,-3}          {bit - 8,3}\n");
            FormatBinaryFancyRow(sb, val, bit - 8, 8);
            sb.Append("\n\n");
        }
    }

    public void Run(object value) {
        var sb = new StringBuilder(1024);

        Console.Clear();

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

        ConsoleEx.Pause();
    }
}
