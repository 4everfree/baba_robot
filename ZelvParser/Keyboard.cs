using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZelvParser
{
    class Keyboard
    {

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName,
            string lpWindowName);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;
        const int WM_CHAR = 0x0102;



        public static void PushTheTempo()
        {


            Console.Title = $"{Program.project} FFmpeg Monster";

            IntPtr handle = FindWindow(@"ConsoleWindowClass", $@"{Program.project} FFmpeg Monster");


            if (handle == null) Console.Write("null handle");



            SendMessage(handle, WM_KEYDOWN, (int)32, null);
        }
    }
}
