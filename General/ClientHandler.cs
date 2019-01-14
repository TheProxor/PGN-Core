using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using PGN.Data;

namespace PGN.General
{
    public class ClientHandler : Handler
    {
        private TcpClient tcpClient;
        private UdpClient udpClient;

        public NetworkStream stream;

        public event Action<NetData> onMessageRecieveTCP;
        public event Action<NetData> onMessageRecieveUDP;

        /// <summary>
        ///Use it if your environment cannot support invoke from threads; 
        /// Используйте это, если ваша среда не поддерживает вызов из потоков
        /// </summary>
        public event Action<NetData> OnMessageRecievedByCheckout;

        /// <summary>
        /// Invoke if you sucsessed connected to server
        /// Вызывается, если вы успешно подключились в серверу
        /// </summary>
        public event Action onConnect;

        /// <summary>
        /// Invoke if you sucsessed disconnected from server
        /// Вызывается, если вы успешно отключились от сервера
        /// </summary>
        public event Action onDisconnect;

        /// <summary>
        /// Invoke if your Internetwork is not avaible
        /// Вызывается, если нет соеденения с сетью
        /// </summary>
        public event Action onNetworkNotAvaible;
        public event Action onServerNotAvaible;
        public event Action onConnectionLost;

        private static ulong numberTCP = 1;
        private static ulong numberUDP = 1;

        private int connectTry = 0;

        private Queue<NetData> tcpMessages = new Queue<NetData>();
        private Queue<NetData> udpMessages = new Queue<NetData>();

        public void Connect()
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {

                try
                {
                    tcpClient = new TcpClient(new IPEndPoint(localAddressTCP.Address, localAddressTCP.Port + connectTry));
                    udpClient = new UdpClient(new IPEndPoint(localAddressUDP.Address, localAddressUDP.Port + connectTry));
                    tcpClient.BeginConnect(remoteAddressTCP.Address, remoteAddressTCP.Port, ConnectCallbackTCP, tcpClient);
                    if(OnMessageRecievedByCheckout != null)
                    {
                        onMessageRecieveTCP += (NetData data) => {
                            lock (tcpMessages)
                            {
                                tcpMessages.Enqueue(data);
                            }
                        };

                        onMessageRecieveUDP += (NetData data) => {
                            lock (udpMessages)
                            {
                                udpMessages.Enqueue(data);
                            }
                        };
                    }
                }
                catch 
                {
                    connectTry++;
                    Connect();
                }
            }
            else
            {
                OnLogReceived("Network is not avaible!");
                onNetworkNotAvaible?.Invoke();
            }
        }

        private void ConnectCallbackTCP(IAsyncResult ar)
        {
            try
            {
                tcpClient.EndConnect(ar);
                ConnectContinue();
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10061)
                {
                    onServerNotAvaible?.Invoke();
                    OnLogReceived("Server is not avaible!");
                }
                else
                {
                    connectTry++;
                    tcpClient = new TcpClient(new IPEndPoint(localAddressTCP.Address, localAddressTCP.Port + connectTry));
                    udpClient = new UdpClient(new IPEndPoint(localAddressUDP.Address, localAddressUDP.Port + connectTry));
                    tcpClient.BeginConnect(remoteAddressTCP.Address, remoteAddressTCP.Port, ConnectCallbackTCP, tcpClient);
                }
            }
        }

        private void ConnectContinue()
        {
            udpClient.Connect(remoteAddressUDP);

            Thread.Sleep(100);

            stream = tcpClient.GetStream();

            NetData message = new NetData(0, user);
            message.SetNumber(numberTCP);
            byte[] data = NetData.GetBytesData(message);

            numberTCP++;
            numberUDP++;
            stream.Write(data, 0, data.Length);
            Thread.Sleep(100);
            udpClient.Send(data, data.Length);

            onConnect?.Invoke();

            ReceiveMessageTCP();
            ReceiveMessageUDP();
        }

        public void ReceiveMessageTCP()
        {
            byte[] bytes = new byte[2048];
            try
            {
                stream.BeginRead(bytes, 0, bytes.Length, new AsyncCallback(ReceiveMessageTCPCallback), bytes);
            }
            catch (Exception e)
            {
                if (stream != null)
                {
                    stream.Flush();
                    stream.Dispose();
                    stream = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Client.Close();
                    tcpClient.Dispose();
                    tcpClient = null;
                }

                if (udpClient != null)
                {
                    udpClient.Dispose();
                    udpClient = null;
                }

                onConnectionLost?.Invoke();
            }
        }

        public void ReceiveMessageTCPCallback(IAsyncResult ar)
        {
            ReceiveMessageTCP();
            try
            {
                int bytesCount = stream.EndRead(ar);
                if (bytesCount > 0)
                {
                    NetData message = NetData.RecoverBytes(ar.AsyncState as byte[]);
                    onMessageRecieveTCP?.Invoke(message);
                }
            }
            catch (Exception e)
            {
                if (stream != null)
                {
                    stream.Flush();
                    stream.Dispose();
                    stream = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Client.Close();
                    tcpClient.Dispose();
                    tcpClient = null;
                }

                if (udpClient != null)
                {
                    udpClient.Dispose();
                    udpClient = null;
                }

                onConnectionLost?.Invoke();
            }
        }

        public void ReceiveMessageUDP()
        {
            try
            {
                udpClient.BeginReceive(new AsyncCallback(ReceiveMessageUDPCallback), udpClient);
            }
            catch (Exception e)
            {
                if (stream != null)
                {
                    stream.Flush();
                    stream.Dispose();
                    stream = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Client.Close();
                    tcpClient.Dispose();
                    tcpClient = null;
                }

                if (udpClient != null)
                {
                    udpClient.Dispose();
                    udpClient = null;
                }

                onConnectionLost?.Invoke();
            }
        }

        public void ReceiveMessageUDPCallback(IAsyncResult ar)
        {
            ReceiveMessageUDP();
            try
            {
                byte[] data = udpClient.EndReceive(ar, ref remoteAddressUDP);
                onMessageRecieveUDP(NetData.RecoverBytes(data));
            }
            catch
            {
                if (stream != null)
                {
                    stream.Flush();
                    stream.Dispose();
                    stream = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Client.Close();
                    tcpClient.Dispose();
                    tcpClient = null;
                }

                if (udpClient != null)
                {
                    udpClient.Dispose();
                    udpClient = null;
                }

                onConnectionLost?.Invoke();
            }
        }

        public void SendMessageTCP(NetData message)
        {
            message.SetNumber(numberTCP);
            byte[] data = NetData.GetBytesData(message);
            stream.Write(data, 0, data.Length);
            stream.Flush();
            numberTCP++;
        }

        public void SendMessageUDP(NetData message)
        {
            message.SetNumber(numberUDP);
            byte[] data = NetData.GetBytesData(message);
            udpClient.Send(data, data.Length);
            numberUDP++;
        }

        public void Disconnect()
        {
            if (stream != null)
            {
                stream.Flush();
                stream.Dispose();
                stream = null;
            }

            if (tcpClient != null)
            {
                tcpClient.Client.Close();
                tcpClient.Dispose();
                tcpClient = null;
            }

            if (udpClient != null)
            {
                udpClient.Dispose();
                udpClient = null;
            }

            onDisconnect?.Invoke();
        }

       
        /// <summary>
        /// Use it if your environment cannot support invoke from threads; 
        /// Используйте это, если ваша среда не поддерживает вызов из потоков
        /// </summary>
        /// 
        public void CheckoutData()
        {
            lock (tcpMessages)
            {
                while (tcpMessages.Count != 0) onMessageRecieveTCP?.Invoke(tcpMessages.Dequeue());
            }

            lock (udpMessages)
            {
                while (udpMessages.Count != 0) onMessageRecieveUDP?.Invoke(udpMessages.Dequeue());
            }

        }
    }
}

