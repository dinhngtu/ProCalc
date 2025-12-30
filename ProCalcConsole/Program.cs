using ProCalcConsole;
using ProCalcCore;
using System.Globalization;
using System.Text;

class Program {
    IRPNCalculator _calc;
    DisplayFormat _format;
    DisplaySignedness _sign;
    InputTypes _type;
    bool _grouping;
    PaddingMode _paddingMode;
    bool _upper;
    bool _index;
    bool _fakeNumpad;

    readonly StringBuilder _input = new();
    int _inputCursor = 0;
    int _inputScroll = 0;
    bool _comment = false;
    bool _exit = false;

    Program(
        DisplayFormat format,
        DisplaySignedness sign,
        InputTypes type,
        bool grouping,
        PaddingMode paddingMode,
        bool upper,
        bool index,
        bool fakeNumpad) {
        _format = format;
        _sign = sign;
        _type = type;
        _grouping = grouping;
        _paddingMode = paddingMode;
        _upper = upper;
        _index = index;
        _fakeNumpad = fakeNumpad;

        _calc = _type switch {
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
            -numpad                 Enable fake numpad
            -?                      Show this message

            """);
    }

    static int Main(string[] args) {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        DisplayFormat format = DisplayFormat.Hexadecimal;
        DisplaySignedness sign = DisplaySignedness.Unsigned;
        InputTypes type = InputTypes.Int64;
        bool grouping = true;
        PaddingMode paddingMode = PaddingMode.RightJustified;
        bool upper = true;
        bool index = true;
        bool fakeNumpad = false;

        try {
            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                if ("-hex".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    format = DisplayFormat.Hexadecimal;
                }
                else if ("-dec".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    format = DisplayFormat.Decimal;
                }
                else if ("-oct".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    format = DisplayFormat.Octal;
                }
                else if ("-bin".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    format = DisplayFormat.Binary;
                }

                else if ("-type".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    var typeName = args[++i];
                    var signedness = char.ToLowerInvariant(typeName[0]) switch {
                        's' => DisplaySignedness.Signed,
                        'u' => DisplaySignedness.Unsigned,
                        _ => throw new ArgumentException($"Invalid -type argument {typeName}"),
                    };
                    var typeSize = typeName[1..] switch {
                        "8" => InputTypes.Int8,
                        "16" => InputTypes.Int16,
                        "32" => InputTypes.Int32,
                        "64" => InputTypes.Int64,
                        "128" => InputTypes.Int128,
                        _ => throw new ArgumentException($"Invalid -type argument {typeName}"),
                    };
                    sign = signedness;
                    type = typeSize;
                }

                else if ("-group".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    grouping = true;
                }
                else if ("-nogroup".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    grouping = false;
                }

                else if ("-padding".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    paddingMode = Enum.Parse<PaddingMode>(args[++i], ignoreCase: true);
                }

                else if ("-upper".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    upper = true;
                }
                else if ("-lower".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    upper = false;
                }

                else if ("-index".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    index = true;
                }
                else if ("-noindex".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    index = false;
                }

                else if ("-numpad".Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    fakeNumpad = true;
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

        var program = new Program(
            format: format,
            sign: sign,
            type: type,
            grouping: grouping,
            paddingMode: paddingMode,
            upper: upper,
            index: index,
            fakeNumpad: fakeNumpad);
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
                if ((HandleHelpKeys(key, out RefreshFlags refreshFlags)) ||
                    (HandleModeKeys(key, out refreshFlags)) ||
                    (HandleEditKeys(key, out refreshFlags)) ||
                    (HandleCommentKeys(key, out refreshFlags)) ||
                    (HandleStackKeys(key, out refreshFlags)) ||
                    (HandleOperators(key, out refreshFlags)) ||
                    (HandleFakeNumpadKeys(key, out refreshFlags)) ||
                    (HandleInputKeys(key, out refreshFlags)) ||
                    (HandleInputKeys2(key, out refreshFlags))) {
                    Refresh(refreshFlags);
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

    bool HandleHelpKeys(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Screen;
        switch (key.Key) {
            case ConsoleKey.F1 when key.Modifiers == ConsoleModifiers.None:
                Console.Clear();
                Console.WriteLine("""
                    Mode:
                    F5-F8 = hex/dec/oct/bin                  F2 = toggle signed/unsigned
                    F3/F4 = reduce/increase word size        Shift+F4 = extend (inv. signedness)
                    Ctrl+9 = toggle digit grouping           Ctrl+0 = toggle zero pad
                    Ctrl+1 = toggle upper/lowercase hex      Ctrl+2 = print index

                    Commenting:
                    Append `;` to add a comment              Use `index:comment` to set comment
                    " = swap comment of index

                    Stack:
                    Up/Down = rotate stack                   Delete = edit last
                    Shift+Delete = delete last               Ctrl+Delete = clear all
                    z/s = extract/swap                       Shift+Enter = pick
                    p = display top

                    Operators:
                    +_*/% = basic operators                  &|^~<> = bitwise logic
                    Alt+Shift+</> = shift (inv. signedness)  Shift+[/] = rotate left/right
                    Shift+9/0 = mask left/right              Alt+Shift+9/0 = count lead/trail 0s
                    Shift+3/Alt+Shift+3 = align up/down      Shift+4 = popcount

                    """);
                Pause();
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleModeKeys(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Status | RefreshFlags.Stack;
        switch (key.Key) {
            case ConsoleKey.F2 when key.Modifiers == ConsoleModifiers.None:
                if (_sign == DisplaySignedness.Signed)
                    _sign = DisplaySignedness.Unsigned;
                else
                    _sign = DisplaySignedness.Signed;
                break;
            case ConsoleKey.F3 when key.Modifiers == ConsoleModifiers.None:
            case ConsoleKey.F3 when key.Modifiers == ConsoleModifiers.Shift: {
                    if (_type == InputTypes.Int16) {
                        _type = InputTypes.Int8;
                        _calc = _calc.ConvertTo(typeof(sbyte), false);
                    }
                    else if (_type == InputTypes.Int32) {
                        _type = InputTypes.Int16;
                        _calc = _calc.ConvertTo(typeof(short), false);
                    }
                    else if (_type == InputTypes.Int64) {
                        _type = InputTypes.Int32;
                        _calc = _calc.ConvertTo(typeof(int), false);
                    }
                    else if (_type == InputTypes.Int128) {
                        _type = InputTypes.Int64;
                        _calc = _calc.ConvertTo(typeof(long), false);
                    }
                    break;
                }
            case ConsoleKey.F4 when key.Modifiers == ConsoleModifiers.None:
            case ConsoleKey.F4 when key.Modifiers == ConsoleModifiers.Shift: {
                    var shifted = key.Modifiers.HasFlag(ConsoleModifiers.Shift);
                    var signExtend = (_sign == DisplaySignedness.Signed) ^ shifted;
                    if (_type == InputTypes.Int8) {
                        _type = InputTypes.Int16;
                        _calc = _calc.ConvertTo(typeof(short), signExtend);
                    }
                    else if (_type == InputTypes.Int16) {
                        _type = InputTypes.Int32;
                        _calc = _calc.ConvertTo(typeof(int), signExtend);
                    }
                    else if (_type == InputTypes.Int32) {
                        _type = InputTypes.Int64;
                        _calc = _calc.ConvertTo(typeof(long), signExtend);
                    }
                    else if (_type == InputTypes.Int64) {
                        _type = InputTypes.Int128;
                        _calc = _calc.ConvertTo(typeof(Int128), signExtend);
                    }
                    break;
                }
            case ConsoleKey.F5 when key.Modifiers == ConsoleModifiers.None:
                _format = DisplayFormat.Hexadecimal;
                break;
            case ConsoleKey.F6 when key.Modifiers == ConsoleModifiers.None:
                _format = DisplayFormat.Decimal;
                break;
            case ConsoleKey.F7 when key.Modifiers == ConsoleModifiers.None:
                _format = DisplayFormat.Octal;
                break;
            case ConsoleKey.F8 when key.Modifiers == ConsoleModifiers.None:
                _format = DisplayFormat.Binary;
                break;
            case ConsoleKey.D9 when key.Modifiers == ConsoleModifiers.Control:
                _grouping = !_grouping;
                break;
            case ConsoleKey.D0 when key.Modifiers == ConsoleModifiers.Control:
                _paddingMode = _paddingMode switch {
                    PaddingMode.None => PaddingMode.RightJustified,
                    PaddingMode.RightJustified => PaddingMode.ZeroPadded,
                    PaddingMode.ZeroPadded => PaddingMode.None,
                    _ => PaddingMode.None,
                };
                break;
            case ConsoleKey.D1 when key.Modifiers == ConsoleModifiers.Control:
                _upper = !_upper;
                break;
            case ConsoleKey.D2 when key.Modifiers == ConsoleModifiers.Control:
                _index = !_index;
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleEditKeys(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Input;
        switch (key.Key) {
            case ConsoleKey.Enter when key.Modifiers == ConsoleModifiers.None:
                PushInput();
                flags = RefreshFlags.Stack | RefreshFlags.Input;
                break;
            case ConsoleKey.D when key.Modifiers == ConsoleModifiers.Control:
                if (_input.Length == 0)
                    _exit = true;
                break;
            case ConsoleKey.L when key.Modifiers == ConsoleModifiers.Control:
                flags = RefreshFlags.Screen;
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
                ResetInput();
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleCommentKeys(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Stack | RefreshFlags.Input;
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

        flags = RefreshFlags.Input;
        if (!_comment)
            return false;
        if (key.KeyChar == '\0')
            return false;
        if (key.Modifiers != ConsoleModifiers.None && key.Modifiers != ConsoleModifiers.Shift)
            return false;
        _input.Insert(_inputCursor++, key.KeyChar);
        return true;
    }

    bool HandleStackKeys(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Stack | RefreshFlags.Input;
        switch (key.Key) {
            case ConsoleKey.UpArrow when key.Modifiers == ConsoleModifiers.None:
                _calc.DoStackOp(StackOperation.Roll, -1);
                break;
            case ConsoleKey.DownArrow when key.Modifiers == ConsoleModifiers.None:
                _calc.DoStackOp(StackOperation.Roll, 1);
                break;
            case ConsoleKey.Delete when key.Modifiers == ConsoleModifiers.None:
                if (_input.Length != 0)
                    throw new InvalidOperationException("Still editing");

                var entry = _calc.Peek();
                _calc.DoStackOp(StackOperation.Drop, 1);
                try {
                    FormatValueRaw(_input, entry.Object, _format, _sign, 0, PaddingMode.None, _upper);
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
                var val = _calc.Peek();
                PrintValue(val.Object);
                flags = RefreshFlags.Screen;
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleOperators(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Stack | RefreshFlags.Input;
        switch (key.Key) {
            case ConsoleKey.OemPlus when key.Modifiers == ConsoleModifiers.Shift:
            case ConsoleKey.Add:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.Add);
                break;
            case ConsoleKey.OemMinus when key.Modifiers == ConsoleModifiers.Shift:
            case ConsoleKey.Subtract:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.Subtract);
                break;
            case ConsoleKey.D8 when key.Modifiers == ConsoleModifiers.Shift:
            case ConsoleKey.Multiply:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.Multiply);
                break;
            case ConsoleKey.Oem2 when key.Modifiers == ConsoleModifiers.None:
            case ConsoleKey.Divide:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.Divide);
                break;
            case ConsoleKey.D5 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.Remainder);
                break;
            case ConsoleKey.D7 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.And);
                break;
            case ConsoleKey.Oem5 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.Or);
                break;
            case ConsoleKey.D6 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.Xor);
                break;
            case ConsoleKey.OemComma when key.Modifiers == ConsoleModifiers.Shift:
            case ConsoleKey.OemComma when key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt):
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.ShiftLeft);
                break;
            case ConsoleKey.OemPeriod when key.Modifiers == ConsoleModifiers.Shift:
            case ConsoleKey.OemPeriod when key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt): {
                    var alt = key.Modifiers.HasFlag(ConsoleModifiers.Alt);
                    var signExtend = (_sign == DisplaySignedness.Signed) ^ alt;
                    PushInput();
                    if (signExtend)
                        _calc.DoBinaryOp(BinaryOperation.ShiftRightArithmetic);
                    else
                        _calc.DoBinaryOp(BinaryOperation.ShiftRight);
                    break;
                }
            case ConsoleKey.Oem4 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.RotateLeft);
                break;
            case ConsoleKey.Oem6 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.RotateRight);
                break;
            case ConsoleKey.D3 when key.Modifiers == ConsoleModifiers.Shift:
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.AlignUp);
                break;
            case ConsoleKey.D3 when key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt):
                PushInput();
                _calc.DoBinaryOp(BinaryOperation.AlignDown);
                break;
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
            case ConsoleKey.D9 when key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt):
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.CountLeadingZeroes);
                break;
            case ConsoleKey.D0 when key.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt):
                PushInput();
                _calc.DoUnaryOp(UnaryOperation.CountTrailingZeroes);
                break;
            default:
                return false;
        }
        return true;
    }

    bool HandleFakeNumpadKeys(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Input;
        if (!_fakeNumpad || key.Modifiers != ConsoleModifiers.None)
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

    bool HandleInputKeys(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Input;
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

    bool HandleInputKeys2(ConsoleKeyInfo key, out RefreshFlags flags) {
        flags = RefreshFlags.Input;
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

    UInt128 ParseOctal(IEnumerable<char> v) {
        var result = UInt128.Zero;
        foreach (var c in v) {
            unchecked {
                var cv = "01234567".IndexOf(c);
                if (cv < 0)
                    throw new InvalidDataException("Invalid octal string");
                result = result * 8 + (UInt128)cv;
            }
        }
        return result;
    }

    void PushInput(bool stack = false) {
        if (_input.Length == 0)
            return;

        var original = _input.ToString();
        var commentIndex = original.IndexOfAny(';', ':');
        string? comment = null;
        bool isOverrideComment = false;
        if (commentIndex >= 0) {
            _input.Remove(commentIndex, _input.Length - commentIndex);
            comment = original[(commentIndex + 1)..].Trim();
            isOverrideComment = original[commentIndex] == ':';
        }

        _input.Replace(" ", null);
        try {
            if (stack) {
                _calc.Push(Int128.Parse(_input.ToString()), comment, null);
            }
            else {
                var realFormat = _format;
                var explicitFormat = false;
                var negative = false;

                if (_input.Length == 0)
                    throw new ArgumentException("Found a comment but input is empty");
                if (_input[0] == '-') {
                    negative = true;
                    _input.Remove(0, 1);
                }
                if (_input.Length > 2) {
                    explicitFormat = true;
                    switch (new string([_input[0], _input[1]]).ToLowerInvariant()) {
                        case "0x":
                        case "0h":
                            realFormat = DisplayFormat.Hexadecimal;
                            break;
                        case "0n":
                            realFormat = DisplayFormat.Decimal;
                            break;
                        case "0o":
                        case "0t":
                            realFormat = DisplayFormat.Octal;
                            break;
                        case "0y":
                        case "0b" when _format == DisplayFormat.Decimal:
                            realFormat = DisplayFormat.Binary;
                            break;
                        default:
                            explicitFormat = false;
                            break;
                    }
                    if (explicitFormat)
                        _input.Remove(0, 2);
                }
                if (!explicitFormat) {
                    explicitFormat = true;
                    switch (char.ToLowerInvariant(_input[^1])) {
                        case 'x':
                        case 'h':
                            realFormat = DisplayFormat.Hexadecimal;
                            break;
                        case 'n':
                            realFormat = DisplayFormat.Decimal;
                            break;
                        case 'o':
                        case 't':
                            realFormat = DisplayFormat.Octal;
                            break;
                        case 'y':
                            realFormat = DisplayFormat.Binary;
                            break;
                        default:
                            explicitFormat = false;
                            break;
                    }
                    if (explicitFormat)
                        _input.Remove(_input.Length - 1, 1);
                }
                var raw = realFormat switch {
                    DisplayFormat.Hexadecimal => UInt128.Parse(_input.ToString(), NumberStyles.AllowHexSpecifier),
                    DisplayFormat.Decimal => UInt128.Parse(_input.ToString(), NumberStyles.None),
                    DisplayFormat.Octal => ParseOctal(_input.ToString()),
                    DisplayFormat.Binary => UInt128.Parse(_input.ToString(), NumberStyles.AllowBinarySpecifier),
                    _ => throw new NotImplementedException(),
                };
                if (negative)
                    _calc.Push(Int128.CreateTruncating(-raw), comment, null);
                else
                    _calc.Push(Int128.CreateTruncating(raw), comment, null);
            }

            if (isOverrideComment) {
                try {
                    _calc.DoStackOp(StackOperation.SetComment, null);
                }
                catch {
                    _calc.DoStackOp(StackOperation.Drop, 1);
                    throw;
                }
            }
        }
        finally {
            ResetInput();
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

    void FormatValueRaw(
        StringBuilder sb,
        object value,
        DisplayFormat format,
        DisplaySignedness sign,
        int group,
        PaddingMode paddingMode,
        bool upper) {

        if (format == DisplayFormat.Octal) {
            FormatOctalRaw(sb, value, paddingMode: paddingMode);
        }
        else {
            var size = GetPadSize(paddingMode);

            if (sign == DisplaySignedness.Unsigned)
                value = IntConverter.ToUInt128(value);

            sb.AppendFormat(format switch {
                DisplayFormat.Hexadecimal => upper ? $"{{0:X{size * 2}}}" : $"{{0:x{size * 2}}}",
                DisplayFormat.Decimal => "{0:G}",
                DisplayFormat.Binary => $"{{0:B{size * 8}}}",
                _ => throw new InvalidOperationException(),
            }, value);
        }

        if (group > 0) {
            group = format switch {
                DisplayFormat.Hexadecimal => 4,
                DisplayFormat.Decimal => 3,
                DisplayFormat.Octal => 3,
                DisplayFormat.Binary => 4,
                _ => throw new InvalidOperationException(),
            };
            for (int i = 0; i < sb.Length; i++)
                if ((i + 1) % (group + 1) == 0 && (i != sb.Length - 1 || sb[0] != '-'))
                    sb.Insert(sb.Length - i, ' ');
        }
    }

    void FormatValue(StringBuilder sb, IStackEntry value) {
        var group = _format switch {
            DisplayFormat.Hexadecimal => 4,
            DisplayFormat.Decimal => 3,
            DisplayFormat.Octal => 3,
            DisplayFormat.Binary => 4,
            _ => throw new InvalidOperationException(),
        };
        if (!_grouping)
            group = 0;
        FormatValueRaw(
            sb,
            value.Object,
            format: _format,
            sign: _sign,
            group: group,
            paddingMode: _paddingMode,
            upper: _upper);
        if (value.Comment != null) {
            sb.Append(" ; ");
            sb.Append(value.Comment);
        }
    }

    void FormatBinaryFancy(StringBuilder sb, object value) {
        var bit = _calc.WordBytes * 8;
        var val = IntConverter.ToUInt128(value);

        while (bit >= 32) {
            sb.Append($"{bit - 1,-3}          {bit - 8,3}    {bit - 9,-3}          {bit - 16,3}    ");
            sb.Append($"{bit - 17,-3}          {bit - 24,3}    {bit - 25,-3}          {bit - 32,3}\n");
            sb.AppendFormat("{0:B32}", (val >> (bit - 32)) & (UInt128)0xffffffffu);
            for (int i = 31; i >= 0; i--)
                sb.Insert(sb.Length - i, ' ');
            for (int i = 64 - 8; i >= 0; i -= 8)
                sb.Insert(sb.Length - i, ' ');
            for (int i = 72 - 18; i >= 0; i -= 18)
                sb.Insert(sb.Length - i, "  ");
            sb.Append('\n');
            sb.Append('\n');
            bit -= 32;
        }
        if (bit >= 16) {
            sb.Append($"{bit - 1,-3}          {bit - 8,3}    {bit - 9,-3}          {bit - 16,3}\n");
            sb.AppendFormat("{0:B16}", (val >> (bit - 16)) & (UInt128)0xffffu);
            for (int i = 15; i >= 0; i--)
                sb.Insert(sb.Length - i, ' ');
            for (int i = 32 - 8; i >= 0; i -= 8)
                sb.Insert(sb.Length - i, ' ');
            for (int i = 36 - 18; i >= 0; i -= 18)
                sb.Insert(sb.Length - i, "  ");
            sb.Append('\n');
            sb.Append('\n');
            bit -= 16;
        }
        if (bit >= 8) {
            sb.Append($"{bit - 1,-3}          {bit - 8,3}\n");
            sb.AppendFormat("{0:B8}", (val >> (bit - 8)) & (UInt128)0xffu);
            for (int i = 7; i >= 0; i--)
                sb.Insert(sb.Length - i, ' ');
            for (int i = 16 - 8; i >= 0; i -= 8)
                sb.Insert(sb.Length - i, ' ');
            sb.Append('\n');
            sb.Append('\n');
        }
    }

    void PrintValue(object value) {
        var sb = new StringBuilder(1024);

        Console.Clear();

        Write("Hex:");
        sb.Clear();
        FormatValueRaw(sb, value, DisplayFormat.Hexadecimal, DisplaySignedness.Unsigned, 4, _paddingMode, _upper);
        Write(sb.ToString());

        Write("");
        Write("Dec (unsigned):");
        sb.Clear();
        FormatValueRaw(sb, value, DisplayFormat.Decimal, DisplaySignedness.Unsigned, 3, _paddingMode, _upper);
        Write(sb.ToString());
        Write("Dec (signed):");
        sb.Clear();
        FormatValueRaw(sb, value, DisplayFormat.Decimal, DisplaySignedness.Signed, 3, _paddingMode, _upper);
        Write(sb.ToString());

        Write("");
        Write("Oct:");
        sb.Clear();
        FormatValueRaw(sb, value, DisplayFormat.Octal, _sign, 3, _paddingMode, _upper);
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
            Write("...");
            printable--;
        }

        var stackItems = calc.GetStackItems(Math.Min(printable, calc.Count)).ToList();
        var maxLength = 0;
        List<string> printed = [];
        var sb = new StringBuilder(16 * 9 + 8);

        for (int i = 0; i < stackItems.Count; i++) {
            sb.Clear();
            FormatValue(sb, stackItems[stackItems.Count - i - 1]);
            maxLength = Math.Max(maxLength, sb.Length);
            printed.Add(sb.ToString());
        }

        for (int i = 0; i < printed.Count; i++) {
            var value = printed[i];
            if (_paddingMode == PaddingMode.RightJustified)
                value = value.PadLeft(maxLength);
            if (_index)
                value = $"{printed.Count - i,4}  " + value;
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
        try {
            Console.CursorVisible = false;

            if (flags.HasFlag(RefreshFlags.Status)) {
                Console.SetCursorPosition(0, 0);
                var mode = _format switch {
                    DisplayFormat.Hexadecimal => "*Hex* F6   F7   F8  ",
                    DisplayFormat.Decimal => " F5  *Dec* F7   F8  ",
                    DisplayFormat.Octal => " F5   F6  *Oct* F8  ",
                    DisplayFormat.Binary => " F5   F6   F7  *Bin*",
                    _ => throw new Exception("Unexpected format"),
                };
                Write(string.Format(
                    "{0}{1,-6} (F2/F3/F4)  {2}  {3,5} {4,5} {5} (Ctrl+9/0/1)",
                    _sign == DisplaySignedness.Signed ? "S" : "U",
                    _type,
                    mode,
                    _grouping ? "Group" : "Ungrp",
                    _paddingMode switch {
                        PaddingMode.None => "Left",
                        PaddingMode.RightJustified => "Right",
                        PaddingMode.ZeroPadded => "Pad",
                        _ => throw new NotImplementedException(),
                    },
                    _upper ? "Upper" : "Lower"));
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
                    Write(ex.Message, width: Console.WindowWidth - 1);
                    Console.Beep();
                }
                else {
                    PrintInputLine();
                }
            }
        }
        finally {
            Console.CursorVisible = true;
        }
    }
}
