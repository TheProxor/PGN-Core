using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace PGN.General
{
    [Serializable]
    public class User
    {     
        public string ID = string.Empty;
        public string name = string.Empty;

        public User()
        {
            name = Dns.GetHostName();
            ID = name; 
        }

        public User(string id)
        {
            this.name = Dns.GetHostName();
            this.ID = id;
        }

        public override string ToString()
        {
            return "\nID: " + ID; 
        }
    }
}
