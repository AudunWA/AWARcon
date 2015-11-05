using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon.Modules
{
    abstract class Module : IDisposable
    {
        protected Client _server;

        internal Module(Client client)
        {
            _server = client;
            Init();
        }

        private void Init()
        {
            RegisterEvents();
        }

        protected abstract void RegisterEvents();
        protected abstract void UnregisterEvents();

        public void Dispose()
        {
            UnregisterEvents();
        }
    }
}
