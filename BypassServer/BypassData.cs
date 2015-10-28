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

        public BypassData(string types, string data, string tag, JSONArray ids) : this()
        {
            Console.WriteLine(ids);
            this.type = types;
            this.data = data;
            this.tag = tag;
            this.ids = new string[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                this.ids[i] = ids[i].Value;
            }
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
