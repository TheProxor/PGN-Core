using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using PGN.Data;


namespace PGN.General.Connections
{
    internal class UdpConnection : Connection
    {
        public UdpClient udpClient;
        public NetUser netUser;

        public UdpConnection(UdpClient udpClient, IPEndPoint adress) : base(adress)
        {
            this.udpClient = udpClient;
        }

        public void Recieve(byte[] bytes)
        {
            NetData message = NetData.RecoverBytes(bytes);
            if (message != null && message.number > lastMessageNumber)
            {
                if (ServerHandler.clients.Contains(message.sender.ID))
                {
                    if ((ServerHandler.clients[message.sender.ID] as NetUser).udpConnection == null)
                        NetUser.OnUserConnectedUDP(message.sender);
                    netUser = ServerHandler.clients[message.sender.ID] as NetUser;
                    netUser.udpConnection = this;
                }
                else
                {
                    netUser = new NetUser(message.sender);
                    netUser.udpConnection = this;
                    ServerHandler.AddConnection(netUser);
                    NetUser.OnUserConnectedUDP(message.sender);
                }
                NetUser.OnUdpMessageHandleCallback(bytes, message, netUser);
                lastMessageNumber = message.number;
            }
        }

        public override void SendMessage(NetData message)
        {
            byte[] data = NetData.GetBytesData(message);
            udpClient.BeginSend(data, data.Length, adress, EndSendCallback, udpClient);
        }

        public override void SendMessage(byte[] data)
        {
            udpClient.BeginSend(data, data.Length, adress, EndSendCallback, udpClient);
        }

        protected override void EndSendCallback(IAsyncResult ar)
        {
            (ar.AsyncState as UdpClient).EndSend(ar);
        }
    }
}

