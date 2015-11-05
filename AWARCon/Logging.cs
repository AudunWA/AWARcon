using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon
{
    internal static class Logging
    {
        internal static void WriteFancyLine(string text, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(DateTime.Now.ToShortTimeString());
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        internal static void WriteServerLine(string text, string server, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(DateTime.Now.ToShortTimeString());
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(server);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        internal static void WriteBlank()
        {
            Console.WriteLine("");
        }
        internal static void LogError(string message)
        {
            File.AppendAllText("errors.log", message + Environment.NewLine);
        }
    }
}
