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

namespace osu_Lyrics.Interop
{
    internal static class Osu
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
                if (Registry.GetValue(@"HKEY_CLASSES_ROOT\osu!\shell\open\command", null, null) is string exec)
                {
                    _process = Process.Start(exec.Split()[0].Trim('"'));
                    return Process;
                }

                MessageBox.Show("osu!가 설치되어있나요?");
                Application.Exit();
                return null;
            }
        }

        #endregion

        #region Show()

        public static bool IsForeground => GetForegroundWindow() == Process.MainWindowHandle;

        public static void Show()
        {
            ShowWindow(Process.MainWindowHandle, SW_SHOWNORMAL);
            SetForegroundWindow(Process.MainWindowHandle);
        }

        #endregion

        #region HookKeyboard(Action<Keys> action), UnhookKeyboard()

        public static event EventHandler<KeyEventArgs> KeyDown;

        private static IntPtr _hhkk = IntPtr.Zero; // hookHandleKeyKeyboard
        private static HookProc _hpk; // hookProcKeyboard

        public static void HookKeyboard()
        {
            if (_hhkk != IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            _hpk = new HookProc(LowLevelKeyboardProc);
            _hhkk = SetWindowsHookEx(WH_KEYBOARD_LL, _hpk, IntPtr.Zero, 0);
        }

        private static IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (IsForeground && nCode == HC_ACTION)
            {
                var state = wParam.ToInt32();
                if (state == WM_KEYDOWN || state == WM_SYSKEYDOWN)
                {
                    //var keyData = (Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT)) as KBDLLHOOKSTRUCT)?.vkCode;
                    var e = new KeyEventArgs((Keys) Marshal.ReadInt32(lParam));
                    KeyDown?.Invoke(null, e);
                    if (e.SuppressKeyPress)
                    {
                        return (IntPtr) 1;
                    }
                }
            }
            return CallNextHookEx(_hhkk, nCode, wParam, lParam);
        }

        public static void UnhookKeyboard()
        {
            if (_hhkk != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hhkk);
                _hpk = null;
            }
        }

        #endregion

        #region Listen(Action<string[]> onSignal)

        public static event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public static void RunMessageServer()
        {
            // dll의 fileVersion을 바탕으로 버전별로 겹치지 않는 경로에 압축 풀기:
            // 시스템 커널에 이전 버전의 dll이 같은 이름으로 남아있을 수 있음
            IO.FileEx.Extract(Assembly.GetExecutingAssembly().GetManifestResourceStream("osu_Lyrics.Server.dll"), Constants._Server);
            var dest = Constants._Server + "." + FileVersionInfo.GetVersionInfo(Constants._Server).FileVersion;
            IO.FileEx.Move(Constants._Server, dest);

            InjectDll(dest);
        }

        private static bool InjectDll(string dllPath)
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

        private static void ListenMessage()
        {
            // 백그라운드에서 서버로부터 데이터를 받아 전달
            using (var pipe = new NamedPipeClientStream(".", "osu!Lyrics", PipeDirection.In, PipeOptions.None))
            using (var sr = new StreamReader(pipe, Encoding.Unicode))
            {
                pipe.Connect();
                while (pipe.IsConnected && !sr.EndOfStream)
                {
                    var e = new MessageReceivedEventArgs(sr.ReadLine());
                    MessageReceived?.BeginInvoke(null, e, null, null);
                }
            }
        }

        public static Task ListenMessageAsync() => Task.Run(() => ListenMessage());

        #endregion

        #region WindowInfo()

        public static Point ClientLocation
        {
            get
            {
                var location = Point.Empty;
                ClientToScreen(Process.MainWindowHandle, ref location);
                return location;
            }
        }

        public static Size ClientSize
        {
            get
            {
                GetClientRect(Process.MainWindowHandle, out Rectangle rect);
                return rect.Size;
            }
        }

        public static Rectangle ClientBounds => new Rectangle(ClientLocation, ClientSize);

        #endregion
    }
}
