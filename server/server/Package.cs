using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    internal class Package
    {
        //SERVER
        public string encryption;
        public string type;

        public Dictionary<string, string>? body;

        public Package(string encryption, string type)
        {
            this.encryption = encryption;
            this.type = type;
        }
    }
}
