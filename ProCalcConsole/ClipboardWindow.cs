using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using static ProCalcConsole.NativeMethods;

namespace ProCalcConsole;

[SupportedOSPlatform("windows")]
class ClipboardWindow : IDisposable {
    delegate IntPtr WndProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    readonly WndProcDelegate _wndProc; // keep a reference to prevent GC collection

    public IntPtr Handle { get; private set; }

    public ClipboardWindow() {
        _wndProc = WndProc;

        var wc = new WNDCLASSEXW {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = GetModuleHandle(null),
            lpszClassName = "ClipboardWindow",
        };

        var atom = RegisterClassExW(ref wc);
        if (atom == 0) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        Handle = CreateWindowExW(
            0,
            atom,
            null,
            0,
            0,
            0,
            0,
            0,
            HWND_MESSAGE,
            IntPtr.Zero,
            wc.hInstance,
            IntPtr.Zero);

        if (Handle == IntPtr.Zero) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
    }

    IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam) {
        switch (uMsg) {
            case WM_DESTROY:
                return IntPtr.Zero;
            default:
                return DefWindowProc(hWnd, uMsg, wParam, lParam);
        }
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
            if (Handle != nint.Zero) {
                DestroyWindow(Handle);
                Handle = nint.Zero;
            }
            disposedValue = true;
        }
    }

    ~ClipboardWindow() {
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
