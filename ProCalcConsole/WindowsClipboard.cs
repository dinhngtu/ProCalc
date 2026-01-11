using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Ole;

namespace ProCalcConsole;

[SupportedOSPlatform("windows10.0")]
class WindowsClipboard : IClipboard {
    bool clipboardOpened;

    internal WindowsClipboard(HWND owner) {
        if (!PInvoke.OpenClipboard(owner)) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
        clipboardOpened = true;
    }

    public string? GetText() {
        if (!clipboardOpened) {
            throw new InvalidOperationException();
        }

        unsafe {
            var handle = new HGLOBAL(PInvoke.GetClipboardData((uint)CLIPBOARD_FORMAT.CF_UNICODETEXT).Value);
            if (handle == HGLOBAL.Null) {
                return null;
            }

            var ptr = PInvoke.GlobalLock(handle);
            if (ptr == null) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            try {
                var size = PInvoke.GlobalSize(handle);
                if (size == 0) {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                else if (size < sizeof(char)) {
                    return null;
                }
                else if (size > (ulong)int.MaxValue) {
                    throw new InvalidDataException();
                }
                return Marshal.PtrToStringUni((nint)ptr, (int)(size / sizeof(char) - 1));
            }
            finally {
                PInvoke.GlobalUnlock(handle);
            }
        }
    }

    public void SetText(string value) {
        if (!clipboardOpened) {
            throw new InvalidOperationException();
        }

        if (string.IsNullOrEmpty(value)) {
            if (!PInvoke.EmptyClipboard()) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return;
        }

        unsafe {
            HGLOBAL handle = HGLOBAL.Null;
            void* ptr = null;
            try {
                var bufSize = (value.Length + 1) * sizeof(char);
                handle = PInvoke.GlobalAlloc(
                    GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE,
                    (nuint)bufSize);
                if (handle == HGLOBAL.Null) {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                ptr = PInvoke.GlobalLock(handle);
                if (ptr == null) {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                unsafe {
                    fixed (char* raw = value) {
                        Buffer.MemoryCopy(
                            raw,
                            ptr,
                            bufSize,
                            value.Length * sizeof(char));
                    }
                    ((char*)ptr)[value.Length] = '\0';
                }

                if (PInvoke.SetClipboardData(
                    (uint)CLIPBOARD_FORMAT.CF_UNICODETEXT,
                    new HANDLE(handle.Value)
                    ) == nint.Zero) {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                // release the handle to the system
                ptr = null;
                PInvoke.GlobalUnlock(handle);
                handle = HGLOBAL.Null;
            }
            finally {
                if (ptr != null && handle != HGLOBAL.Null) {
                    PInvoke.GlobalUnlock(handle);
                }
                if (handle != HGLOBAL.Null) {
                    PInvoke.GlobalFree(handle);
                }
            }
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
            if (clipboardOpened) {
                PInvoke.CloseClipboard();
                clipboardOpened = false;
            }
            disposedValue = true;
        }
    }

    ~WindowsClipboard() {
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
