using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using PGN.Data;


namespace PGN.General.Connections
{
    internal abstract class Connection
    {
        protected ulong lastMessageNumber;

        public IPEndPoint adress;

        public Connection(IPEndPoint adress)
        {
            this.adress = adress;
        }

        public abstract void SendMessage(NetData message);
        public abstract void SendMessage(byte[] data);

        protected abstract void EndSendCallback(IAsyncResult ar);
    }
}
