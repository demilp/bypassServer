using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BypassServer
{
    public class Program
    {
        public static string delimitador;
        static void Main(string[] args)
        {
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
                else if (!server.DataArrived(s))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid command");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

            } while (s != "exit");
            server.Dispose();
        }
    }
}
