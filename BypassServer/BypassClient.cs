using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpGenericServerNET;

namespace BypassServer
{
    public class BypassClient : TcpConnection
    {
        public string identifier = "";
        public string[] tags;

        public BypassClient(TcpClient client)
            : base(client, Program.delimitador)
        {

        }
        public string ToJsonObject()
        {
            if (tags != null)
            {
                string s = "{";
                s += "\"id\":\"" + identifier + "\",";
                s += "\"tag\":\"" + ConcatTags() + "\",";
                s += "\"number\":\"" + id + "\",";
                s += "\"ip\":\"" + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + "\"";
                s += "}";
                return s;
            }
            else
            {
                string s = "{";
                s += "\"id\":\"" + identifier + "\",";
                s += "\"tag\":\"\",";
                s += "\"number\":\"" + id + "\",";
                s += "\"ip\":\"" + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + "\"";
                s += "}";
                return s;
            }
            
        }
        public string ConcatTags()
        {
            string s = "";
            for (int i = 0; i < tags.Length; i++)
            {
                s += tags[i];
                if (i < tags.Length-1)
                {
                    s += "|";
                }
            }
            return s;
        }

    }
}
