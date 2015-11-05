using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon.Proxy
{
    public class RConProxy
    {
                #region Declares
        private Socket _socket;
        private readonly int _port;
        #endregion

        public RConProxy(int port)
        {
            _port = port;
        }
        #region Methods
        public void Init()
        {
            StartListen();
        }

        public void StartListen()
        {
            IPHostEntry localHostEntry;
            try
            {
                //Create a UDP socket.
                Socket soUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                }
                catch (Exception)
                {
                    Logging.WriteFancyLine("Local Host not found"); // fail
                    return;
                }
                IPEndPoint localIpEndPoint = new IPEndPoint(IPAddress.Any, 2050);
                soUdp.Bind(localIpEndPoint);
                while (true)
                {
                    Byte[] received = new Byte[256];
                    IPEndPoint tmpIpEndPoint = new IPEndPoint(IPAddress.Any, 2050);
                    EndPoint remoteEP = (tmpIpEndPoint);
                    int bytesReceived = soUdp.ReceiveFrom(received, ref remoteEP);
                    String dataReceived = System.Text.Encoding.ASCII.GetString(received);
                    Logging.WriteFancyLine("SampleClient is connected through UDP.");
                    Logging.WriteFancyLine(dataReceived);
                    String returningString = "The Server got your message through UDP:" + dataReceived;
                    Byte[] returningByte = System.Text.Encoding.ASCII.GetBytes(returningString.ToCharArray());
                    soUdp.SendTo(returningByte, remoteEP);
                }
            }
            catch (SocketException se)
            {
                Logging.WriteFancyLine("A Socket Exception has occurred!" + se.ToString());
            }
        }

        public void DataRequest(IAsyncResult iAr)
        {
            try
            {
                Program.CManager.AddClient(((Socket)iAr.AsyncState).EndAccept(iAr));
            }
            catch (SocketException) // Nothing special
            {
            }
            catch (ObjectDisposedException) // Nothing special
            {
            }
            catch (Exception x)
            {
                Logging.WriteFancyLine(x.ToString() + " (GameSocket.DataRequest)");
            }
            _socket.BeginAccept(new AsyncCallback(DataRequest), _socket);
        }
       #endregion
    }
}
