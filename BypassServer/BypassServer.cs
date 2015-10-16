using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpGenericServerNET;

namespace BypassServer
{
    public class BypassServer : TcpServer
    {
        public List<BypassClient> clients;
        private bool debugMode;
        private string[] messages;
        private int messagesIndex;
        
        public BypassServer(int port, int maxConn = 0, string delimiter = null) : base(port, maxConn, delimiter)
        {
            clients = new List<BypassClient>();
            debugMode = ConfigurationManager.AppSettings["debug"].ToLower() == "true";
            if (debugMode)
            {
                messages = new string[int.Parse(ConfigurationManager.AppSettings["logCount"])];
                messagesIndex = 0;
            }
        }
        public void ActivateDebugMode(bool s)
        {
            debugMode = s;
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (debugMode)
            {
                messages = new string[int.Parse(ConfigurationManager.AppSettings["logCount"])];
                messagesIndex = 0;
                Console.WriteLine("Debug mode activated");
            }
            else
            {
                messages = null;
                Console.WriteLine("Debug mode deactivated");
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        public override void ClientConnected(TcpConnection connection)
        {
            base.ClientConnected(connection);
            clients.Add((BypassClient)connection);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Client connected");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            AddMessage("Server", "Client connected");
        }
        public override void ClientDisconnected(TcpConnection connection)
        {
            base.ClientDisconnected(connection);
            clients.Remove((BypassClient)connection);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Client "+ ((BypassClient)connection).identifier +" disconnected");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            AddMessage("Server", "Client \"" + ((BypassClient)connection).identifier + "\" disconnected");
        }
        public bool DataArrived(string data)
        {
            try
            {
                BypassData receivedData = JsonConvert.DeserializeObject<BypassData>(data);

                if (receivedData.type == "send")
                {
                    BypassClient[] receivers = Filter(receivedData.ids, receivedData.tag);
                    for (int i = 0; i < receivers.Length; i++)
                    {
                        receivers[i].WriteLine(receivedData.data + Program.delimitador);
                    }
                    return true;
                }
                if (receivedData.type == "broadcast")
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        clients[i].WriteLine(receivedData.data);
                    }
                    return true;
                }
                return false;
            }
            catch(Exception e)
            {
                return false;
            }
        }
        public void AddMessage(string senderId, string message)
        {
            if(debugMode)
            {
                message = message.Replace("\"", "\\\"");
                messages[messagesIndex] = senderId + " = " + message;
                messagesIndex++;
                messagesIndex %= messages.Length;
            }
        }
        public override void DataArrived(TcpConnection connection, string data)
        {
            base.DataArrived(connection, data);
            try
            {                
                BypassData receivedData = JsonConvert.DeserializeObject<BypassData>(data);

                BypassClient client = (BypassClient) connection;
                if (receivedData.type == "register")
                {
                    client.identifier = receivedData.data;
                    client.tags = receivedData.tag.Split('|');
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (client.ConcatTags() != "")
                    {
                        
                        Console.WriteLine("Id " + client.identifier + " registered with tags " + client.ConcatTags());
                        
                    }
                    else
                    {
                        Console.WriteLine("Id " + client.identifier);
                    }
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                else if (receivedData.type == "send")
                {
                    BypassClient[] receivers = Filter(receivedData.ids, receivedData.tag);
                    for (int i = 0; i < receivers.Length; i++)
                    {
                        receivers[i].WriteLine(receivedData.data + Program.delimitador);
                    }
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    if (receivedData.ids == null || receivedData.ids.Length == 0)
                    {
                        Console.WriteLine(((BypassClient) connection).identifier + " sent \"" + receivedData.data + "\" to clients with tags " + receivedData.tag);
                    }
                    else
                    {
                        Console.WriteLine(((BypassClient)connection).identifier + " sent \"" + receivedData.data + "\" to " + ContatArray(receivedData.ids));
                    }
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else if (receivedData.type == "broadcast")
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        if(clients[i] != connection)
                        {
                            clients[i].WriteLine(receivedData.data);
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(((BypassClient) connection).identifier + " broadcasted \""+ receivedData.data +"\"");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else if (receivedData.type == "broadcastAll")
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        clients[i].WriteLine(receivedData.data);
                    }
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(((BypassClient)connection).identifier + " broadcasted \"" + receivedData.data + "\"");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else if (receivedData.type == "status")
                {
                    TcpConnection[] c = ConnectedConnections();
                    for (int i = clients.Count-1; i >= 0; i--)
                    {
                        bool exists = false;
                        for (int j = 0; j < c.Length; j++)
                        {
                            if (c[j] == clients[i])
                            {
                                exists = true;
                            }
                        }
                        if (!exists)
                        {
                            ClientDisconnected(clients[i]);
                        }
                    }
                    connection.WriteLine(GetStatus());
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("Status update requested by " + ((BypassClient)connection).identifier);
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                AddMessage(client.identifier, data);
            }
            catch (JsonSerializationException e)
            {

            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            }

        }
        protected override TcpConnection connectionFactory(System.Net.Sockets.TcpClient client)
        {
            return new BypassClient(client);
        }

        private BypassClient[] Filter(string[] ids, string tag = "")
        {
            if(ids == null || ids.Length == 0)
            {
                return Filter(tag);
            }
            else
            {
                return Filter(ids);
            }
        }

        private BypassClient[] Filter(string[] ids)
        {
            List<BypassClient> c = new List<BypassClient>();
            for (int i = 0; i < clients.Count; i++)
            {
                for (int j = 0; j < ids.Length; j++)
                {
                    if (clients[i].identifier == ids[j])
                    {
                        c.Add(clients[i]);
                    }
                }
            }
            return c.ToArray();
        }

        private BypassClient[] Filter(string tag)
        {
            List<BypassClient> c = new List<BypassClient>();
            for (int i = 0; i < clients.Count; i++)
            {
                for (int j = 0; j < clients[i].tags.Length; j++)
                {
                    if (clients[i].tags[j] == tag)
                    {
                        c.Add(clients[i]);
                    }
                }
            }
            return c.ToArray();
        }
        private string ContatArray(string[] array)
        {
            string s = "";
            for (int i = 0; i < array.Length; i++)
            {
                s += array[i]+ ", ";
            }
            return s;
        }

        private string GetStatus()
        {
            string s = "{\"status\":[";

            for (int i = 0; i < clients.Count; i++)
            {
                s += clients[i].ToJsonObject();
                if(i != clients.Count -1)
                {
                    s += ",";
                }
            }
            s += "], \"lastMessages\":[";
            if (debugMode)
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    s += "\"" + messages[(messagesIndex+i) % messages.Length] + "\"";
                    if (i != messages.Length)
                    {
                        s += ",";
                    }
                }
            }
            else
            {
                s += "\"debug mode off\"";
            }
            return s+"]}";
        }
    }

    

}
