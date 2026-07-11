using System;
using System.Runtime.InteropServices;

namespace OASIS.Omniverse.UnityHost.Native
{
    public static class Win32Interop
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        public const int GWL_STYLE = -16;
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_MINIMIZE = 0x20000000;
        public const int WS_MAXIMIZE = 0x01000000;
        public const int WS_SYSMENU = 0x00080000;

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll")]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
#endif

        public static IntPtr GetUnityWindowHandle()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return GetActiveWindow();
#else
            return IntPtr.Zero;
#endif
        }

        public static ulong GetAvailablePhysicalMemoryMb()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
            if (!GlobalMemoryStatusEx(ref status))
            {
                return 0;
            }

            return status.ullAvailPhys / (1024UL * 1024UL);
#else
            return 0;
#endif
        }
    }
}

