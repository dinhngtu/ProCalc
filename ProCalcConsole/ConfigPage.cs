namespace ProCalcConsole;

class ConfigPage {
    class Setting {
        public required string Name { get; set; }
        public required Func<ProgramConfig, string> Getter { get; set; }
        public required Action<ProgramConfig> Toggle { get; set; }
    }

    public ProgramConfig Config { get; }

    int _active = 0;
    bool _exit = false;

    static readonly IReadOnlyList<Setting> Settings = [
        new() {
            Name = "Group digits",
            Getter = pc => pc.Grouping ? "x" : " ",
            Toggle = pc => pc.Grouping = !pc.Grouping,
        },
        new() {
            Name = "Number alignment",
            Getter = pc => pc.PaddingMode switch {
                PaddingMode.None => "Left ",
                PaddingMode.RightJustified => "Right",
                PaddingMode.ZeroPadded => "Pad  ",
                _ => "?",
            },
            Toggle = pc => pc.PaddingMode = pc.PaddingMode switch {
                PaddingMode.None => PaddingMode.RightJustified,
                PaddingMode.RightJustified => PaddingMode.ZeroPadded,
                PaddingMode.ZeroPadded => PaddingMode.None,
                _ => PaddingMode.None,
            },
        },
        new() {
            Name = "Uppercase hexadecimals",
            Getter = pc => pc.Upper ? "x" : " ",
            Toggle = pc => pc.Upper = !pc.Upper,
        },
        new() {
            Name = "Show stack index",
            Getter = pc => pc.ShowStackIndex ? "x" : " ",
            Toggle = pc => pc.ShowStackIndex= !pc.ShowStackIndex,
        },
        new() {
            Name = "Show stack base",
            Getter = pc => pc.ShowStackBase ? "x" : " ",
            Toggle = pc => pc.ShowStackBase = !pc.ShowStackBase,
        },
        new() {
            Name = "Input uses current base",
            Getter = pc => pc.InputUsesCurrentBase ? "x" : " ",
            Toggle = pc => pc.InputUsesCurrentBase = !pc.InputUsesCurrentBase,
        },
        new() {
            Name = "Keyboard as numpad",
            Getter = pc => pc.FakeNumpad ? "x" : " ",
            Toggle = pc => pc.FakeNumpad = !pc.FakeNumpad,
        },
        new() {
            Name = "Show hints",
            Getter = pc => pc.ShowHints ? "x" : " ",
            Toggle = pc => pc.ShowHints = !pc.ShowHints,
        },
        new() {
            Name = "Auto-dismiss errors",
            Getter = pc => pc.AutoDismissErrors ? "x" : " ",
            Toggle = pc => pc.AutoDismissErrors = !pc.AutoDismissErrors,
        },
        new() {
            Name = "Show last stack",
            Getter = pc => pc.ShowLastStack ? "x" : " ",
            Toggle = pc => pc.ShowLastStack = !pc.ShowLastStack,
        },
    ];

    public ConfigPage(ProgramConfig config) {
        Config = config;
    }

    public void Run() {
        bool previous = true;
        if (ConsoleEx.IsWindows)
            previous = Console.CursorVisible;
        try {
            Console.CursorVisible = false;
            Console.Clear();
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
        finally {
            if (ConsoleEx.IsWindows)
                Console.CursorVisible = previous;
            else
                Console.CursorVisible = true;
        }
    }

    void HandleKey(ConsoleKeyInfo key) {
        try {
            switch (key.Key) {
                case ConsoleKey.UpArrow when key.Modifiers == ConsoleModifiers.None:
                    _active = Math.Max(_active - 1, 0);
                    break;
                case ConsoleKey.DownArrow when key.Modifiers == ConsoleModifiers.None:
                    _active = Math.Min(_active + 1, Settings.Count - 1);
                    break;
                case ConsoleKey.Home when key.Modifiers == ConsoleModifiers.None:
                case ConsoleKey.PageUp when key.Modifiers == ConsoleModifiers.None:
                    _active = 0;
                    break;
                case ConsoleKey.End when key.Modifiers == ConsoleModifiers.None:
                case ConsoleKey.PageDown when key.Modifiers == ConsoleModifiers.None:
                    _active = Settings.Count - 1;
                    break;
                case ConsoleKey.Spacebar when key.Modifiers == ConsoleModifiers.None:
                    Settings[_active].Toggle(Config);
                    break;
                case ConsoleKey.Q when key.Modifiers == ConsoleModifiers.None:
                case ConsoleKey.D when key.Modifiers == ConsoleModifiers.Control:
                case ConsoleKey.Escape when key.Modifiers == ConsoleModifiers.None:
                    _exit = true;
                    break;
                default:
                    throw new NotSupportedException(string.Format(
                        "Unknown key: {0}{1}{2}{3}",
                        key.Modifiers.HasFlag(ConsoleModifiers.Control) ? "Ctrl+" : "",
                        key.Modifiers.HasFlag(ConsoleModifiers.Alt) ? "Alt+" : "",
                        key.Modifiers.HasFlag(ConsoleModifiers.Shift) ? "Shift+" : "",
                        key.Key.ToString()));
            }
            Refresh();
        }
        catch (Exception ex) {
            Refresh(ex);
        }
    }

    void Refresh(Exception? ex = null) {
        int width = Console.WindowWidth, height = Console.WindowHeight;
        ConsoleColor fg = Console.ForegroundColor, bg = Console.BackgroundColor;
        Console.SetCursorPosition(0, 0);
        for (int i = 0; i < Settings.Count; i++) {
            if (i == _active)
                (Console.ForegroundColor, Console.BackgroundColor) = (bg, fg);
            try {
                ConsoleEx.Write(string.Format("{0,-8} {1}", $"[{Settings[i].Getter(Config)}]", Settings[i].Name));
            }
            finally {
                if (i == _active)
                    (Console.ForegroundColor, Console.BackgroundColor) = (fg, bg);
            }
        }
        if (ex != null) {
            Console.SetCursorPosition(0, height - 1);
            ConsoleEx.Write(ex.Message, width: width - 1);
            Console.Beep();
        }
    }
}
