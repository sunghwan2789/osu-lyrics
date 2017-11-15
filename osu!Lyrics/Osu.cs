using System;
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
using System.Text;
using static osu_Lyrics.Interop.NativeMethods;

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

        public static bool IsForeground()
        {
            if (GetForegroundWindow() == Process.MainWindowHandle)
            {
                return true;
            }
            return Lyrics.Settings?.Visible ?? false;
        }

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
            ShowWindow(Process.MainWindowHandle, SW_SHOWNORMAL);
            SetForegroundWindow(Process.MainWindowHandle);
            return !Lyrics.Settings?.Visible ?? true;
        }

        #endregion

        #region HookKeyboard(Action<Keys> action), UnhookKeyboard()

        private static IntPtr _hhkk; // hookHandleKeyKeyboard
        private static HookProc _hpk; // hookProcKeyboard
        private static Func<Keys, bool> _hak; // hookActionKeyboard

        public static void HookKeyboard(Func<Keys, bool> action)
        {
            const int WH_KEYBOARD_LL = 13;

            if (_hhkk != IntPtr.Zero)
            {
                throw new StackOverflowException();
            }

            _hak = action;
            _hpk = new HookProc(LowLevelKeyboardProc);
            _hhkk = SetWindowsHookEx(WH_KEYBOARD_LL, _hpk, IntPtr.Zero, 0);
        }

        private static IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int HC_ACTION = 0;
            const int WM_KEYDOWN = 0x100;
            const int WM_SYSKEYDOWN = 0x104;

            if (IsForeground() && nCode == HC_ACTION)
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
        
        private static bool InjectDLL(string dllPath)
        {
            var hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, Process.Id);

            var szFileName = Marshal.StringToHGlobalUni(dllPath);
            var nFileNameLength = GlobalSize(szFileName);
            var pParameter = VirtualAllocEx(hProcess, IntPtr.Zero, nFileNameLength, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            WriteProcessMemory(hProcess, pParameter, szFileName, nFileNameLength, out _);
            Marshal.FreeHGlobal(szFileName);

            var pThreadProc = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");

            var hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, pThreadProc, pParameter, 0, IntPtr.Zero);
            WaitForSingleObject(hThread, INFINITE);
            CloseHandle(hThread);

            VirtualFreeEx(hProcess, pParameter, 0, MEM_RELEASE);

            CloseHandle(hProcess);

            return hThread != IntPtr.Zero;
        }

        public static bool Listen(Action<string> onSignal)
        {
            // dll의 fileVersion을 바탕으로 버전별로 겹치지 않는 경로에 압축 풀기:
            // 시스템 커널에 이전 버전의 dll이 같은 이름으로 남아있을 수 있음
            IO.FileEx.Extract(Assembly.GetExecutingAssembly().GetManifestResourceStream("osu_Lyrics.Server.dll"), Constants._Server);
            var dest = Constants._Server + "." + FileVersionInfo.GetVersionInfo(Constants._Server).FileVersion;
            IO.FileEx.Move(Constants._Server, dest);
            if (!InjectDLL(dest))
            {
                return false;
            }

            // 백그라운드에서 서버로부터 데이터를 받아 전달
            Task.Run(() =>
            {
                using (var pipe = new NamedPipeClientStream(".", "osu!Lyrics", PipeDirection.In, PipeOptions.None))
                using (var sr = new StreamReader(pipe, Encoding.Unicode))
                {
                    pipe.Connect();
                    while (pipe.IsConnected && !sr.EndOfStream)
                    {
                        onSignal(sr.ReadLine());
                    }
                }
            });
            return true;
        }

        #endregion

        #region WindowInfo()

        public static WNDINFO WindowInfo()
        {
            var location = new POINT(0, 0);
            ClientToScreen(Process.MainWindowHandle, location);

            GetWindowRect(Process.MainWindowHandle, out RECT rect);
            var border = location.x - rect.left;
            var title = location.y - rect.top;

            return new WNDINFO
            {
                Location = new Point(location.x, location.y),
                ClientSize = new Size(rect.right - rect.left - border * 2, rect.bottom - rect.top - border - title)
            };
        }

        #endregion
    }
}