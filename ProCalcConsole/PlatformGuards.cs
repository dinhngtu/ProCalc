using System.Runtime.Versioning;

namespace ProCalcConsole {
    static class PlatformGuards {
        static readonly bool _isWindows = OperatingSystem.IsWindows();
        static readonly bool _isWindowsXP = OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600);
        static readonly bool _isWindows8 = OperatingSystem.IsWindowsVersionAtLeast(6, 2);

        [SupportedOSPlatformGuard("windows")]
        public static bool IsWindows => _isWindows;
        [SupportedOSPlatformGuard("windows5.1.2600")]
        public static bool IsWindowsXP => _isWindowsXP;
        [SupportedOSPlatformGuard("windows8.0")]
        public static bool IsWindows8 => _isWindows8;
    }
}
