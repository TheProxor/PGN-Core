using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using PGN.Data;

namespace PGN.General
{
    public class Handler
    {
        public static event Action<string> onLogReceived;

        public static readonly float version = 1.0f;

        protected static IPEndPoint localAddressTCP;
        protected static IPEndPoint localAddressUDP;

        protected static IPEndPoint remoteAddressTCP;
        protected static IPEndPoint remoteAddressUDP;

        protected Socket TCP_socket;

        protected static User user;

        public void SetLocalAdressTCP(string ip, int port)
        {
            localAddressTCP = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void SetLocalAdressUDP(string ip, int port)
        {
            localAddressUDP = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void SetRemoteAdressTCP(string ip, int port)
        {
            remoteAddressTCP = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void SetRemoteAdressUDP(string ip, int port)
        {
            remoteAddressUDP = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void RegistrUser(User _user)
        {
            user = _user;
            OnLogReceived("You registr new user - " + _user.ID);
        }

        public static void OnLogReceived(string text)
        {
            onLogReceived?.Invoke("PGN LOG: " + text);
        }

        public static string GetLocalIpAddress()
        {
            System.Net.NetworkInformation.UnicastIPAddressInformation mostSuitableIp = null;

            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                if (network.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;

                var properties = network.GetIPProperties();

                if (properties.GatewayAddresses.Count == 0)
                    continue;

                foreach (var address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    if (!address.IsDnsEligible)
                    {
                        if (mostSuitableIp == null)
                            mostSuitableIp = address;
                        continue;
                    }

                    // The best IP is the IP got from DHCP server
                    if (address.PrefixOrigin != System.Net.NetworkInformation.PrefixOrigin.Dhcp)
                    {
                        if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            mostSuitableIp = address;
                        continue;
                    }

                    return address.Address.ToString();
                }
            }

            return mostSuitableIp != null
                ? mostSuitableIp.Address.ToString()
                : "";
        }
    }
}
