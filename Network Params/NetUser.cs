using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using PGN.Data;
using PGN.General.Connections;

namespace PGN.General
{
    public class NetUser
    {

        public User user { get; private set; }

        public static ServerHandler server;

        public static event Action<byte[], NetData, NetUser> onTcpMessageHandleCallback;
        public static event Action<byte[], NetData, NetUser> onUdpMessageHandleCallback;

        public static event Action<User> onUserConnectedTCP;
        public static event Action<User> onUserDisconnectedTCP;

        public static event Action<User> onUserConnectedUDP;
        public static event Action<User> onUserDisconnectedUDP;

        public object info = null;


        internal TcpConnection tcpConnection;
        internal UdpConnection udpConnection;

        public NetUser(User user)
        {
            this.user = user;
        }

        public static void OnUserConnectedTCP(User user)
        {
            onUserConnectedTCP(user);
        }

        public static void OnUserConnectedUDP(User user)
        {
            onUserConnectedUDP(user);
        }

        public static void OnUserDisconnectedTCP(User user)
        {
            onUserDisconnectedTCP(user);
        }

        public static void OnUserDisconnectedUDP(User user)
        {
            onUserDisconnectedUDP(user);
        }

        public static void OnTcpMessageHandleCallback(byte[] dataBytes, NetData data, NetUser netUser)
        {
            onTcpMessageHandleCallback(dataBytes, data, netUser);
        }

        public static void OnUdpMessageHandleCallback(byte[] dataBytes, NetData data, NetUser netUser)
        {
            onUdpMessageHandleCallback(dataBytes, data, netUser);
        }
    }
}

