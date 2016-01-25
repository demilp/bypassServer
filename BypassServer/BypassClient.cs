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
        public bool needSender;
        public string senderSeparator = "";

        public BypassClient(TcpClient client)
            : base(client, Program.delimitador)
        {

        }
        public JSONClass ToJsonObject()
        {
            JSONClass json = new JSONClass();
            json["id"] = identifier;
            json["tag"] = ConcatTags();
            json["number"] = id.ToString();
            json["ip"] = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            return json;
            /*if (tags != null)
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
            }*/
            
        }
        public string ConcatTags()
        {
            string s = "";
            if (tags != null)
            {
                for (int i = 0; i < tags.Length; i++)
                {
                    s += tags[i];
                    if (i < tags.Length - 1)
                    {
                        s += "|";
                    }
                }
            }
            return s;
        }

    }
}
