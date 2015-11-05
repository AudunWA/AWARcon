using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon.Messaging
{
    class Message
    {
        internal byte ServerID;
        internal string MessageText;

        internal Message(byte serverID, string messageText)
        {
            ServerID = serverID;
            MessageText = messageText;
        }
    }
}
