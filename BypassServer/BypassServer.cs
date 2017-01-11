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
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(BypassServer));
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
            if (debugMode)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Client connected");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            Log("Client connected", "BypassServer", "BypassServer");
            AddMessage("Server", "Client connected");
        }
        public override void ClientDisconnected(TcpConnection connection)
        {
            base.ClientDisconnected(connection);
            clients.Remove((BypassClient)connection);
            if (debugMode)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Client " + ((BypassClient) connection).identifier + " disconnected");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            AddMessage("Server", "Client \"" + ((BypassClient)connection).identifier + "\" disconnected");
            Log("Client disconnected", ((BypassClient)connection).identifier==""? "(non-registered)" : ((BypassClient)connection).identifier, "BypassServer");
        }

        /*
        public bool DataArrived(string data)
        {
            try
            {
                JSONNode node = JSONNode.Parse(data);
                BypassData receivedData = new BypassData(node["type"].Value, node["data"].Value, node["tag"].Value, node["ids"].AsArray);

                if (receivedData.type == "send")
                {
                    BypassClient[] receivers = Filter(receivedData.ids, receivedData.tag);
                    for (int i = 0; i < receivers.Length; i++)
                    {
                        if (receivers[i].needSender)
                        {
                            receivers[i].WriteLine("bypassServer" + clients[i].senderSeparator + receivedData.data);
                        }
                        else
                        {
                            receivers[i].WriteLine(receivedData.data);
                        }
                    }
                    return true;
                }
                if (receivedData.type == "broadcast")
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        if (clients[i].needSender)
                        {
                            clients[i].WriteLine("bypassServer" + clients[i].senderSeparator + receivedData.data);
                        }
                        else
                        {
                            clients[i].WriteLine(receivedData.data);
                        }
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
        */

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
                JSONNode node = JSONNode.Parse(data);
                //sConsole.WriteLine(node["ids"].Count);
                BypassData receivedData = new BypassData(node["type"].Value, node["data"].Value, node["tag"].Value, node["ids"].AsArray);

                BypassClient client = (BypassClient) connection;
                if (receivedData.type == "register")
                {
                    client.identifier = receivedData.data;
                    client.tags = receivedData.tag.Split('|');
                    
                    if (client.ConcatTags() != "")
                    {
                        string conc = client.ConcatTags();
                        if (debugMode)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Id " + client.identifier + " registered with tags " + conc);
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        Log("Registed with tags " + conc, ((BypassClient)connection).identifier, "BypassServer");
                    }
                    else
                    {
                        if (debugMode)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Id " + client.identifier);
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        Log("Registed", ((BypassClient)connection).identifier, "BypassServer");
                    }
                    
                }

                else if (receivedData.type == "send")
                {
                    BypassClient[] receivers = Filter(receivedData.ids, receivedData.tag);
                    for (int i = 0; i < receivers.Length; i++)
                    {
                        if (receivers[i].needSender)
                        {
                            receivers[i].WriteLine(((BypassClient)connection).identifier + receivers[i].senderSeparator + receivedData.data);
                        }
                        else
                        {
                            receivers[i].WriteLine(receivedData.data);
                        }
                    }
                    
                    if (receivedData.ids == null || receivedData.ids.Length == 0)
                    {
                        if (debugMode)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(((BypassClient) connection).identifier + " sent \"" + receivedData.data +
                                              "\" to clients with tags " + receivedData.tag);
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        Log(receivedData.data, ((BypassClient)connection).identifier, "tag "+ receivedData.tag);
                    }
                    else
                    {
                        string ids = ContatArray(receivedData.ids);
                        if (debugMode)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(((BypassClient) connection).identifier + " sent \"" + receivedData.data +
                                              "\" to " + ids);
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        Log(receivedData.data, ((BypassClient)connection).identifier, ids);
                    }
                    
                }
                else if (receivedData.type == "needSender")
                {
                    
                    
                    ((BypassClient)connection).needSender = true;
                    ((BypassClient)connection).senderSeparator = receivedData.data;
                    if (debugMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(((BypassClient) connection).identifier + " needs sender id, with separator " +
                                          receivedData.data);
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    Log("Needs sender id with separator "+ receivedData.data, ((BypassClient)connection).identifier, "BypassServer");
                }
                else if (receivedData.type == "broadcast")
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        if(clients[i] != connection)
                        {
                            if (clients[i].needSender)
                            {
                                clients[i].WriteLine(((BypassClient)connection).identifier + clients[i].senderSeparator + receivedData.data);
                            }
                            else
                            {
                                clients[i].WriteLine(receivedData.data);
                            }
                            
                        }
                    }
                    if (debugMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(((BypassClient) connection).identifier + " broadcasted \"" + receivedData.data +
                                          "\"");
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    Log(receivedData.data, ((BypassClient)connection).identifier, "Broadcast");
                }
                else if (receivedData.type == "broadcastAll")
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        if (clients[i].needSender)
                        {
                            clients[i].WriteLine(((BypassClient)connection).identifier + clients[i].senderSeparator + receivedData.data);
                        }
                        else
                        {
                            clients[i].WriteLine(receivedData.data);
                        }
                    }
                    if (debugMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(((BypassClient) connection).identifier + " broadcasted \"" + receivedData.data +
                                          "\"");
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    Log(receivedData.data, ((BypassClient)connection).identifier, "BroadcastAll");
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
                    if (debugMode)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Status update requested by " + ((BypassClient) connection).identifier);
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    Log("Status update requested", ((BypassClient)connection).identifier, "BypassServer");
                }
                AddMessage(client.identifier, data);
            }
            catch (Exception e)
            {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
                Log(e.Message, "BypassServer", "Error");
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
            JSONClass json = new JSONClass();
            JSONArray status = new JSONArray();
            

            for (int i = 0; i < clients.Count; i++)
            {
                status.Add(clients[i].ToJsonObject());
            }
            json.Add("status", status);
            JSONArray lastMessages = new JSONArray();
            
            if (debugMode)
            {
                
                for (int i = 0; i < messages.Length; i++)
                {
                    if (messages[(messagesIndex + i) % messages.Length] != null)
                    {
                        lastMessages.Add(messages[(messagesIndex + i) % messages.Length]);
                    }
                    
                }
            }
            json.Add("lastMessages", lastMessages);
            return json.ToString();
        }
        void Log(string message, string sender, string receiver)
        {
            //logger.Info(new {Message=message, Sender=sender, Receiver=receiver, DateTime=DateTime.Now});
            log4net.ThreadContext.Properties["receiver"] = receiver;
            log4net.ThreadContext.Properties["sender"] = sender;
            //log4net.ThreadContext.Properties["dateTime"] = DateTime.Now.to;
            logger.Info(message);

        }
    }

 

}
