// Derived from https://github.com/dotnet/runtime/blob/cd5f273267e4363720d76e28685c0e9bbb268d08/src/libraries/System.Console/src/System/ConsolePal.Windows.cs

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Console;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace ProCalcConsole;

[SupportedOSPlatform("windows")]
static class ConsoleEx {
    static readonly HANDLE InputHandle = PInvoke.GetStdHandle(STD_HANDLE.STD_INPUT_HANDLE);
    static readonly HANDLE OutputHandle = PInvoke.GetStdHandle(STD_HANDLE.STD_OUTPUT_HANDLE);
    static INPUT_RECORD _cachedInputRecord;

    static bool IsReadKeyEvent(ref INPUT_RECORD ir) {
        if (ir.EventType != PInvoke.KEY_EVENT) {
            // Skip non key events.
            return false;
        }

        if (ir.Event.KeyEvent.bKeyDown == false) {
            // The only keyup event we don't skip is Alt keyup with a synthesized unicode char,
            // which is either the result of an Alt+Numpad key sequence, an IME-generated char,
            // or a pasted char without a matching key.
            return ir.Event.KeyEvent.wVirtualKeyCode == (ushort)VIRTUAL_KEY.VK_MENU && ir.Event.KeyEvent.uChar.UnicodeChar != 0;
        }
        else {
            // Keydown event. Some of these we need to skip as well.
            ushort keyCode = ir.Event.KeyEvent.wVirtualKeyCode;
            if (keyCode is >= 0x10 and <= 0x12) {
                // Skip modifier keys Shift, Control, Alt.
                return false;
            }

            if (keyCode is 0x14 or 0x90 or 0x91) {
                // Skip CapsLock, NumLock, and ScrollLock keys,
                return false;
            }

            var keyState = ir.Event.KeyEvent.dwControlKeyState;
            if ((keyState & (PInvoke.LEFT_ALT_PRESSED | PInvoke.RIGHT_ALT_PRESSED)) != 0) {
                // Possible Alt+NumPad unicode key sequence which surfaces by a subsequent
                // Alt keyup event with uChar (see above).
                ConsoleKey key = (ConsoleKey)keyCode;
                if (key is >= ConsoleKey.NumPad0 and <= ConsoleKey.NumPad9) {
                    // Alt+Numpad keys (as received if NumLock is on).
                    return false;
                }

                // If Numlock is off, the physical Numpad keys are received as navigation or
                // function keys. The EnhancedKey flag tells us whether these virtual keys
                // really originate from the numpad, or from the arrow pad / control pad.
                if ((keyState & PInvoke.ENHANCED_KEY) == 0) {
                    // If the EnhancedKey flag is not set, the following virtual keys originate
                    // from the numpad.
                    if (key is ConsoleKey.Clear or ConsoleKey.Insert) {
                        // Skip Clear and Insert (usually mapped to Numpad 5 and 0).
                        return false;
                    }

                    if (key is >= ConsoleKey.PageUp and <= ConsoleKey.DownArrow) {
                        // Skip PageUp/Down, End/Home, and arrow keys.
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public static object ReadConsoleInput() {
        INPUT_RECORD ir;
        bool r;
        uint numEventsRead;

        if (_cachedInputRecord.EventType == PInvoke.KEY_EVENT) {
            // We had a previous keystroke with repeated characters.
            ir = _cachedInputRecord;
            if (_cachedInputRecord.Event.KeyEvent.wRepeatCount == 0)
                _cachedInputRecord.EventType = 0;
            else {
                _cachedInputRecord.Event.KeyEvent.wRepeatCount--;
            }
            // We will return one key from this method, so we decrement the
            // repeatCount here, leaving the cachedInputRecord in the "queue".

        }
        else { // We did NOT have a previous keystroke with repeated characters:

            while (true) {
                unsafe {
                    r = PInvoke.ReadConsoleInput(InputHandle, &ir, 1, &numEventsRead);
                }
                if (!r) {
                    // This will fail when stdin is redirected from a file or pipe.
                    // We could theoretically call Console.Read here, but I
                    // think we might do some things incorrectly then.
                    throw new InvalidOperationException();
                }

                if (numEventsRead == 0) {
                    // This can happen when there are multiple console-attached
                    // processes waiting for input, and another one is terminated
                    // while we are waiting for input.
                    //
                    // (This is "almost certainly" a bug, but behavior has been
                    // this way for a long time, so we should handle it:
                    // https://github.com/microsoft/terminal/issues/15859)
                    //
                    // (It's a rare case to have multiple console-attached
                    // processes waiting for input, but it can happen sometimes,
                    // such as when ctrl+c'ing a build process that is spawning
                    // tons of child processes--sometimes, due to the order in
                    // which processes exit, a managed shell process (like pwsh)
                    // might get back to the prompt and start trying to read input
                    // while there are still child processes getting cleaned up.)
                    //
                    // In this case, we just need to retry the read.
                    continue;
                }

                if (ir.EventType == PInvoke.WINDOW_BUFFER_SIZE_EVENT) {
                    return new ConsoleResizeInfo(ir.Event.WindowBufferSizeEvent.dwSize.X, ir.Event.WindowBufferSizeEvent.dwSize.Y);
                }
                else if (!IsReadKeyEvent(ref ir)) {
                    continue;
                }

                if (ir.Event.KeyEvent.wRepeatCount > 1) {
                    ir.Event.KeyEvent.wRepeatCount--;
                    _cachedInputRecord = ir;
                }
                break;
            }
        }  // we did NOT have a previous keystroke with repeated characters.

        var state = ir.Event.KeyEvent.dwControlKeyState;
        bool shift = (state & PInvoke.SHIFT_PRESSED) != 0;
        bool alt = (state & (PInvoke.LEFT_ALT_PRESSED | PInvoke.RIGHT_ALT_PRESSED)) != 0;
        bool control = (state & (PInvoke.LEFT_CTRL_PRESSED | PInvoke.RIGHT_CTRL_PRESSED)) != 0;

        ConsoleKeyInfo info = new(ir.Event.KeyEvent.uChar.UnicodeChar, (ConsoleKey)ir.Event.KeyEvent.wVirtualKeyCode, shift, alt, control);

        return info;
    }

    static uint SetConsoleMode(HANDLE handle, Func<uint, uint> cb) {
        CONSOLE_MODE oldMode, newMode;
        unsafe {
            if (!PInvoke.GetConsoleMode(handle, &oldMode))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            newMode = (CONSOLE_MODE)cb((uint)oldMode);
            if (!PInvoke.SetConsoleMode(handle, newMode))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
        return (uint)oldMode;
    }

    public static (uint, uint) EnableTuiMode() {
        var oldIm = SetConsoleMode(InputHandle, mode =>
            (uint)CONSOLE_MODE.ENABLE_WINDOW_INPUT);
        var oldOm = SetConsoleMode(OutputHandle, mode =>
            (uint)CONSOLE_MODE.ENABLE_PROCESSED_OUTPUT |
            (uint)CONSOLE_MODE.ENABLE_WRAP_AT_EOL_OUTPUT |
            (uint)CONSOLE_MODE.ENABLE_VIRTUAL_TERMINAL_PROCESSING |
            (uint)CONSOLE_MODE.DISABLE_NEWLINE_AUTO_RETURN);
        return (oldIm, oldOm);
    }

    public static void RestoreMode(uint im, uint om) {
        SetConsoleMode(InputHandle, _ => im);
        SetConsoleMode(OutputHandle, _ => om);
    }
}
