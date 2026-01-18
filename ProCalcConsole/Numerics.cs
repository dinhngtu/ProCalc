using ProCalcCore;
using System.Text;

namespace ProCalcConsole;

static class Numerics {
    static int GetPadSize(PaddingMode paddingMode, int byteCount) {
        if (paddingMode != PaddingMode.ZeroPadded)
            return 0;
        return byteCount;
    }

    static void FormatOctalRaw(
        StringBuilder sb,
        object value,
        PaddingMode paddingMode,
        int byteCount) {
        var size = GetPadSize(PaddingMode.ZeroPadded, byteCount);

        var val = CalculatorMath.ToUInt128Unsigned(value);
        var pad = (size * 8 + 2) / 3;
        var begin = sb.Length;
        for (int i = pad - 1; i >= 0; i--)
            sb.Append("01234567"[(int)(val >> (i * 3) & 7)]);
        if (paddingMode != PaddingMode.ZeroPadded)
            while (sb.Length > begin + 1 && sb[begin] == '0')
                sb.Remove(begin, 1);
    }

    static void ApplyGrouping(StringBuilder sb, int groupSize) {
        if (groupSize <= 0 || sb.Length <= groupSize) return;

        int start = (sb.Length > 0 && sb[0] == '-') ? 1 : 0;
        int contentLen = sb.Length - start;
        int separators = (contentLen - 1) / groupSize;
        if (separators <= 0) return;

        int oldLen = sb.Length;
        int newLen = oldLen + separators;
        sb.Length = newLen;

        int readPtr = oldLen - 1;
        int writePtr = newLen - 1;
        int count = 0;

        while (readPtr >= start) {
            sb[writePtr--] = sb[readPtr--];
            count++;
            if (count == groupSize && readPtr >= start) {
                sb[writePtr--] = ' ';
                count = 0;
            }
        }
    }

    public static void FormatValueRaw(
        StringBuilder sb,
        object value,
        IntegerFormat format,
        bool signed,
        int group,
        PaddingMode paddingMode,
        bool upper) {

        if (format == IntegerFormat.Octal) {
            FormatOctalRaw(sb, value, paddingMode, CalculatorMath.ByteCount(value));
        }
        else {
            var size = GetPadSize(paddingMode, CalculatorMath.ByteCount(value));

            if (!signed)
                value = CalculatorMath.ToUInt128Unsigned(value);

            sb.AppendFormat(format switch {
                IntegerFormat.Hexadecimal => upper ? $"{{0:X{size * 2}}}" : $"{{0:x{size * 2}}}",
                IntegerFormat.Decimal => "{0:G}",
                IntegerFormat.Binary => $"{{0:B{size * 8}}}",
                _ => throw new InvalidOperationException(),
            }, value);
        }

        if (group > 0)
            ApplyGrouping(sb, group);
    }
}
