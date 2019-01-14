using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

using PGN.Data;
using PGN.General.Connections;

namespace PGN.General
{
    public class ServerHandler : Handler
    {
        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static Hashtable clients { get; private set; } = new Hashtable();

        //DATABASE FUNCTIONS
        public static Func<User, object> createUserBD;
        public static Func<string, object> getInfoFromBD;

        protected internal static void AddConnection(NetUser client)
        {
            clients.Add(client.user.ID, client);
        }

        protected internal static void RemoveConnection(NetUser client)
        {
            if (clients.Contains(client.user.ID))
                clients.Remove(client.user.ID);
        }

        protected internal static void RemoveConnection(string id)
        {
            if (clients.Contains(id))
                clients.Remove(id);
        }

        public void Start()
        {
            tcpListener = new TcpListener(localAddressTCP);
            udpListener = new UdpClient(localAddressUDP);
            udpListener.EnableBroadcast = true;
            udpListener.Client.ReceiveBufferSize = 100000;

            tcpListener.Start();
            OnLogReceived("Server was created.");

            NetUser.server = this;

            ListenTCP();
            ListenUDP();
        }

        private void ListenTCP()
        {
            try
            {
                tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), tcpListener);
            }

            catch (Exception ex)
            {
                OnLogReceived(ex.ToString());
            }
        }

        private void ListenUDP()
        {
            try
            {
                udpListener.BeginReceive(ReceiveCallback, udpListener);
            }
            catch (Exception ex)
            {
               OnLogReceived(ex.ToString());
            }
        }

        protected internal void AcceptCallback(IAsyncResult ar)
        {
            ListenTCP();
            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
            TcpConnection tcpConnection = new TcpConnection(tcpClient, tcpClient.Client.RemoteEndPoint as IPEndPoint);
            tcpConnection.Recieve();
        }

        protected internal void ReceiveCallback(IAsyncResult ar)
        {
            byte[] bytes = new byte[2048];

            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 8001);

            UdpClient udpClient = (ar.AsyncState as UdpClient);

            try
            {
                bytes = udpClient.EndReceive(ar, ref iPEndPoint);

                UdpConnection udpConnection = new UdpConnection(udpClient, iPEndPoint);
                udpConnection.Recieve(bytes);
            }
            catch (Exception e)
            {
                OnLogReceived(e.Message);
            }
            finally
            {
                ListenUDP();
            }
        }

        public void BroadcastMessageTCP(byte[] data, User sender)
        {
            if (data != null)
            {
                foreach (string key in clients.Keys)
                {
                    if ((clients[key] as NetUser).user.ID != sender.ID && (clients[key] as NetUser).tcpConnection != null)
                        (clients[key] as NetUser).tcpConnection.SendMessage(data);
                }
            }
        }

        public void BroadcastMessageUDP(byte[] data, User sender)
        {
            if (data != null)
            {
                foreach (string key in clients.Keys)
                    if ((clients[key] as NetUser).user.ID != sender.ID && (clients[key] as NetUser).udpConnection != null)
                        (clients[key] as NetUser).udpConnection.SendMessage(data);
            }
        }


        public void SendMessageViaTCP(NetData message, User user)
        {
            (clients[user.ID] as NetUser).tcpConnection.SendMessage(message);
        }

        public void SendMessageViaUDP(NetData message, User user)
        {
            (clients[user.ID] as NetUser).udpConnection.SendMessage(message);
        }

        public void SendMessageViaTCP(byte[] bytes, User user)
        {
            (clients[user.ID] as NetUser).tcpConnection.SendMessage(bytes);
        }

        public void SendMessageViaUDP(byte[] bytes, User user)
        {
            (clients[user.ID] as NetUser).udpConnection.SendMessage(bytes);
        }

        public object GetUserData(string id)
        {
           return getInfoFromBD(id);
        }

        public void Stop()
        {
            tcpListener.Stop();
            udpListener.Close();

            for (int i = 0; i < clients.Count; i++)
            {
                (clients[i] as NetUser).tcpConnection.Close();
            }
            Environment.Exit(0);
        }
    }
}
