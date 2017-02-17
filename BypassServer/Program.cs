using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
namespace BypassServer
{
    public class Program
    {

        public static string delimitador;
        static void Main(string[] args)
        {
            DisableConsoleQuickEdit();
            delimitador = ConfigurationManager.AppSettings["delimitador"];
            BypassServer server;
            try
            {
                 server = new BypassServer(int.Parse(ConfigurationManager.AppSettings["port"]), 0, delimitador);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Server initialized");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            string s;
            do
            {
                s = Console.ReadLine();
                if(s.ToLower().IndexOf("debug") == 0)
                {
                    s = s.ToLower().Trim();
                    string p = s.Substring(s.IndexOf(" ")+1);
                    if(p == "on")
                    {
                        server.ActivateDebugMode(true);
                    }
                    else if(p == "off")
                    {
                        server.ActivateDebugMode(false);
                    }
                }
                /*else if (!server.DataArrived(s))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid command");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }*/

            } while (s != "exit");
            server.Dispose();
        }

        const uint ENABLE_QUICK_EDIT = 0x0040;

        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        internal static bool DisableConsoleQuickEdit()
        {

            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode))
            {
                // ERROR: Unable to get console mode.
                return false;
            }

            // Clear the quick edit bit in the mode flags
            consoleMode &= ~ENABLE_QUICK_EDIT;

            // set the new mode
            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                // ERROR: Unable to set console mode
                return false;
            }

            return true;
        }
        
    }
}
