using System;
using System.Collections.Generic;
using System.Text;

namespace PGN.Data
{
    [AttributeUsage(AttributeTargets.Method)]
    class RPC : Attribute
    {
        private static uint lastID = 0;

        public int instanceID = -1;
        public string functionName;

        public RPC(string functionName)
        {
            
        }
    }
}
