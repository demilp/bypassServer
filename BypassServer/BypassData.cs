using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BypassServer
{
    public struct BypassData
    {
        public string type;
        public string tag;
        public string[] ids;
        public string data;

        public enum Types { REGISTER, SEND, BROADCAST, BROADCAST_ALL, STATUS }
        public BypassData(Types types, string data, string tag, string[] ids = null)
            : this()
        {
            ids = null;
            switch (types)
            {
                case Types.REGISTER:
                    type = "register";
                    break;
                case Types.SEND:
                    type = "send";
                    break;
                case Types.BROADCAST:
                    type = "broadcast";
                    break;
                case Types.BROADCAST_ALL:
                    type = "broadcastAll";
                    break;
                case Types.STATUS:
                    type = "status";
                    break;
                default:
                    type = "";
                    break;
            }
            this.data = data;
            this.tag = tag;
            this.ids = ids;
        }
        public string ToJson()
        {
            string s = "";
            s = "{\"type\":\"" + type + "\", \"data\":\"" + data + "\", \"tags\":\"" + tag + "\", \"ids\":[" + ConcatIds() + "]}";
            return s;
        }
        private string ConcatIds()
        {
            string s = "";
            if (ids == null || ids.Length == 0)
            {
                return s;
            }
            for (int i = 0; i < ids.Length - 1; i++)
            {
                s += ids[i] + ", ";
            }
            s += ids[ids.Length - 1];
            return s;
        }

    }
}
