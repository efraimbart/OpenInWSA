using System;
using System.Runtime.InteropServices;

namespace OpenInWSA.Classes
{
    public static class Console
    {
        [DllImport("kernel32")]
        static extern bool AllocConsole();
        
        private static bool ConsoleAllocated { get; set; }

        private static void AllocateConsole()
        {
            if (ConsoleAllocated) return;
            
            AllocConsole();
            ConsoleAllocated = true;
        }
        
        //TODO: Add remaining Console methods
        public static void WriteLine(string value = null)
        {
            AllocateConsole();
            System.Console.WriteLine(value);
        }

        public static ConsoleKeyInfo ReadKey()
        {
            AllocateConsole();
            return System.Console.ReadKey();
        }

        public static string ReadLine()
        {
            AllocateConsole();
            return System.Console.ReadLine();
        }
    }
}