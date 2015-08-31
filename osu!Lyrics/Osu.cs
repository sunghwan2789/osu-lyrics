using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Linq;

namespace osu_Lyrics
{
    internal class Osu
    {
        #region Process

        private static Process _process;

        public static Process Process
        {
            get
            {
                if (_process != null)
                {
                    while (_process.MainWindowHandle == IntPtr.Zero)
                    {
                        Thread.Sleep(1000);
                    }
                    return _process;
                }

                // osu!가 실행 중인지 확인하고 _process로 연결
                _process = Process.GetProcessesByName("osu!").FirstOrDefault();
                if (_process != null)
                {
                    return Process;
                }

                // osu!가 실행 중이 아니므로 하나 띄운다
                var exec = Registry.GetValue(@"HKEY_CLASSES_ROOT\osu!\shell\open\command", null, null);
                if (exec != null)
                {
                    _process = Process.Start(exec.ToString().Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries)[0]);
                    return Process;
                }

                MessageBox.Show("osu!가 설치되어있나요?");
                Application.Exit();
                return null;
            }
        }

        #endregion

        #region Show()

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// 창이 활성화되어 있는지 확인하거나 활성화함.
        /// 이미 창이 활성화되어 있다면 false를 반환.
        /// </summary>
        /// <returns>bool</returns>
        public static bool Show(bool checkOnly = false)
        {
            const int SW_SHOWNORMAL = 1;

            if (GetForegroundWindow() == Process.MainWindowHandle)
            {
                return false;
            }
            if (!checkOnly)
            {
                ShowWindow(Process.MainWindowHandle, SW_SHOWNORMAL);
                SetForegroundWindow(Process.MainWindowHandle);
            }
            return Lyrics.Settings == null || !Lyrics.Settings.Visible;
        }

        #endregion

        #region HookKeyboard(Action<Keys> action), UnhookKeyboard()

        // DECLARED
        //[DllImport("user32.dll")]
        //private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HOOKPROC lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        private delegate IntPtr HOOKPROC(int code, IntPtr wParam, IntPtr lParam);

        private static IntPtr _hhkk; // hookHandleKeyKeyboard
        private static HOOKPROC _hpk; // hookProcKeyboard
        private static Func<Keys, bool> _hak; // hookActionKeyboard

        public static void HookKeyboard(Func<Keys, bool> action)
        {
            const int WH_KEYBOARD_LL = 13;

            if (_hhkk != IntPtr.Zero)
            {
                throw new StackOverflowException();
            }

            _hak = action;
            _hpk = new HOOKPROC(LowLevelKeyboardProc);
            _hhkk = SetWindowsHookEx(WH_KEYBOARD_LL, _hpk, IntPtr.Zero, 0);
        }

        private static IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int HC_ACTION = 0;
            const int WM_KEYDOWN = 0x100;
            const int WM_SYSKEYDOWN = 0x104;

            if (!Show(true) && nCode == HC_ACTION)
            {
                var state = wParam.ToInt32();
                // 설정 중이면 키보드 후킹 안 하기!
                if (Lyrics.Settings == null &&
                    (state == WM_KEYDOWN || state == WM_SYSKEYDOWN) &&
                    _hak((Keys) Marshal.ReadInt32(lParam)) && Settings.SuppressKey)
                {
                    // 설정 중 "핫키 전송 막기" 활성화시 osu!로 핫기 전송 막는 부분..
                    return (IntPtr) 1;
                }
            }
            return CallNextHookEx(_hhkk, nCode, wParam, lParam);
        }

        public static void UnhookKeyboard()
        {
            UnhookWindowsHookEx(_hhkk);
        }

        #endregion

        #region Listen(Action<string[]> onSignal)

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int flAllocationType, int flProtect);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int dwFreeType);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, string lpBuffer, int nSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, int dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, int dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        
        private static bool InjectDLL(string dllPath)
        {
            const int PROCESS_ALL_ACCESS = 0x1F0FFF;
            const int MEM_RESERVE = 0x2000;
            const int MEM_COMMIT = 0x1000;
            const int PAGE_READWRITE = 0x04;
            const int INFINITE = unchecked((int) 0xFFFFFFFF);
            const int MEM_RELEASE = 0x8000;
            
            var hProcess = OpenProcess(PROCESS_ALL_ACCESS, true, Process.Id);

            var pLoadLibrary = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            var pDllPath = VirtualAllocEx(hProcess, IntPtr.Zero, dllPath.Length + 1, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            WriteProcessMemory(hProcess, pDllPath, dllPath, dllPath.Length + 1, IntPtr.Zero);

            var hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibrary, pDllPath, 0, IntPtr.Zero);
            WaitForSingleObject(hThread, INFINITE);
            CloseHandle(hThread);

            VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);

            CloseHandle(hProcess);

            return hThread != IntPtr.Zero;
        }

        public static bool Listen(Action<string> onSignal)
        {
            // dll의 fileVersion을 바탕으로 버전별로 겹치지 않는 경로에 압축 풀기:
            // 시스템 커널에 이전 버전의 dll이 같은 이름으로 남아있을 수 있음
            Program.Extract(Assembly.GetExecutingAssembly().GetManifestResourceStream("osu_Lyrics.Server.dll"), Settings._Server);
            var dest = Settings._Server + "." + FileVersionInfo.GetVersionInfo(Settings._Server).FileVersion;
            Program.Move(Settings._Server, dest);
            if (!InjectDLL(dest))
            {
                return false;
            }

            // 백그라운드에서 서버로부터 데이터를 받아 전달
            Task.Run(() =>
            {
                using (var pipe = new NamedPipeClientStream(".", "osu!Lyrics", PipeDirection.In, PipeOptions.None))
                using (var sr = new StreamReader(pipe))
                {
                    pipe.Connect();
                    while (pipe.IsConnected)
                    {
                        try
                        {
                            onSignal(sr.ReadLine());
                        }
                        catch {}
                    }
                }
            });
            return true;
        }

        #endregion

        #region WindowInfo()

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public struct WNDINFO
        {
            public Point Location;
            public Size ClientSize;
        }

        public static WNDINFO WindowInfo()
        {
            Point location;
            ClientToScreen(Process.MainWindowHandle, out location);

            RECT rect;
            GetWindowRect(Process.MainWindowHandle, out rect);
            var border = location.X - rect.left;
            var title = location.Y - rect.top;

            return new WNDINFO
            {
                Location = location,
                ClientSize = new Size(rect.right - rect.left - border * 2, rect.bottom - rect.top - border - title)
            };
        }

        #endregion
    }
}