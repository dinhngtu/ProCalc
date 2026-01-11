using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ProCalcConsole;

class ClipboardManager : IDisposable {
    [SupportedOSPlatformGuard("windows10.0")]
    readonly bool _guard;
    HWND _hwnd = HWND.Null;

    const string WindowClassName = "{4F17CD5A-051C-4F51-98FC-2D8548CBCCED}";

    public ClipboardManager() {
        _guard = OperatingSystem.IsWindowsVersionAtLeast(10, 0);
        if (!_guard)
            return;
        unsafe {
            fixed (char* pClassName = WindowClassName) {
                var className = new PCWSTR(pClassName);

                var wc = new WNDCLASSEXW {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                    lpfnWndProc = &WndProc,
                    hInstance = new HINSTANCE(PInvoke.GetModuleHandle(null).DangerousGetHandle()),
                    lpszClassName = className,
                };

                var atom = PInvoke.RegisterClassEx(in wc);
                if (atom == 0) {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                _hwnd = PInvoke.CreateWindowEx(
                    0,
                    className,
                    null,
                    WINDOW_STYLE.WS_OVERLAPPED,
                    0,
                    0,
                    0,
                    0,
                    HWND.HWND_MESSAGE,
                    HMENU.Null,
                    wc.hInstance,
                    null);
                if (_hwnd == HWND.Null) {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }
        }
    }

    [SupportedOSPlatform("windows10.0")]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    static LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam) {
        switch (uMsg) {
            case PInvoke.WM_DESTROY:
                return (LRESULT)0;
            default:
                return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
        }
    }

    public IClipboard? Open() {
        if (!_guard)
            return null;
        return new WindowsClipboard(_hwnd);
    }

    #region IDisposable
    bool disposedValue;

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                // dispose managed state (managed objects)
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            if (_guard && _hwnd != HWND.Null) {
                PInvoke.DestroyWindow(_hwnd);
                _hwnd = HWND.Null;
            }
            disposedValue = true;
        }
    }

    ~ClipboardManager() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
