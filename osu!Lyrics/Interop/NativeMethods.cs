using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace osu_Lyrics.Interop
{
    internal static class NativeMethods
    {
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport(ExternDll.Gdi32, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size pSizeDst, IntPtr hdcSrc, ref Point pptSrc, int crKey, ref BLENDFUNCTION pBlend, int dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        public const int AC_SRC_OVER = 0x00;
        public const int AC_SRC_ALPHA = 0x01;
        public const int ULW_COLORKEY = 0x00000001;
        public const int ULW_ALPHA = 0x00000002;
        public const int ULW_OPAQUE = 0x00000004;

        [DllImport(ExternDll.User32)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.Gdi32, SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.Gdi32, SetLastError = true)]
        public static extern bool DeleteDC(IntPtr hDC);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        public const int PROCESS_TERMINATE = 0x0001;
        public const int PROCESS_CREATE_THREAD = 0x0002;
        public const int PROCESS_SET_SESSIONID = 0x0004;
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int PROCESS_DUP_HANDLE = 0x0040;
        public const int PROCESS_CREATE_PROCESS = 0x0080;
        public const int PROCESS_SET_QUOTA = 0x0100;
        public const int PROCESS_SET_INFORMATION = 0x0200;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const int SYNCHRONIZE = 0x00100000;
        public const int PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF;

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int flAllocationType, int flProtect);
        
        public const int MEM_COMMIT = 0x00001000;
        public const int MEM_RESERVE = 0x00002000;
        public const int MEM_DECOMMIT = 0x00004000;
        public const int MEM_RELEASE = 0x00008000;
        public const int MEM_FREE = 0x00010000;

        public const int PAGE_NOACCESS = 0x01;
        public const int PAGE_READONLY = 0x02;
        public const int PAGE_READWRITE = 0x04;
        public const int PAGE_WRITECOPY = 0x08;
        public const int PAGE_EXECUTE = 0x10;
        public const int PAGE_EXECUTE_READ = 0x20;
        public const int PAGE_EXECUTE_READWRITE = 0x40;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int dwFreeType);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

        //TODO
        [DllImport(ExternDll.Kernel32)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern int WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        public const uint INFINITE = 0xFFFFFFFF;

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern uint GlobalSize(IntPtr hMem);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        //TODO
        [DllImport(ExternDll.Kernel32)]
        public static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        //TODO
        [DllImport(ExternDll.Kernel32)]
        public static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

        [DllImport(ExternDll.User32)]
        public static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport(ExternDll.User32)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(ExternDll.User32)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_NORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11;
        public const int SW_MAX = 11;

        [DllImport(ExternDll.User32)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(ExternDll.User32)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        public const int WH_KEYBOARD_LL = 13;

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        public const int HC_ACTION = 0;
        public const int HC_GETNEXT = 1;
        public const int HC_SKIP = 2;
        public const int HC_NOREMOVE = 3;
        public const int HC_NOREM = HC_NOREMOVE;
        public const int HC_SYSMODALON = 4;
        public const int HC_SYSMODALOFF = 5;

        public const int WM_KEYFIRST = 0x0100;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_DEADCHAR = 0x0103;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_SYSCHAR = 0x0106;
        public const int WM_SYSDEADCHAR = 0x0107;

        [DllImport(ExternDll.User32)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32)]
        public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);
    }
}
