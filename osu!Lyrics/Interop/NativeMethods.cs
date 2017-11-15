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
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport(ExternDll.Gdi32, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size pSizeDst, IntPtr hdcSrc, ref Point pptSrc, int crKey, ref BLENDFUNCTION pBlend, int dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;

            public POINT()
            {
            }

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
#if DEBUG
            public override string ToString()
            {
                return "{x=" + x + ", y=" + y + "}";
            }
#endif
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        public const int AC_SRC_OVER = 0x00000000;
        public const int AC_SRC_ALPHA = 0x00000001;
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
        
        public const int MEM_COMMIT = 0x1000;
        public const int MEM_RESERVE = 0x2000;
        public const int MEM_RELEASE = 0x8000;
        // MEM_DECOMMIT???
        public const int MEM_FREE = 0x10000;

        public const int PAGE_READWRITE = 0x04;
        public const int PAGE_READONLY = 0x02;
        public const int PAGE_WRITECOPY = 0x08;
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

        [DllImport(ExternDll.User32)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(ExternDll.User32)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport(ExternDll.User32)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32)]
        public static extern bool ClientToScreen(IntPtr hWnd, POINT lpPoint);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(ExternDll.User32, SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r)
            {
                this.left = r.Left;
                this.top = r.Top;
                this.right = r.Right;
                this.bottom = r.Bottom;
            }

            public static RECT FromXYWH(int x, int y, int width, int height)
            {
                return new RECT(x, y, x + width, y + height);
            }

            public System.Drawing.Size Size
            {
                get
                {
                    return new System.Drawing.Size(this.right - this.left, this.bottom - this.top);
                }
            }
        }

        //TODO DELETE
        public struct WNDINFO
        {
            public Point Location;
            public Size ClientSize;
        }
    }
}
