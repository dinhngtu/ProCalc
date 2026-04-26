using System.Text;

namespace ProCalcConsole;

static class ConsoleEx {
    public static object ReadConsoleInput() {
        if (PlatformGuards.IsWindows)
            return WindowsConsole.ReadConsoleInput();
        else
            return Console.ReadKey(true);
    }

    public static void Write(ReadOnlySpan<char> text, int width = -1, int scroll = 0, int totalLength = -1) {
        if (width < 0)
            width = Console.WindowWidth;
        if (totalLength < 0)
            totalLength = text.Length;

        var display = new StringBuilder();
        if (scroll > 0)
            display.Append('<');
        display.Append(text);
        if (totalLength > scroll + text.Length)
            display.Append('>');

        if (display.Length > width) {
            display.Remove(width - 1, display.Length - width + 1);
            display.Append('>');
        }
        else {
            display.Append(' ', width - display.Length);
        }
        Console.Write(display.ToString());
    }

    public static void Pause() {
        Console.SetCursorPosition(0, Console.WindowHeight - 1);
        Console.Write("Press any key...");
        while (true)
            if (ReadConsoleInput() is ConsoleKeyInfo)
                break;
    }
}
