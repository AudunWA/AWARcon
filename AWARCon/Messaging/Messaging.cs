using AWARCon.MySQL;
using BattleNET;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AWARCon.Messaging
{
    class Messaging
    {
        private List<Message> _messages;

        public Messaging()
        {
            _messages = new List<Message>();
        }

        private void LoadMessages()
        {
            _messages.Clear();
            using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
            {
                foreach(DataRow row in dbClient.ReadDataTable("SELECT * FROM broadcasting").Rows)
                    _messages.Add(new Message(Convert.ToByte(row[0]), Convert.ToString(row[1])));
            }
        }

        private void BroadcastingLoop()
        {
            while (true)
            {
                foreach (Message message in _messages)
                {
                    Client client = Program.GetRConClient(message.ServerID);
                    client.GetBattlEyeClient().SendCommand(BattlEyeCommand.Say, "-1 " + message.MessageText);
                }
                Thread.Sleep(540000);
            }
        }
    }
}
