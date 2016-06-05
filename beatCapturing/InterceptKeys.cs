using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;

namespace AudioMechanica
{
    class InterceptKeys
    {
        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x0100;
        const int LLKHF_EXTENDED = 0x100 >> 8;
        static LowLevelKeyboardProc _proc = HookCallback;
        static IntPtr _hookID = IntPtr.Zero;
        static Thread _messagePump = null;

        static InterceptKeys _instance = new InterceptKeys();
        public static InterceptKeys Instance
        {
            get
            {
                return _instance;
            }
        }

        public event EventHandler<KeyOfInterest> OnKey;

        public enum KeyOfInterest
        {
            BeatKey,
            TransitionKey,
            EndKey
        }

        static Dictionary<int, KeyOfInterest> _keyMap = new Dictionary<int, KeyOfInterest>()
        {
            { (int)Keys.Decimal, KeyOfInterest.TransitionKey },
            { (int)Keys.NumPad0, KeyOfInterest.BeatKey },
        };

        public static void Start()
        {
            _messagePump = new Thread(RunPump);
            _messagePump.Start();
        }

        static void RunPump()
        {
            _hookID = SetHook(_proc);
            Application.Run();
        }

        public static void Stop()
        {
            Application.Exit();
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
                int vkCode = Marshal.ReadInt32(lParam);
                int flags = Marshal.ReadInt32(lParam + 8);

                if ((flags & LLKHF_EXTENDED) > 0 && vkCode == (int)Keys.Enter)
                {
                    Instance.OnKey(Instance, KeyOfInterest.EndKey);
                    return new IntPtr(1);
                }
                else if (_keyMap.ContainsKey(vkCode)) {
                    Instance.OnKey(Instance, _keyMap[vkCode]);
                    return new IntPtr(1);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
