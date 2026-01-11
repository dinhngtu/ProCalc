using ProCalcConsole;
using ProCalcCore;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;

[SupportedOSPlatform("windows")]
class Program {
    IRPNCalculator _calc;

    readonly ProgramConfig _config;

    readonly StringBuilder _input = new();
    int _inputCursor = 0;
    int _inputScroll = 0;
    bool _comment = false;
    InputLineMode _ilm = InputLineMode.Normal;
    bool _exit = false;

    ResultFlags _flags = 0;

    readonly ClipboardWindow _clipboardWindow = new();

    Program(ProgramConfig config) {
        _config = config;

        _calc = _config.Type switch {
            InputTypes.Int8 => new RPNCalculator<sbyte>(),
            InputTypes.Int16 => new RPNCalculator<short>(),
            InputTypes.Int32 => new RPNCalculator<int>(),
            InputTypes.Int64 => new RPNCalculator<long>(),
            InputTypes.Int128 => new RPNCalculator<Int128>(),
            _ => throw new NotImplementedException(),
        };
    }

    static void PrintCommandLineHelp() {
        Console.WriteLine("""
            Command line parameters:

            -hex/-dec/-oct/-bin     Set display format
            -type <type>            Set type (e.g. s32, u64)
            -group/-nogroup         Set digit grouping
            -padding <padding>      Set padding mode
            -upper/-lower           Set hexadecimal case
            -index/-noindex         Show stack index
            -base/-nobase           Show base
            -numpad                 Enable fake numpad
            -?                      Show this message

            """);
    }

    [STAThread]
    static int Main(string[] args) {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        var config = new ProgramConfig() {
            Format = IntegerFormat.Decimal,
            Signed = false,
            Type = InputTypes.Int64,
            Grouping = true,
            PaddingMode = PaddingMode.RightJustified,
            Upper = true,
            Index = true,
            Base = false,
            FakeNumpad = false,
            ShowHints = false,
        };

        try {
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
                    config.Index = true;
                }
                else if ("-noindex".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    config.Index = false;
                }

                else if ("-base".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    config.Base = true;
                }
                else if ("-nobase".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    config.Base = false;
                }

                else if ("-numpad".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    config.FakeNumpad = true;
                }

                else if ("-?".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    PrintCommandLineHelp();
                    return 0;
                }

                else {
                    throw new ArgumentException($"Invalid argument {arg}");
                }
            }
        }
        catch (Exception ex) {
            Console.WriteLine(ex.Message);
            PrintCommandLineHelp();
            return 1;
        }

        var program = new Program(config);
        program.DoMain();
        return 0;
    }

    void DoMain() {
        Console.TreatControlCAsInput = true;
        _calc.Push(0, null, null);
        Refresh(RefreshFlags.Screen);
        while (!_exit) {
            var key = Console.ReadKey(true);
            try {
                if (HandleHelpKeys(key, out RefreshFlags refresh) ||
                    HandleModeKeys(key, out refresh) ||
                    HandleEditKeys(key, out refresh) ||
                    HandleCommentKeys(key, out refresh) ||
                    HandleStackKeys(key, out refresh) ||
                    HandleBinaryOrCarryOperators(key, out refresh) ||
                    HandleOperators(key, out refresh) ||
                    HandleFakeNumpadKeys(key, out refresh) ||
                    HandleInputKeys(key, out refresh) ||
                    HandleInputKeys2(key, out refresh)) {
                    Refresh(refresh);
                    continue;
                }
                throw new NotSupportedException(string.Format(
                    "Unknown key: {0}{1}{2}{3}",
                    key.Modifiers.HasFlag(ConsoleModifiers.Control) ? "Ctrl+" : "",
                    key.Modifiers.HasFlag(ConsoleModifiers.Alt) ? "Alt+" : "",
                    key.Modifiers.HasFlag(ConsoleModifiers.Shift) ? "Shift+" : "",
                    key.Key.ToString()));
            }
            catch (Exception ex) {
                Refresh(RefreshFlags.Screen, ex);
                continue;
            }
        }
    }

    void Pause() {
        Console.SetCursorPosition(0, Console.WindowHeight - 1);
        Console.Write("Press any key...");
        Console.ReadKey(true);
    }

    void ResetInput() {
        _input.Clear();
        _inputCursor = 0;
        _inputScroll = 0;
        _comment = false;
    }

    bool HandleHelpKeys(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Screen;
        switch (key.Key) {
            case ConsoleKey.F1 when key.Modifiers == ConsoleModifiers.None:
                Console.Clear();
                Console.WriteLine("""
                    Mode:
                    F5-F8 = hex/dec/oct/bin                  F2 = toggle signed/unsigned
                    F3/F4 = truncate/extend (Ctrl inverts S/U)
                    Ctrl+9 = toggle digit grouping           Ctrl+0 = toggle zero pad
                    Ctrl+1 = toggle upper/lowercase hex      Ctrl+2 = print index
                    Ctrl+3 = toggle base display on stack

                    Editing:
                    Ctrl+c/v = copy/paste                    " = swap comment of index
                    Append `;` to add a comment              Use `index:comment` to set comment

                    Stack:
                    Up/Down = rotate stack                   Del/Shift+Del= edit/delete last
                    Ctrl+Del = clear all                     p = display top
                    z/s = extract/swap                       Shift+Enter = pick

                    Carry operators (add Alt to use carry):
                    `+_` = add/subtract                      `{}` = rotate left/right
                    `<>` = shift (Ctrl inverts S/U)
                    r = push CF/OF (Shift inverts S/U)       Ctrl+r = clear flags

                    Other operators:
                    `*%` = mul/rem                           `/` = div (Ctrl inverts S/U)
                    `&|^~` = bitwise logic                   Shift+4 = popcount
                    Shift+9/0 = mask left/right              Ctrl+Shift+9/0 = count lead/trail 0s
                    Shift+2 = pow2                           Shift+3/Ctrl+Shift+3 = align up/down
                    Alt+Shift+` = byteswap

                    """);
                Pause();
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleModeKeys(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Status | RefreshFlags.Stack;
        switch (key.Key) {
            case ConsoleKey.F2 when key.Modifiers == ConsoleModifiers.None:
                _config.Signed = !_config.Signed;
                break;
            case ConsoleKey.F3 when key.Modifiers == ConsoleModifiers.None:
            case ConsoleKey.F3 when key.Modifiers == ConsoleModifiers.Shift: {
                    if (_config.Type == InputTypes.Int16) {
                        _config.Type = InputTypes.Int8;
                        _calc = _calc.ConvertTo(typeof(sbyte), false);
                    }
                    else if (_config.Type == InputTypes.Int32) {
                        _config.Type = InputTypes.Int16;
                        _calc = _calc.ConvertTo(typeof(short), false);
                    }
                    else if (_config.Type == InputTypes.Int64) {
                        _config.Type = InputTypes.Int32;
                        _calc = _calc.ConvertTo(typeof(int), false);
                    }
                    else if (_config.Type == InputTypes.Int128) {
                        _config.Type = InputTypes.Int64;
                        _calc = _calc.ConvertTo(typeof(long), false);
                    }
                    break;
                }
            case ConsoleKey.F4 when key.Modifiers == ConsoleModifiers.None:
            case ConsoleKey.F4 when key.Modifiers == ConsoleModifiers.Shift: {
                    var shifted = key.Modifiers.HasFlag(ConsoleModifiers.Shift);
                    var signed = _config.Signed ^ shifted;
                    if (_config.Type == InputTypes.Int8) {
                        _config.Type = InputTypes.Int16;
                        _calc = _calc.ConvertTo(typeof(short), signed);
                    }
                    else if (_config.Type == InputTypes.Int16) {
                        _config.Type = InputTypes.Int32;
                        _calc = _calc.ConvertTo(typeof(int), signed);
                    }
                    else if (_config.Type == InputTypes.Int32) {
                        _config.Type = InputTypes.Int64;
                        _calc = _calc.ConvertTo(typeof(long), signed);
                    }
                    else if (_config.Type == InputTypes.Int64) {
                        _config.Type = InputTypes.Int128;
                        _calc = _calc.ConvertTo(typeof(Int128), signed);
                    }
                    break;
                }
            case ConsoleKey.F5 when key.Modifiers == ConsoleModifiers.None:
                _config.Format = IntegerFormat.Hexadecimal;
                break;
            case ConsoleKey.F6 when key.Modifiers == ConsoleModifiers.None:
                _config.Format = IntegerFormat.Decimal;
                break;
            case ConsoleKey.F7 when key.Modifiers == ConsoleModifiers.None:
                _config.Format = IntegerFormat.Octal;
                break;
            case ConsoleKey.F8 when key.Modifiers == ConsoleModifiers.None:
                _config.Format = IntegerFormat.Binary;
                break;
            case ConsoleKey.D9 when key.Modifiers == ConsoleModifiers.Control:
                _config.Grouping = !_config.Grouping;
                break;
            case ConsoleKey.D0 when key.Modifiers == ConsoleModifiers.Control:
                _config.PaddingMode = _config.PaddingMode switch {
                    PaddingMode.None => PaddingMode.RightJustified,
                    PaddingMode.RightJustified => PaddingMode.ZeroPadded,
                    PaddingMode.ZeroPadded => PaddingMode.None,
                    _ => PaddingMode.None,
                };
                break;
            case ConsoleKey.D1 when key.Modifiers == ConsoleModifiers.Control:
                _config.Upper = !_config.Upper;
                break;
            case ConsoleKey.D2 when key.Modifiers == ConsoleModifiers.Control:
                _config.Index = !_config.Index;
                break;
            case ConsoleKey.D3 when key.Modifiers == ConsoleModifiers.Control:
                _config.Base = !_config.Base;
                break;
            case ConsoleKey.LeftWindows:
            case ConsoleKey.RightWindows:
                refresh = RefreshFlags.None;
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleEditKeys(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Input;
        switch (key.Key) {
            case ConsoleKey.Enter when key.Modifiers == ConsoleModifiers.None:
                PushInput();
                refresh = RefreshFlags.Stack | RefreshFlags.Input;
                break;
            case ConsoleKey.D when key.Modifiers == ConsoleModifiers.Control:
                _exit = true;
                break;
            case ConsoleKey.L when key.Modifiers == ConsoleModifiers.Control:
                refresh = RefreshFlags.Screen;
                break;
            case ConsoleKey.Backspace when key.Modifiers == ConsoleModifiers.None:
                if (_inputCursor > 0)
                    _input.Remove(--_inputCursor, 1);
                break;
            case ConsoleKey.Delete when key.Modifiers == ConsoleModifiers.None:
                if (_input.Length <= 0 || _inputCursor >= _input.Length)
                    return false;
                _input.Remove(_inputCursor, 1);
                break;
            case ConsoleKey.LeftArrow when key.Modifiers == ConsoleModifiers.None:
                if (_inputCursor > 0)
                    _inputCursor--;
                break;
            case ConsoleKey.RightArrow when key.Modifiers == ConsoleModifiers.None:
                if (_inputCursor < _input.Length)
                    _inputCursor++;
                break;
            case ConsoleKey.Home when key.Modifiers == ConsoleModifiers.None:
                _inputCursor = 0;
                break;
            case ConsoleKey.End when key.Modifiers == ConsoleModifiers.None:
                _inputCursor = _input.Length;
                break;
            case ConsoleKey.Escape when key.Modifiers == ConsoleModifiers.None:
                if (_ilm == InputLineMode.Normal)
                    ResetInput();
                break;
            case ConsoleKey.W when key.Modifiers == ConsoleModifiers.Control:
                ResetInput();
                break;
            case ConsoleKey.C when key.Modifiers == ConsoleModifiers.Control:
                using (var clipboard = new Clipboard(_clipboardWindow.Handle)) {
                    var entry = _calc.Peek();
                    var sb = new StringBuilder();
                    FormatValueRaw(
                        sb,
                        entry.Object,
                        _config.Format,
                        _config.Signed,
                        0,
                        PaddingMode.None,
                        _config.Upper);
                    clipboard.SetText(sb.ToString());
                }
                break;
            case ConsoleKey.V when key.Modifiers == ConsoleModifiers.Control:
                using (var clipboard = new Clipboard(_clipboardWindow.Handle)) {
                    var text = clipboard.GetText() ?? string.Empty;
                    _input.Insert(_inputCursor, text);
                    _inputCursor += text.Length;
                }
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleCommentKeys(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Stack | RefreshFlags.Input;
        switch (key.Key) {
            case ConsoleKey.Oem7 when key.Modifiers == ConsoleModifiers.Shift:
                if (_input.Length == 0) {
                    _calc.DoStackOp(StackOperation.SwapComment, 1);
                }
                else {
                    PushInput(true);
                    try {
                        _calc.DoStackOp(StackOperation.SwapComment, null);
                    }
                    catch {
                        _calc.DoStackOp(StackOperation.Drop, 1);
                        throw;
                    }
                }
                return true;
            default:
                break;
        }

        refresh = RefreshFlags.Input;
        if (!_comment)
            return false;
        if (key.KeyChar == '\0')
            return false;
        if (key.Modifiers != ConsoleModifiers.None && key.Modifiers != ConsoleModifiers.Shift)
            return false;
        _input.Insert(_inputCursor++, key.KeyChar);
        return true;
    }

    bool HandleStackKeys(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Stack | RefreshFlags.Input;
        switch (key.Key) {
            case ConsoleKey.UpArrow when key.Modifiers == ConsoleModifiers.None:
                _calc.DoStackOp(StackOperation.Roll, -1);
                break;
            case ConsoleKey.DownArrow when key.Modifiers == ConsoleModifiers.None:
                _calc.DoStackOp(StackOperation.Roll, 1);
                break;
            case ConsoleKey.Delete when key.Modifiers == ConsoleModifiers.None: {
                    if (_input.Length != 0)
                        throw new InvalidOperationException("Still editing");

                    var entry = _calc.Peek();
                    _calc.DoStackOp(StackOperation.Drop, 1);
                    try {
                        FormatValueRaw(
                            _input,
                            entry.Object,
                            _config.Format,
                            _config.Signed,
                            0,
                            PaddingMode.None,
                            _config.Upper);
                        if (entry.Comment != null) {
                            _input.Append(';');
                            _input.Append(entry.Comment);
                        }
                    }
                    catch {
                        _calc.Push(entry);
                        throw;
                    }
                    _inputCursor = _input.Length;
                    break;
                }
            case ConsoleKey.Delete when key.Modifiers == ConsoleModifiers.Shift:
                if (_input.Length == 0) {
                    _calc.DoStackOp(StackOperation.Drop, 1);
                }
                else {
                    PushInput(true);
                    try {
                        _calc.DoStackOp(StackOperation.Drop, null);
                    }
                    catch {
                        _calc.DoStackOp(StackOperation.Drop, 1);
                        throw;
                    }
                }
                break;
            case ConsoleKey.Delete when key.Modifiers == ConsoleModifiers.Control:
                _calc.Clear();
                ResetInput();
                break;
            case ConsoleKey.Z when key.Modifiers == ConsoleModifiers.None:
                if (_input.Length == 0) {
                    _calc.DoStackOp(StackOperation.Extract, 2);
                }
                else {
                    PushInput(true);
                    try {
                        _calc.DoStackOp(StackOperation.Extract, null);
                    }
                    catch {
                        _calc.DoStackOp(StackOperation.Drop, 1);
                        throw;
                    }
                }
                break;
            case ConsoleKey.S when key.Modifiers == ConsoleModifiers.None:
                if (_input.Length == 0) {
                    _calc.DoStackOp(StackOperation.Swap, 2);
                }
                else {
                    PushInput(true);
                    try {
                        _calc.DoStackOp(StackOperation.Swap, null);
                    }
                    catch {
                        _calc.DoStackOp(StackOperation.Drop, 1);
                        throw;
                    }
                }
                break;
            case ConsoleKey.Enter when key.Modifiers == ConsoleModifiers.Shift:
                if (_input.Length == 0) {
                    _calc.DoStackOp(StackOperation.Pick, 1);
                }
                else {
                    PushInput(true);
                    try {
                        _calc.DoStackOp(StackOperation.Pick, null);
                    }
                    catch {
                        _calc.DoStackOp(StackOperation.Drop, 1);
                        throw;
                    }
                }
                break;
            case ConsoleKey.P when key.Modifiers == ConsoleModifiers.None:
                PrintValue(_calc.Peek().Object);
                refresh = RefreshFlags.Screen;
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleBinaryOrCarryOperators(ConsoleKeyInfo key, out RefreshFlags refresh) {
        var isNoneOrAlt = key.Modifiers == ConsoleModifiers.None ||
            key.Modifiers == ConsoleModifiers.Alt;
        var isShiftOrAltShift = key.Modifiers == ConsoleModifiers.Shift ||
            key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt);
        var ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        var shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);
        var alt = key.Modifiers.HasFlag(ConsoleModifiers.Alt);
        var co = _config.Signed ?
            _flags.HasFlag(ResultFlags.Overflow) :
            _flags.HasFlag(ResultFlags.Carry);
        var oc = _config.Signed ?
            _flags.HasFlag(ResultFlags.Carry) :
            _flags.HasFlag(ResultFlags.Overflow);

        refresh = RefreshFlags.Status | RefreshFlags.Stack | RefreshFlags.Input;
        switch (key.Key) {
            case ConsoleKey.OemPlus when isShiftOrAltShift:
            case ConsoleKey.Add when isNoneOrAlt:
                PushInput();
                _flags = _calc.DoBinaryOp(
                    alt ? BinaryOperation.AddCarry : BinaryOperation.Add,
                    co,
                    _flags);
                break;

            case ConsoleKey.OemMinus when isShiftOrAltShift:
            case ConsoleKey.Subtract when isNoneOrAlt:
                PushInput();
                _flags = _calc.DoBinaryOp(
                    alt ? BinaryOperation.SubtractBorrow : BinaryOperation.Subtract,
                    co,
                    _flags);
                break;

            case ConsoleKey.D8 when key.Modifiers == ConsoleModifiers.Shift:
            case ConsoleKey.Multiply when key.Modifiers == ConsoleModifiers.None:
                PushInput();
                _flags = _calc.DoBinaryOp(BinaryOperation.Multiply, false, _flags);
                break;

            case ConsoleKey.Oem2 when key.Modifiers == ConsoleModifiers.None:
            case ConsoleKey.Oem2 when key.Modifiers == ConsoleModifiers.Control:
            case ConsoleKey.Divide when key.Modifiers == ConsoleModifiers.None:
            case ConsoleKey.Divide when key.Modifiers == ConsoleModifiers.Control:
                PushInput();
                if (_config.Signed ^ ctrl)
                    _flags = _calc.DoBinaryOp(BinaryOperation.SignedDivide, false, _flags);
                else
                    _flags = _calc.DoBinaryOp(BinaryOperation.UnsignedDivide, false, _flags);
                break;

            case ConsoleKey.D5 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _flags = _calc.DoBinaryOp(BinaryOperation.Remainder, false, _flags);
                break;

            case ConsoleKey.D7 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _flags = _calc.DoBinaryOp(BinaryOperation.And, false, _flags);
                break;

            case ConsoleKey.Oem5 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _flags = _calc.DoBinaryOp(BinaryOperation.Or, false, _flags);
                break;

            case ConsoleKey.D6 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _flags = _calc.DoBinaryOp(BinaryOperation.Xor, false, _flags);
                break;

            case ConsoleKey.OemComma when shift:
                PushInput();
                _flags = _calc.DoBinaryOp(BinaryOperation.ShiftLeft, false, _flags);
                break;

            case ConsoleKey.OemPeriod when shift:
                PushInput();
                if (_config.Signed ^ ctrl)
                    _flags = _calc.DoBinaryOp(BinaryOperation.ShiftRightArithmetic, false, _flags);
                else
                    _flags = _calc.DoBinaryOp(BinaryOperation.ShiftRight, false, _flags);
                break;

            case ConsoleKey.Oem4 when isShiftOrAltShift:
                PushInput();
                _flags = _calc.DoBinaryOp(
                    alt ? BinaryOperation.RotateLeftCarry : BinaryOperation.RotateLeft,
                    co,
                    _flags);
                break;

            case ConsoleKey.Oem6 when isShiftOrAltShift:
                PushInput();
                _flags = _calc.DoBinaryOp(
                    alt ? BinaryOperation.RotateRightCarry : BinaryOperation.RotateRight,
                    co,
                    _flags);
                break;

            case ConsoleKey.D3 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _flags = _calc.DoBinaryOp(BinaryOperation.AlignUp, false, _flags);
                break;

            case ConsoleKey.D3 when key.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift):
                PushInput();
                _flags = _calc.DoBinaryOp(BinaryOperation.AlignDown, false, _flags);
                break;

            case ConsoleKey.R when key.Modifiers == ConsoleModifiers.None:
            case ConsoleKey.R when key.Modifiers == ConsoleModifiers.Shift:
                if (_input.Length != 0)
                    throw new InvalidOperationException("Still editing");

                _calc.Push(
                    (key.Modifiers.HasFlag(ConsoleModifiers.Shift) ? oc : co) ? 1 : 0,
                    null,
                    null);
                break;

            case ConsoleKey.R when key.Modifiers == ConsoleModifiers.Control:
                if (_input.Length != 0)
                    throw new InvalidOperationException("Still editing");

                _flags = 0;
                break;

            default:
                return false;
        }
        return true;
    }

    bool HandleOperators(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Stack | RefreshFlags.Input;
        switch (key.Key) {
            case ConsoleKey.Oem3 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.Not);
                break;
            case ConsoleKey.D9 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.MaskLeft);
                break;
            case ConsoleKey.D0 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.MaskRight);
                break;
            case ConsoleKey.D4 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.PopCount);
                break;
            case ConsoleKey.D9 when key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Control):
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.CountLeadingZeroes);
                break;
            case ConsoleKey.D0 when key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Control):
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.CountTrailingZeroes);
                break;
            case ConsoleKey.D2 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.Pow2);
                break;
            case ConsoleKey.Oem3 when key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt):
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.ByteSwap);
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleFakeNumpadKeys(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Input;
        if (!_config.FakeNumpad || key.Modifiers != ConsoleModifiers.None)
            return false;
        switch (key.Key) {
            case ConsoleKey.M:
                _input.Insert(_inputCursor, "00");
                _inputCursor += 2;
                break;
            case ConsoleKey.OemComma:
                _input.Insert(_inputCursor++, '0');
                break;
            case ConsoleKey.J:
                _input.Insert(_inputCursor++, '1');
                break;
            case ConsoleKey.K:
                _input.Insert(_inputCursor++, '2');
                break;
            case ConsoleKey.L:
                _input.Insert(_inputCursor++, '3');
                break;
            case ConsoleKey.U:
                _input.Insert(_inputCursor++, '4');
                break;
            case ConsoleKey.I:
                _input.Insert(_inputCursor++, '5');
                break;
            case ConsoleKey.O:
                _input.Insert(_inputCursor++, '6');
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleInputKeys(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Input;
        if (key.Modifiers != ConsoleModifiers.None && key.Modifiers != ConsoleModifiers.Shift)
            return false;
        switch (key.Key) {
            case ConsoleKey.Oem1:
                _comment = true;
                _input.Insert(_inputCursor++, key.KeyChar);
                break;
            case ConsoleKey.A:
            case ConsoleKey.B:
            case ConsoleKey.C:
            case ConsoleKey.D:
            case ConsoleKey.E:
            case ConsoleKey.F:
            // bases
            case ConsoleKey.H:
            case ConsoleKey.N:
            case ConsoleKey.O:
            case ConsoleKey.T:
            case ConsoleKey.X:
            case ConsoleKey.Y:
            case ConsoleKey.OemMinus:
                // Shift+Minus is handled in HandleOperators
                _input.Insert(_inputCursor++, key.KeyChar);
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleInputKeys2(ConsoleKeyInfo key, out RefreshFlags refresh) {
        refresh = RefreshFlags.Input;
        if (key.Modifiers != ConsoleModifiers.None)
            return false;
        switch (key.Key) {
            case ConsoleKey.D0:
            case ConsoleKey.NumPad0:
                _input.Insert(_inputCursor++, '0');
                break;
            case ConsoleKey.D1:
            case ConsoleKey.NumPad1:
                _input.Insert(_inputCursor++, '1');
                break;
            case ConsoleKey.D2:
            case ConsoleKey.NumPad2:
                _input.Insert(_inputCursor++, '2');
                break;
            case ConsoleKey.D3:
            case ConsoleKey.NumPad3:
                _input.Insert(_inputCursor++, '3');
                break;
            case ConsoleKey.D4:
            case ConsoleKey.NumPad4:
                _input.Insert(_inputCursor++, '4');
                break;
            case ConsoleKey.D5:
            case ConsoleKey.NumPad5:
                _input.Insert(_inputCursor++, '5');
                break;
            case ConsoleKey.D6:
            case ConsoleKey.NumPad6:
                _input.Insert(_inputCursor++, '6');
                break;
            case ConsoleKey.D7:
            case ConsoleKey.NumPad7:
                _input.Insert(_inputCursor++, '7');
                break;
            case ConsoleKey.D8:
            case ConsoleKey.NumPad8:
                _input.Insert(_inputCursor++, '8');
                break;
            case ConsoleKey.D9:
            case ConsoleKey.NumPad9:
                _input.Insert(_inputCursor++, '9');
                break;
            case ConsoleKey.Spacebar:
                _input.Insert(_inputCursor++, ' ');
                break;
            default:
                return false;
        }
        return true;
    }

    void PushInput(bool stack = false) {
        var original = _input.ToString();
        try {
            var entry = _calc.ParseEntry(
                original,
                stack ? IntegerFormat.Decimal : _config.Format,
                out var commentChar);
            if (entry == null)
                return;

            _calc.Push(entry);
            if (commentChar == ':') {
                try {
                    _calc.DoStackOp(StackOperation.SetComment, null);
                }
                catch {
                    _calc.DoStackOp(StackOperation.Drop, 1);
                    throw;
                }
            }
            ResetInput();
        }
        catch {
            _input.Clear();
            _input.Append(original);
            _inputCursor = _input.Length;
            throw;
        }
    }

    void Write(ReadOnlySpan<char> text, int width = -1, int scroll = 0, int totalLength = -1) {
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

    int GetPadSize(PaddingMode paddingMode) {
        if (paddingMode != PaddingMode.ZeroPadded)
            return 0;
        return _calc.WordBytes;
    }

    void FormatOctalRaw(
        StringBuilder sb,
        object value,
        PaddingMode paddingMode) {
        var size = GetPadSize(PaddingMode.ZeroPadded);

        var val = IntConverter.ToUInt128(value);
        var pad = (size * 8 + 2) / 3;
        var begin = sb.Length;
        for (int i = pad - 1; i >= 0; i--)
            sb.Append("01234567"[(int)(val >> (i * 3) & 7)]);
        if (paddingMode != PaddingMode.ZeroPadded)
            while (sb.Length > begin + 1 && sb[begin] == '0')
                sb.Remove(begin, 1);
    }

    void ApplyGrouping(StringBuilder sb, int groupSize) {
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

    void FormatValueRaw(
        StringBuilder sb,
        object value,
        IntegerFormat format,
        bool signed,
        int group,
        PaddingMode paddingMode,
        bool upper) {

        if (format == IntegerFormat.Octal) {
            FormatOctalRaw(sb, value, paddingMode: paddingMode);
        }
        else {
            var size = GetPadSize(paddingMode);

            if (!signed)
                value = IntConverter.ToUInt128(value);

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

    void FormatBinaryFancy(StringBuilder sb, object value) {
        var bit = _calc.WordBytes * 8;
        var val = IntConverter.ToUInt128(value);

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

    void PrintValue(object value) {
        var sb = new StringBuilder(1024);

        Console.Clear();

        Write("Hex:");
        sb.Clear();
        FormatValueRaw(
            sb,
            value,
            IntegerFormat.Hexadecimal,
            false,
            4,
            _config.PaddingMode,
            _config.Upper);
        Write(sb.ToString());

        Write("");
        Write("Dec (unsigned):");
        sb.Clear();
        FormatValueRaw(
            sb,
            value,
            IntegerFormat.Decimal,
            false,
            3,
            _config.PaddingMode,
            _config.Upper);
        Write(sb.ToString());
        Write("Dec (signed):");
        sb.Clear();
        FormatValueRaw(
            sb,
            value,
            IntegerFormat.Decimal,
            true,
            3,
            _config.PaddingMode,
            _config.Upper);
        Write(sb.ToString());

        Write("");
        Write("Oct:");
        sb.Clear();
        FormatValueRaw(
            sb,
            value,
            IntegerFormat.Octal,
            _config.Signed,
            3,
            _config.PaddingMode,
            _config.Upper);
        Write(sb.ToString());

        Write("");
        Write("Bin:");
        sb.Clear();
        FormatBinaryFancy(sb, value);
        foreach (var line in sb.ToString().Split('\n'))
            Write(line);

        Pause();
    }

    void PrintStack(IRPNCalculator calc) {
        var printable = Console.WindowHeight - 2;
        if (printable < calc.Count) {
            printable--;
            Write(string.Format("...{0}", calc.Count - printable));
        }

        var stackItems = calc.GetStackItems(Math.Min(printable, calc.Count)).ToList();
        var maxLength = 0;
        List<string> printed = [];
        var sb = new StringBuilder(16 * 9 + 8);

        for (int i = 0; i < stackItems.Count; i++) {
            var entry = stackItems[stackItems.Count - i - 1];
            sb.Clear();
            var group = _config.Format switch {
                IntegerFormat.Hexadecimal => 4,
                IntegerFormat.Decimal => 3,
                IntegerFormat.Octal => 3,
                IntegerFormat.Binary => 4,
                _ => throw new InvalidOperationException(),
            };
            if (!_config.Grouping)
                group = 0;
            FormatValueRaw(
                sb,
                entry.Object,
                format: _config.Format,
                signed: _config.Signed,
                group: group,
                paddingMode: _config.PaddingMode,
                upper: _config.Upper);
            maxLength = Math.Max(maxLength, sb.Length);
            printed.Add(sb.ToString());
        }

        var baseString = _config.Format switch {
            IntegerFormat.Hexadecimal => "0x",
            IntegerFormat.Decimal => "  ",
            IntegerFormat.Octal => "0o",
            IntegerFormat.Binary => "0b",
            _ => throw new NotImplementedException(),
        };
        for (int i = 0; i < stackItems.Count; i++) {
            var entry = stackItems[stackItems.Count - i - 1];
            var value = printed[i];
            if (_config.PaddingMode == PaddingMode.RightJustified)
                value = value.PadLeft(maxLength);
            if (_config.Base) {
                value = baseString + value;
            }
            if (_config.Index)
                value = $"{printed.Count - i,4}  {value}";
            if (entry.Comment != null)
                value = value + " ; " + entry.Comment;
            Write(value);
        }
    }

    void PrintInputLine() {
        var inputLineWidth = Console.WindowWidth - 1;
        var input = _input.ToString();
        int markerLeft, usableWidth, cursorCol;

        // update _inputScroll to not scroll past the cursor (e.g. when pressing Home)
        _inputScroll = Math.Min(_inputScroll, _inputCursor);

        while (true) {
            // check if we need a '<' marker on the left
            markerLeft = _inputScroll > 0 ? 1 : 0;
            usableWidth = inputLineWidth - markerLeft;
            // if there's more text than fits the current window, we need a '>' marker on the right
            if (input.Length > _inputScroll + usableWidth)
                usableWidth--;

            var markerRight = (input.Length > _inputScroll + usableWidth) ? 1 : 0;

            // calculate where the cursor will physically appear on screen
            cursorCol = markerLeft + (_inputCursor - _inputScroll);
            // if the cursor fits within the bounds (including the potential '>' marker), we're done
            if (cursorCol < inputLineWidth - markerRight)
                break;
            // otherwise, scroll right so the cursor is at the very end of the line
            _inputScroll = _inputCursor - (inputLineWidth - markerLeft - markerRight - 1);
        }

        var visible = input.AsSpan(_inputScroll, Math.Min(usableWidth, input.Length - _inputScroll));
        Write(visible, inputLineWidth, _inputScroll, input.Length);
        Console.SetCursorPosition(cursorCol, Console.WindowHeight - 1);
    }

    void Refresh(RefreshFlags flags, Exception? ex = null) {
        var inputCol = Console.CursorLeft;
        try {
            Console.CursorVisible = false;

            if (flags.HasFlag(RefreshFlags.Status)) {
                Console.SetCursorPosition(0, 0);
                var mode = _config.Format switch {
                    IntegerFormat.Hexadecimal => "Hex",
                    IntegerFormat.Decimal => "Dec",
                    IntegerFormat.Octal => "Oct",
                    IntegerFormat.Binary => "Bin",
                    _ => throw new NotImplementedException(),
                };
                string statusFormat;
                if (_config.ShowHints)
                    statusFormat = "{0}{1,-6} (F234)  {2} (F5678)  {3,5} {4,5} {5} {6} {7,5} (^90123)  {8}{9}";
                else
                    statusFormat = "{0}{1,-6}    {2}    {3,5} {4,5} {5} {6} {7,5}    {8}{9}";
                Write(string.Format(
                    statusFormat,
                    _config.Signed ? "S" : "U",
                    _config.Type,
                    mode,
                    _config.Grouping ? "Group" : "Ungrp",
                    _config.PaddingMode switch {
                        PaddingMode.None => "Left",
                        PaddingMode.RightJustified => "Right",
                        PaddingMode.ZeroPadded => "Pad",
                        _ => throw new NotImplementedException(),
                    },
                    _config.Upper ? "Upper" : "Lower",
                    _config.Index ? "Index" : "NoIdx",
                    _config.Base ? "Base" : "NoBas",
                    _flags.HasFlag(ResultFlags.Carry) ? "C" : " ",
                    _flags.HasFlag(ResultFlags.Overflow) ? "O" : " "));
            }

            if (flags.HasFlag(RefreshFlags.Stack)) {
                Console.SetCursorPosition(0, 1);
                PrintStack(_calc);

                while (Console.CursorTop < Console.WindowHeight - 1)
                    Write("");
            }

            if (flags.HasFlag(RefreshFlags.Input)) {
                Console.SetCursorPosition(0, Console.WindowHeight - 1);
                if (ex != null) {
                    _ilm = InputLineMode.Exception;
                    Write(ex.Message, width: Console.WindowWidth - 1);
                    Console.Beep();
                }
                else {
                    _ilm = InputLineMode.Normal;
                    PrintInputLine();
                }
            }
            else {
                Console.SetCursorPosition(inputCol, Console.WindowHeight - 1);
            }
        }
        finally {
            Console.CursorVisible = true;
        }
    }
}
