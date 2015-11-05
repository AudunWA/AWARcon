using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon.Proxy
{
    public class ClientManager
    {
        private int _maxClients;
        private Hashtable _clients;

       // internal BufferManager BufferManager;

        private string _lastConnectionIp; // For anti-ddos

        internal delegate void requestHandler();
        //internal IMessageHandler[] RequestHandlers;

        public void Init(int maxClients)
        {
            _clients = new Hashtable();
            _maxClients = maxClients;

            //BufferManager = BufferManager.CreateBufferManager(_maxClients + 10, 1024);
            //RequestHandlers = new IMessageHandler[byte.MaxValue];
            RegisterGlobal();
        }
        internal void RegisterGlobal()
        {
            //RequestHandlers[17] = new HELLO();
        }
        public void Destroy()
        {
            var copy = (Hashtable)_clients.Clone();
            foreach (GameClient client in copy.Values)
            {
                RemoveClient(client);
            }
        }

        public GameClient GetClient(string IP)
        {
            return _clients.Values.Cast<GameClient>().FirstOrDefault(client => client.IP == IP);
        }

        public int GetClientAmount(string IP)
        {
            return _clients.Values.Cast<GameClient>().Count(client => client.IP == IP);
        }

        public GameClient GetClient(uint ID)
        {
            return _clients.Values.Cast<GameClient>().FirstOrDefault(client => client.ID == ID);
        }

        private int GenerateClientID()
        {
            int i = 0;
            while (_clients.Contains(i))
                i++;
            return i;
        }
        public void AddClient(Socket socket)
        {
            if (_clients.Count >= _maxClients)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
            }
            else
            {
                var Client = new GameClient(GenerateClientID(), socket);
                _clients.Add(Client.ID, Client);

                Logging.WriteFancyLine("Accepted new client [" + Client.ID + "] from " + Client.IP);
            }
        }
        public void RemoveClient(GameClient Client)
        {
            try
            {
                if (Client != null)
                {
                    Logging.WriteFancyLine("Disconnected client [" + Client.ID + "] from " + Client.IP);
                    Client.Stop();
                    _clients.Remove(Client.ID);
                }
            }
            catch
            { }
        }
        public string GetIP(Socket socket)
        {
            return socket.RemoteEndPoint.ToString().Split(':')[0];
        }
    }
}
