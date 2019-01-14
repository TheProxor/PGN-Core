using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.Compression;
using PGN.General;

namespace PGN.Data
{
    [Serializable]
    public class NetData
    {
        public NetData(object data, User sender)
        {
            this.sender = sender;
            this.data = data;
            this.time = DateTime.Now;
        }

        public User sender;
        public DateTime time;

        public object data;

        public ulong number { get; private set; }

        public void SetNumber(ulong number)
        {
            this.number = number;
        }

        public static byte[] GetBytesData(NetData message)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    (new BinaryFormatter()).Serialize(memoryStream, message);
                    return memoryStream.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        public static NetData RecoverBytes(byte[] bytesData)
        {
            try
            {
                using (var memoryStream = new MemoryStream(bytesData))
                    return (new BinaryFormatter()).Deserialize(memoryStream) as NetData;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }
    }
}
