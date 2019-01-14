using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using PGN.Data;

namespace PGN.General.Connections
{
    internal class TcpConnection : Connection
    {
        protected internal NetworkStream stream { get; private set; }

        public TcpClient tcpClient;

        public NetUser netUser;

        public TcpConnection(TcpClient tcpClient, IPEndPoint adress) : base(adress)
        {
            this.tcpClient = tcpClient;
            stream = tcpClient.GetStream();
        }

        public void Recieve()
        {
            try
            {
                while (true)
                    GetMessage();
            }
            catch (Exception e)
            {
                if (netUser != null)
                {
                    NetUser.OnUserDisconnectedTCP(netUser.user);
                    NetUser.OnUserDisconnectedUDP(netUser.user);
                    ServerHandler.RemoveConnection(netUser);
                }
                Close();
            }
        }

        public void GetMessage()
        {
            do
            {
                stream.Flush();
                byte[] bytes = new byte[2048];
                stream.Read(bytes, 0, bytes.Length);
                NetData message = NetData.RecoverBytes(bytes);

                if (message.number > lastMessageNumber)
                {

                    if (ServerHandler.clients.Contains(message.sender.ID))
                    {
                        if ((ServerHandler.clients[message.sender.ID] as NetUser).tcpConnection == null)
                        {
                            netUser = ServerHandler.clients[message.sender.ID] as NetUser;
                            netUser.tcpConnection = this;
                            NetUser.OnUserConnectedTCP(message.sender);
                        }
                    }
                    else
                    {
                        netUser = new NetUser(message.sender);
                        netUser.tcpConnection = this;
                        ServerHandler.AddConnection(netUser);
                        NetUser.OnUserConnectedTCP(message.sender);
                    }

                    if (netUser.info == null)
                        netUser.info = NetUser.server.GetUserData(message.sender.ID);
                    if (netUser.info == null)
                        netUser.info = ServerHandler.createUserBD(message.sender);

                    NetUser.OnTcpMessageHandleCallback(bytes, message, netUser);
                    lastMessageNumber = message.number;
                }
            }
            while (stream.DataAvailable);
        }
        

        public override void SendMessage(NetData message)
        {
            byte[] data = NetData.GetBytesData(message);
            SendMessage(data);
        }

        public override void SendMessage(byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        protected override void EndSendCallback(IAsyncResult ar)
        {
            try
            {
                stream.EndWrite(ar);
            }
            catch
            {

            }
        }

        protected internal void Close()
        {
            if (stream != null)
                stream.Close();
            if (tcpClient != null)
                tcpClient.Close();
        }
    }
}
