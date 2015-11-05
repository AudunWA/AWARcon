using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AWARCon.Proxy;
using AWARCon.MySQL;
using System.Data;

namespace AWARCon
{
    class Program
    {
        public static List<Client> RConClients;
        //public static DatabaseManager GlobalDbManager;
        public static AWADatabaseManager DBManager;

        public static ClientManager CManager;
        public static List<PlayerInfo> Administrators;

        static void Main(string[] args)
        {
            Console.Title = "AWA RCon";
            Logging.WriteFancyLine("Starting the AWA RCon", ConsoleColor.Green);
            Configuration.ParseConfig("config.cfg");
            RConClients = new List<Client>();
            DBManager = new AWADatabaseManager();
            Logging.WriteBlank();

            Administrators = new List<PlayerInfo>();
            DataTable result;
            using (DatabaseClient dbClient = DBManager.GetGlobalClient())
                result = dbClient.ReadDataTable("SELECT * FROM bypasses");
            foreach (DataRow row in result.Rows)
                Administrators.Add(new PlayerInfo { GUID = (string)row["guid"], IP = (string)row["ip"] });

            int clientCount = int.Parse(Configuration.Entries["server.count"]);
            for (int i = 1; i <= clientCount; i++)
            {
                Client client = new Client();
                client.serverID = i;
                client.hiveID = int.Parse(Configuration.Entries["server" + i + ".hive"]);
                client.IP = Configuration.Entries["server" + i + ".ip"];
                client.Port = int.Parse(Configuration.Entries["server" + i + ".port"]);
                client.Password = Configuration.Entries["server" + i + ".password"];
                client.IsOrigins = Configuration.Entries.ContainsKey("server" + i + ".origins");
                RConClients.Add(client);

                client.Start();
            }
            Logging.WriteBlank();
            //NotificationsLoop();
            //ClientMonitor();
        }

        private static void ClientMonitor()
        {
            int counter = 0;
            while (true)
            {
                try
                {
                    List<int> ClientsToRefresh = new List<int>();

                    foreach (Client client in RConClients)
                    {
                        // Logging.WriteFancyLine("TEST: " + (DateTime.Now - client.LastMessageRecieved).Seconds);
                        if ((DateTime.Now - client.LastMessageRecieved).Seconds >= 120 || (DateTime.Now - client.LastMessageRecieved).Minutes >= 2 || (DateTime.Now - client.LastMessageRecieved).Hours >= 1) // probably timed out or something, reset
                        {
                            Logging.WriteFancyLine("#" + client.serverID + " has timed out, reconnecting..");
                            ClientsToRefresh.Add(client.serverID);
                        }
                        else
                        {
                            if (counter >= 1000) // Time for message
                            {
                                //client.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, "-1 Please go to awkack.org/index.php?topic=1193 to vote for a character wipe!");
                                //client.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, "-1 If we get enough votes for yes, everything will be flushed and everyone will get a fresh start.");
                                client.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, "-1 The servers will be wiped on this friday, a fresh start for everyone!");
                            }
                        }
                    }

                    foreach (int clientID in ClientsToRefresh)
                    {
                        Client client = GetRConClient(clientID);
                        client.Stop();
                        RConClients.Remove(client);

                        client = new Client();
                        client.serverID = clientID;
                        client.hiveID = int.Parse(Configuration.Entries["server" + clientID + ".hive"]);
                        client.IP = Configuration.Entries["server" + clientID + ".ip"];
                        client.Port = int.Parse(Configuration.Entries["server" + clientID + ".port"]);
                        client.Password = Configuration.Entries["server" + clientID + ".password"];

                        client.Start();
                        RConClients.Add(client);
                    }
                }
                catch (Exception x) { Logging.LogError(x.ToString()); }
               Thread.Sleep(1000);
                if(counter >= 500)
                    counter = 0;
               counter++;
            }
        }

        private static void NotificationsLoop()
        {
            int counter = 0;
            while (true)
            {
                if (counter >= 600) // every 10th minute
                {
                    //client.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, "-1 Please go to awkack.org/index.php?topic=1193 to vote for a character wipe!");
                    //client.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, "-1 If we get enough votes for yes, everything will be flushed and everyone will get a fresh start.");
                    foreach (Client client in RConClients)
                    {
                        client.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, "-1 Due to several downsides with the DayZ mod, and a new update of The War Z, we have decided to shut down our DayZ servers.");
                        client.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, "-1 You will in a few days be able to access The AWA WARZ servers! For more info about the new AWA WARZ, go to http://awkack.org/!");
                    }
                    counter = 0;
                }
                Thread.Sleep(1000);
                counter++;
            }
        }

        public static Client GetRConClient(int serverID)
        {
            foreach (Client client in RConClients)
                if (client.serverID == serverID)
                    return client;
            return null;
        }

        public static void MergeBanLists()
        {
            foreach (Client client in RConClients)
            {
                client.GetBattlEyeClient().SendCommand("bans");
            }
            foreach (Client client in RConClients)
            {
                DateTime startTime = DateTime.Now;
                while (!client.GotBanList)
                {

                }
            }
        }

        public static bool IsAdmin(PlayerInfo player)
        {
            return Administrators.Any(t => t.IP == player.IP || t.GUID == player.GUID);
        }
    }
}
