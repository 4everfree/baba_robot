using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace ZelvParser
{
    class Tell
    {
        public static void ChangePart(string message, ConsoleColor c)
        {
            ChangeTitle(message);
            ColoredMessage($"{message}\n", c);
        }

        public static void ChangeTitle(string message)
        {
            Console.Title = message;
        }

        public static void ColoredMessage(string message, ConsoleColor c)
        {
            ForegroundColor = c;
            WriteLine(message);
        }

        public static void ColoredWrite(string message, ConsoleColor c)
        {
            ForegroundColor = c;
            Write(message);
        }
    }
}
