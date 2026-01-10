using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using static ProCalcConsole.NativeMethods;

namespace ProCalcConsole;

[SupportedOSPlatform("windows")]
class Clipboard : IDisposable {
    bool clipboardOpened;

    public Clipboard(nint owner) {
        if (!OpenClipboard(owner)) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }
        clipboardOpened = true;
    }

    public string? GetText() {
        if (!clipboardOpened) {
            throw new InvalidOperationException();
        }

        var handle = GetClipboardData(CF_UNICODETEXT);
        if (handle == nint.Zero) {
            return null;
        }

        var ptr = GlobalLock(handle);
        if (ptr == nint.Zero) {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        try {
            var size = GlobalSize(ptr);
            if (size == 0) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            else if (size < 2) {
                return null;
            }
            else if (size > (ulong)int.MaxValue) {
                throw new InvalidDataException();
            }
            return Marshal.PtrToStringUni(ptr, (int)size / 2 - 1);
        }
        finally {
            GlobalUnlock(ptr);
        }
    }

    public void SetText(string value) {
        if (!clipboardOpened) {
            throw new InvalidOperationException();
        }

        if (string.IsNullOrEmpty(value)) {
            if (!EmptyClipboard()) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return;
        }

        nint handle = nint.Zero, ptr = nint.Zero;
        try {
            handle = GlobalAlloc(GMEM_MOVEABLE, (nuint)(value.Length * 2 + 2));
            if (handle == nint.Zero) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            ptr = GlobalLock(handle);
            if (ptr == nint.Zero) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            unsafe {
                fixed (char* raw = value) {
                    Buffer.MemoryCopy(raw, (char*)ptr, value.Length * 2 + 2, value.Length * 2);
                }
                ((char*)ptr)[value.Length] = '\0';
            }

            if (SetClipboardData(CF_UNICODETEXT, handle) == nint.Zero) {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            // release the handle to the system
            handle = nint.Zero;
        }
        finally {
            if (ptr != nint.Zero) {
                GlobalUnlock(ptr);
            }
            if (handle != nint.Zero) {
                GlobalFree(handle);
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
                CloseClipboard();
                clipboardOpened = false;
            }
            disposedValue = true;
        }
    }

    ~Clipboard() {
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
