using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon.Proxy
{
    public class GameClient
    {
        #region Fields
        public static uint MESSAGEID_MAX = 4000;

        internal delegate void requestHandler();
        private int _ID;
        private Socket _socket;
        private byte[] _buffer;

        public int ID { get { return _ID; } }

        public string IP { get { return _socket.RemoteEndPoint.ToString().Split(':')[0]; } }
        public bool Ponged { get; set; }

        #endregion

        #region Properties
        //public GamePlayer GamePlayer
        //{
        //    get
        //    {
        //        return mGamePlayer;
        //    }
        //    set
        //    {
        //        if (value != null)
        //            value.mClient = this;
        //        mGamePlayer = value;
        //    }
        //}
        internal bool StillAlive() // this was a thriump. I'm making a note here: HUGE SUCCESS!
        {
            return (_socket != null);
        }
        #endregion

        #region Connection stuff
        public GameClient(int ID, Socket Socket)
        {
            _ID = ID;
            _socket = Socket;
            Ponged = true;

            _buffer = new byte[2048];

            _socket.BeginReceive(_buffer, 0, 2048, SocketFlags.None, DataIn, null);
        }
        private void DataIn(IAsyncResult iAr)
        {
            try
            {
                int packetLength = _socket.EndReceive(iAr);

                if (packetLength > 0)
                {
                    int pos = 0;
                    byte[] dataIn = new byte[packetLength];
                    Array.Copy(_buffer, 0, dataIn, 0, packetLength);

                    while (pos < packetLength)
                    {
                        break;
                    }
                }
                else
                {
                    Program.CManager.RemoveClient(this);
                    return;
                }
                _socket.BeginReceive(_buffer, 0, 2048, SocketFlags.None, DataIn, null);
            }
            catch (SocketException) // Nothing special
            {
                Program.CManager.RemoveClient(this);
                return;
            }
            catch (ObjectDisposedException) // Nothing special
            {
                Program.CManager.RemoveClient(this);
                return;
            }
            catch (Exception x)
            {
                Logging.WriteFancyLine(x.ToString() + " (GameClient.DataIn)");
                Program.CManager.RemoveClient(this);
                return;
            }
        }

        public void SendData(List<byte> data)
        {
            try
            {
                string consoleText = Encoding.Default.GetString(data.ToArray()); ; // We want to replace some chars with readable text!
                for (int i = 0; i <= 13; i++) // char 0-13
                {
                    consoleText = consoleText.Replace(Convert.ToChar(i).ToString(), "{" + i + "}");
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                Logging.WriteFancyLine("Outgoing data to client [" + _ID + "]: \"" + consoleText + "\"");
                Console.ResetColor();

                _socket.Send(data.ToArray());
                //mSocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(DataSent), null);
            }
            catch (SocketException) // Nothing special
            {
                Program.CManager.RemoveClient(this);
            }
            catch (ObjectDisposedException) // Nothing special
            {
                Program.CManager.RemoveClient(this);
            }
            catch (Exception x)
            {
                Logging.WriteFancyLine(x + " (GameClient.SendData)");
                Program.CManager.RemoveClient(this);
            }
        }

        public void Stop()
        {
            if (!StillAlive())
                return;

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            _socket.Dispose();

            _socket = null;
            _buffer = null;
        }
        #endregion
    }
}
