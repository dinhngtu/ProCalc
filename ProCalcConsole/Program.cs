using ProCalcConsole;
using ProCalcCore;
using System.Globalization;
using System.Text;

class Program {
    IRPNCalculator _calc;

    ProgramConfig _config;

    readonly StringBuilder _input = new();
    int _inputCursor = 0;
    int _inputScroll = 0;
    bool _comment = false;
    InputLineMode _ilm = InputLineMode.Normal;
    bool _exit = false;

    ResultFlags _flags = 0;

    readonly ClipboardManager _clipboard = new();

    readonly System.Timers.Timer _timer = new() {
        Enabled = false,
        AutoReset = false,
        Interval = 1000,
    };

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

    [STAThread]
    static int Main(string[] args) {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        ProgramConfig config;
        try {
            if (!ProgramConfig.ParseCommandLineArgs(args, out config)) {
                Console.WriteLine(ProgramConfig.CommandLineHelp);
                return 0;
            }
        }
        catch (Exception ex) {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ProgramConfig.CommandLineHelp);
            return 1;
        }

        var program = new Program(config);
        return program.Run();
    }

    int Run() {
        uint oldIm = 0, oldOm = 0;
        if (ConsoleEx.IsWindows) {
            try {
                (oldIm, oldOm) = WindowsConsole.EnableTuiMode();
            }
            catch {
                Console.WriteLine("Not a terminal");
                return 1;
            }
        }
        try {
            _timer.Elapsed += (o, e) => Refresh(RefreshFlags.Input);
            _calc.Push(0, null, null);
            Refresh(RefreshFlags.Screen);
            while (!_exit) {
                var cin = ConsoleEx.ReadConsoleInput();
                switch (cin) {
                    case ConsoleKeyInfo key:
                        HandleKey(key);
                        break;
                    case ConsoleResizeInfo:
                        Refresh(RefreshFlags.Screen | RefreshFlags.Clear, null);
                        break;
                }
            }
            _timer.Stop();

            return 0;
        }
        finally {
            if (ConsoleEx.IsWindows)
                WindowsConsole.RestoreMode(oldIm, oldOm);
        }
    }

    void HandleKey(ConsoleKeyInfo key) {
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
            }
            else {
                throw new NotSupportedException(string.Format(
                    "Unknown key: {0}{1}{2}{3}",
                    key.Modifiers.HasFlag(ConsoleModifiers.Control) ? "Ctrl+" : "",
                    key.Modifiers.HasFlag(ConsoleModifiers.Alt) ? "Alt+" : "",
                    key.Modifiers.HasFlag(ConsoleModifiers.Shift) ? "Shift+" : "",
                    key.Key.ToString()));
            }
        }
        catch (Exception ex) {
            Refresh(RefreshFlags.Screen, ex);
        }
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
                    F3/F4 = trunc./extend (Ctrl inverts S/U) F12 = settings

                    Editing:
                    h/x/n/o/t/b/y = base prefixes/suffixes
                    Ctrl+c/v = copy/paste                    `"` = swap comment of index
                    Append `;` to add a comment              Use `index:comment` to set comment

                    Stack:
                    Up/Down = rotate stack                   Del/Shift+Del = edit/delete last
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
                ConsoleEx.Pause();
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
            case ConsoleKey.F12 when key.Modifiers == ConsoleModifiers.None: {
                    refresh = RefreshFlags.Screen;
                    var configPage = new ConfigPage(_config);
                    configPage.Run();
                    _config = configPage.Config;
                    break;
                }
            case ConsoleKey.LeftWindows:
            case ConsoleKey.RightWindows:
                refresh = RefreshFlags.None;
                break;
            default:
                return false;
        }
        return true;
    }

    void Backspace() {
        if (_inputCursor > 0) {
            var last = _input[--_inputCursor];
            _input.Remove(_inputCursor, 1);
            if (last == ';' || last == ':') {
                var input = _input.ToString();
                _comment = input.Contains(';') || input.Contains(':');
            }
        }
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
                refresh = RefreshFlags.Screen | RefreshFlags.Clear;
                break;
            case ConsoleKey.Backspace when key.Modifiers == ConsoleModifiers.None:
                Backspace();
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
                using (var clipboard = _clipboard.Open()) {
                    if (clipboard == null)
                        break;
                    var entry = _calc.Peek();
                    var sb = new StringBuilder();
                    Numerics.FormatValueRaw(
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
                using (var clipboard = _clipboard.Open()) {
                    if (clipboard == null)
                        break;
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
                        Numerics.FormatValueRaw(
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
                refresh = RefreshFlags.Screen;
                new DisplayPage(_config).Run(_calc.Peek().Object);
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
        if (key.KeyChar == ';' || key.KeyChar == ':') {
            _comment = true;
            _input.Insert(_inputCursor++, key.KeyChar);
            return true;
        }
        switch (key.Key) {
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
                (stack || !_config.InputUsesCurrentBase) ? IntegerFormat.Decimal : _config.Format,
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

    void PrintStack(IRPNCalculator calc) {
        var printable = Console.WindowHeight - 2;
        if (printable < calc.Count) {
            printable--;
            ConsoleEx.Write($"...{calc.Count - printable}");
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
            Numerics.FormatValueRaw(
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
            var value = new StringBuilder(printed[i]);
            if (_config.PaddingMode == PaddingMode.RightJustified && value.Length < maxLength)
                value.Insert(0, " ", maxLength - value.Length);
            if (_config.ShowStackBase) {
                value.Insert(0, baseString);
            }
            if (_config.ShowStackIndex)
                value.Insert(0, $"{printed.Count - i,4}  ");
            if (entry.Comment != null)
                value.AppendFormat(" ; {0}", entry.Comment);
            ConsoleEx.Write(value.ToString());
        }
    }

    void PrintInputLine() {
        var inputLineWidth = Console.WindowWidth;
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
        ConsoleEx.Write(visible, inputLineWidth, _inputScroll, input.Length);
        Console.SetCursorPosition(cursorCol, Console.WindowHeight - 1);
    }

    void Refresh(RefreshFlags flags, Exception? ex = null) {
        int width = Console.WindowWidth, height = Console.WindowHeight;
        var inputCol = Console.CursorLeft;
        try {
            Console.CursorVisible = false;

            if (width < 79 || height < 4) {
                Console.SetCursorPosition(0, 0);
                Console.Clear();
                Console.Write($"Window too small ({width}x{height})");
                return;
            }

            if (flags.HasFlag(RefreshFlags.Clear)) {
                Console.Clear();
            }

            if (flags.HasFlag(RefreshFlags.Status)) {
                Console.SetCursorPosition(0, 0);
                var mode = _config.Format switch {
                    IntegerFormat.Hexadecimal => "HexDOB",
                    IntegerFormat.Decimal => "HDecOB",
                    IntegerFormat.Octal => "HDOctB",
                    IntegerFormat.Binary => "HDOBin",
                    _ => throw new NotImplementedException(),
                };
                string statusFormat;
                if (_config.ShowHints)
                    statusFormat = "{0}{1,-6} (F234)  {2} (F5678)  {3}{4}";
                else
                    statusFormat = "{0}{1,-6}    {2}    {3}{4}";
                ConsoleEx.Write(string.Format(
                    statusFormat,
                    _config.Signed ? "S" : "U",
                    _config.Type,
                    mode,
                    _flags.HasFlag(ResultFlags.Carry) ? "C" : " ",
                    _flags.HasFlag(ResultFlags.Overflow) ? "O" : " "));
            }

            if (flags.HasFlag(RefreshFlags.Stack)) {
                Console.SetCursorPosition(0, 1);
                PrintStack(_calc);

                while (Console.CursorTop < height - 1)
                    ConsoleEx.Write("");
            }

            _timer.Stop();
            if (flags.HasFlag(RefreshFlags.Input)) {
                Console.SetCursorPosition(0, height - 1);
                if (ex != null) {
                    _ilm = InputLineMode.Exception;
                    ConsoleEx.Write(ex.Message, width: width - 1);
                    Console.Beep();
                    if (_config.AutoDismissErrors)
                        _timer.Start();
                }
                else {
                    _ilm = InputLineMode.Normal;
                    PrintInputLine();
                }
            }
            else {
                Console.SetCursorPosition(inputCol, height - 1);
            }
        }
        finally {
            Console.CursorVisible = true;
        }
    }
}
