using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MCPinger
{

    public class McServerPingHelper
    {
        private String _host = null;
        private int port = 25565;//default port

        public McServerPingHelper(String host, int port)
        {
            _host = host;
            this.port = port;
        }
        public McServerPingHelper(String host)
        {
            _host = host;
        }


        /// <summary>
        /// <para>Try to ping server with specify ProtocolMinecraftVersion</para>
        /// <para>About ProtocolVersion:<see href="https://wiki.vg/Protocol_version_numbers"></see></para>
        /// </summary>
        /// <param name="ProtocolVersion"></param>
        /// <returns cref="PingResult">Packaged Ping Result(Json String,but the key unchecked)</returns>
        /// <exception cref="Exception"></exception>
        public PingResult Ping(int ProtocolVersion)
        {
            if (_host == null)
            {
                throw new Exception("host can't not be null");
            }
            try
            {
                TcpClient tcpClient = new TcpClient(_host, port);
                MemoryStream handShakeStream = new MemoryStream();
                NetworkStream Stream = tcpClient.GetStream();
                //building handshakePackage
                handShakeStream.WriteByte(0x00);
                //writing protocolVersion
                writeVarInt(handShakeStream, ProtocolVersion);
                //writing hostLength
                writeVarInt(handShakeStream, _host.Length);
                //a safe way to convent string to byte
                foreach (char c in _host)
                {
                    handShakeStream.WriteByte((byte)c);
                }
                //yet and other to convent int byte
                handShakeStream.WriteByte((byte)((port >>> 8) & 0xFF));
                handShakeStream.WriteByte((byte)((port >>> 0) & 0xFF));
                //dont change it unless if u want to try login
                writeVarInt(handShakeStream, 1);
                byte[] handshakePackage = handShakeStream.ToArray();
                //send handshake length to server
                writeVarInt(Stream, handshakePackage.Length);
                //send handshake datapackage
                Stream.Write(handshakePackage, 0, handshakePackage.Length);
                //request server to send information
                Stream.WriteByte(0x01);
                Stream.WriteByte(0x00);
                //read response
                int size = readVarInt(Stream);
                int pingVersion = readVarInt(Stream);
                int length = readVarInt(Stream);
                byte[] data = new byte[length];
                Stream.Read(data, 0, length);
                return new PingResult(pingVersion, size, Encoding.UTF8.GetString(data));
            }
            catch (Exception ex)
            {
                throw new Exception("PingFail:" + ex.Message);
            }
        }

        public PingResult Ping()
        {
            return this.Ping(-1);
        }

        private static int readVarInt(Stream stream)
        {
            int i = 0;
            int j = 0;
            while (true)
            {
                int k = stream.ReadByte();
                i |= (k & 0x7F) << (j++ * 7);
                if (j > 5)
                    throw new Exception("VarInt too big");
                if ((k & 0x80) != 0x80)
                    break;
            }
            return i;
        }
        private static void writeVarInt(Stream stream, int paramInt)
        {
            while (true)
            {
                if ((paramInt & ~0x7F) == 0)
                {

                    stream.WriteByte((byte)paramInt);
                    return;
                }
                stream.WriteByte((byte)((paramInt & 0x7F) | 0x80));
                paramInt >>>= 7;

            }
        }


    }
    public class PingResult
    {
        /// <summary>
        /// ServerResponse Version
        /// </summary>
        public int TargetVersion;

        /// <summary>
        /// ServerResponse datapackage size;
        /// </summary>
        public int PackageSize;
        /// <summary>
        /// Raw Response JsonString (the key may changed dependence on FML/Bukkit or Vanilla Server))
        /// <para>See:<see href="https://wiki.vg/Server_List_Ping#Response"/></para>
        /// </summary>
        public String data = String.Empty;

        public PingResult(int targetVersion, int packageSize, String data)
        {
            this.TargetVersion = targetVersion;
            this.PackageSize = packageSize;
            this.data = data;
        }
    }

    internal class Program
    {

        public static void Main()
        {
            McServerPingHelper hpyixel = new McServerPingHelper("mc.hypixel.net");
            PingResult result = hpyixel.Ping();
            Console.WriteLine(result.TargetVersion);
            Console.WriteLine(result.PackageSize);
            Console.WriteLine(result.data);
        }

    }
}
