using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

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
                if (_process == null)
                {
                    try
                    {
                        _process = Process.GetProcessesByName("osu!")[0];
                    }
                    catch
                    {
                        try
                        {
                            _process = new Process
                            {
                                StartInfo =
                                    new ProcessStartInfo(
                                        Registry.GetValue(@"HKEY_CLASSES_ROOT\osu!\DefaultIcon\", null, null)
                                            .ToString()
                                            .Split('"')[1])
                            };
                            _process.Start();
                        }
                        catch
                        {
                            MessageBox.Show(@"osu!가 설치되어있나요?");
                            Application.Exit();
                            return null;
                        }
                    }

                    while (_process.MainWindowHandle == IntPtr.Zero)
                    {
                        Thread.Sleep(1000);
                    }
                }
                return _process;
            }
        }

        #endregion

        #region Directory

        private static string _Directory;

        public static string Directory
        {
            get { return _Directory ?? (_Directory = Path.GetDirectoryName(Process.MainModule.FileName)); }
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
                if ((state == WM_KEYDOWN || state == WM_SYSKEYDOWN) && _hak((Keys) Marshal.ReadInt32(lParam)) && Settings.SuppressKey)
                {
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
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess,
            IntPtr lpAddress,
            int dwSize,
            int flAllocationType,
            int flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess,
            IntPtr lpBaseAddress,
            string lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes,
            int dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            int dwCreationFlags,
            IntPtr lpThreadId);
        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        
        [DllImport("kernel32.dll")]
        private static extern Int32 CloseHandle(IntPtr hObject);

        private static bool InjectDLL(string dllName)
        {
            const int PROCESS_ALL_ACCESS = 0x1F0FFF;
            const int MEM_COMMIT = 0x1000;
            const int PAGE_EXECUTE_READWRITE = 0x40;

            var hProcess = OpenProcess(PROCESS_ALL_ACCESS, true, Process.Id);
            var libAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            
            var llParam = VirtualAllocEx(hProcess, IntPtr.Zero, dllName.Length + 1, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
            WriteProcessMemory(hProcess, llParam, dllName, dllName.Length + 1, IntPtr.Zero);
            var hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, libAddr, llParam, 0, IntPtr.Zero);
            
            WaitForSingleObject(hThread);
            
            CloseHandle(hThread);
            CloseHandle(hProcess);
            
            return hThread != IntPtr.Zero;
        }

        private readonly static ConcurrentQueue<string> DataQueue = new ConcurrentQueue<string>();

        public static bool Listen(Action<string[]> onSignal)
        {
            Program.Extract(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("osu_Lyrics.Server.dll"), Settings._Server);
            if (!InjectDLL(Settings._Server))
            {
                return false;
            }

            Task.Factory.StartNew(
                () =>
                {
                    using (var pipe = new NamedPipeClientStream(".", "osu!Lyrics", PipeDirection.In, PipeOptions.None))
                    using (var sr = new StreamReader(pipe))
                    {
                        pipe.Connect();
                        while (pipe.IsConnected)
                        {
                            // 파이프 통신을 할 때 버퍼가 다 차면
                            // 비동기적으로 데이터를 보내는 프로그램도 멈추므로 빨리빨리 받기!
                            DataQueue.Enqueue(sr.ReadLine());
                        }
                    }
                });
            Task.Factory.StartNew(
                () =>
                {
                    while (true)
                    {
                        string data;
                        if (DataQueue.TryDequeue(out data))
                        {
                            Lyrics.Constructor.Invoke(new MethodInvoker(() => onSignal(data.Split('|'))));
                        }
                        else
                        {
                            Thread.Sleep(100);
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

        #region GetBeatmapSetting(string osu, string key, string def = "")

        public static string GetBeatmapSetting(string osu, string key, string def = "")
        {
            var val = Regex.Match(osu, key + @".*?:([^\r\n]*)").Groups[1].Value.Trim();
            return val == "" ? def : val;
        }

        #endregion
    }
}
