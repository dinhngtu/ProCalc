using ProCalcCore;

namespace ProCalcConsole;

class ProgramConfig {
    public IntegerFormat Format;
    public bool Signed;
    public InputTypes Type;
    public bool Grouping;
    public PaddingMode PaddingMode;
    public bool Upper;
    public bool ShowStackIndex;
    public bool ShowStackBase;
    public bool FakeNumpad;
    public bool ShowHints;
    public bool InputUsesCurrentBase;

    public static ProgramConfig CreateDefault() => new() {
        Format = IntegerFormat.Decimal,
        Signed = false,
        Type = InputTypes.Int64,
        Grouping = true,
        PaddingMode = PaddingMode.RightJustified,
        Upper = true,
        ShowStackIndex = true,
        ShowStackBase = false,
        FakeNumpad = false,
        ShowHints = false,
        InputUsesCurrentBase = true,
    };

    public static string CommandLineHelp => """
        Command line parameters:
        
        -hex/-dec/-oct/-bin     Set display format
        -type <type>            Set type (e.g. s32, u64)
        -[no]group              Set digit grouping
        -padding <padding>      Set padding mode
        -upper/-lower           Set hexadecimal case
        -[no]index              Show stack index
        -[no]base               Show stack base
        -[no]numpad             Enable fake numpad
        -[no]hints              Show status hints
        -[no]inputbase          Input uses current base
        -?                      Show this message
        """;

    public static bool ParseCommandLineArgs(string[] args, out ProgramConfig config) {
        config = CreateDefault();

        for (int i = 0; i < args.Length; i++) {
            var arg = args[i];

            if ("-hex".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.Format = IntegerFormat.Hexadecimal;
            }
            else if ("-dec".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.Format = IntegerFormat.Decimal;
            }
            else if ("-oct".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.Format = IntegerFormat.Octal;
            }
            else if ("-bin".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.Format = IntegerFormat.Binary;
            }

            else if ("-type".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                var typeName = args[++i];
                var typeSize = typeName[1..] switch {
                    "8" => InputTypes.Int8,
                    "16" => InputTypes.Int16,
                    "32" => InputTypes.Int32,
                    "64" => InputTypes.Int64,
                    "128" => InputTypes.Int128,
                    _ => throw new ArgumentException($"Invalid -type argument {typeName}"),
                };
                config.Signed = char.ToLowerInvariant(typeName[0]) switch {
                    's' => true,
                    'u' => false,
                    _ => throw new ArgumentException($"Invalid -type argument {typeName}"),
                };
                config.Type = typeSize;
            }

            else if ("-group".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.Grouping = true;
            }
            else if ("-nogroup".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.Grouping = false;
            }

            else if ("-padding".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.PaddingMode = Enum.Parse<PaddingMode>(args[++i], ignoreCase: true);
            }

            else if ("-upper".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.Upper = true;
            }
            else if ("-lower".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.Upper = false;
            }

            else if ("-index".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.ShowStackIndex = true;
            }
            else if ("-noindex".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.ShowStackIndex = false;
            }

            else if ("-base".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.ShowStackBase = true;
            }
            else if ("-nobase".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.ShowStackBase = false;
            }

            else if ("-numpad".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.FakeNumpad = true;
            }
            else if ("-nonumpad".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.FakeNumpad = false;
            }

            else if ("-hints".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.ShowHints = true;
            }
            else if ("-nohints".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.ShowHints = false;
            }

            else if ("-inputbase".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.InputUsesCurrentBase = true;
            }
            else if ("-noinputbase".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                config.InputUsesCurrentBase = false;
            }

            else if ("-?".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            else {
                throw new ArgumentException($"Invalid argument {arg}");
            }
        }

        return true;
    }
}
