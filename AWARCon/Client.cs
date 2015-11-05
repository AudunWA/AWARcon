using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BattleNET;
using AWARCon.MySQL;
using System.Net;
using AWARCon.Modules;

namespace AWARCon
{
    internal class Client
    {
        internal int serverID;
        internal int hiveID;
        internal string IP;
        internal int Port;
        internal string Password;

        private bool _isConnecting;
        private BattlEyeClient _beClient;

        internal Dictionary<int, PlayerInfo> PlayerList;
        public DateTime LastMessageRecieved;

        private Whitelist _whitelist;
        private DonorCommands _donorCommands;
        private VotekickModule _votekickModule;
        private DonationGoalReminder _donationGoalReminder;
        private List<Module> _modules;

        // for ban list sync
        public bool GotBanList;
        public bool IsOrigins;

        // Delegates
        internal delegate void PlayerStatusEventHandler(PlayerInfo player);

        internal delegate void PlayerChatEventHandler(PlayerInfo player, string message, int channel);

        // Events
        internal event PlayerStatusEventHandler OnPlayerConnect;
        internal event PlayerStatusEventHandler OnPlayerDisconnect;
        internal event PlayerStatusEventHandler OnPlayerGUID;
        internal event PlayerChatEventHandler OnPlayerChat;

        internal int PlayerCount
        {
            get { return PlayerList.Count; }
        }

        internal DatabaseClient GetDbClient()
        {
            return Program.DBManager.GetDbClient(hiveID);
        }

        internal void Start()
        {
            LastMessageRecieved = DateTime.Now;
            PlayerList = new Dictionary<int, PlayerInfo>();

            if (Configuration.Entries["server" + serverID + ".whitelist"] == "1")
            {
                _whitelist = new Whitelist(this);
            }

            // Register events
            OnPlayerGUID += PlayerGUIDVerifiedEvent;
            OnPlayerDisconnect += PlayerDisconnectedEvent;

            try
            {
                Connect();
            }
            catch
            {
                Logging.LogError("Fatal error (yeah, this one)");
            }
        }

        private void Connect()
        {
            _beClient = new BattlEyeClient(new BattlEyeLoginCredentials(IPAddress.Parse(IP), Port, Password));

            //_beClient.ReconnectOnPacketLoss = true;
            _beClient.BattlEyeConnected += _beClient_BattlEyeConnected;
            _beClient.BattlEyeMessageReceived += MessageFromServerEvent;
            _beClient.BattlEyeDisconnected += ServerDisconnectEvent;

            //_isConnecting = true;

            _beClient.Connect();
        }

        private void _beClient_BattlEyeConnected(BattlEyeConnectEventArgs args)
        {
            if (args.ConnectionResult != BattlEyeConnectionResult.Success)
            {
                // Couldn't connect somehow
                Logging.WriteServerLine("Could not connect to server", serverID.ToString());
                Connect();
                return;
            }

            // Start modules
            //_donorCommands = new DonorCommands(this);
            //_votekickModule = new VotekickModule(this);
            //_donationGoalReminder = new DonationGoalReminder(this);

            _modules = new List<Module>();
            _modules.Add(new UserStats(this));

            Logging.WriteServerLine("Connected to server!", serverID.ToString());

            // Fetch player list
            _beClient.SendCommand(BattlEyeCommand.Players);
        }

        private void ServerDisconnectEvent(BattlEyeDisconnectEventArgs args)
        {
            //if (args.DisconnectionType == BattlEyeDisconnectionType.ConnectionLost)
            //    return;

            Logging.WriteServerLine("Disconnected from server! (" + args.DisconnectionType + ")", serverID.ToString());
            Logging.WriteServerLine("Reconnecting...", serverID.ToString());

            Stop();
            //Thread.Sleep(5000); // sleep 5 seconds
            Connect();
        }

        private void MessageFromServerEvent(BattlEyeMessageEventArgs args)
        {
            //Logging.WriteFancyLine("SERVER {0}: {1}", serverID, args.Message);
            //return;
            //LastMessageRecieved = DateTime.Now;

            try
            {
                if (args.Message.Contains(") has been kicked by BattlEye: Global Ban #")) // Probably no good IP either!
                {
                    string help = args.Message.Substring(args.Message.IndexOf('#') + 1);
                    int ID = int.Parse(help.Split(' ')[0]);
                    string name = "";

                    help = help.Substring(ID.ToString().Length + 1);
                    name = help.Substring(0, help.IndexOf(" ("));
                    PlayerInfo p = GetPlayerByName(name);
                    _beClient.SendCommand("addban " + p.IP + " -1 Previous global ban");
                    _beClient.SendCommand(BattlEyeCommand.LoadBans);
                }
                else if (args.Message.StartsWith("Player #")) // Connect or disconnect!
                {
                    // :27 BattlEye Server: Player #28 NeRo (212.37.167.4:2304) connected
                    // :42 BattlEye Server: Player #57 johnjuanita disconnected

                    string help = args.Message.Substring(args.Message.IndexOf('#') + 1);
                    int ID = int.Parse(help.Split(' ')[0]);
                    string name = "";

                    if (args.Message.EndsWith("disconnected"))
                    {
                        help = help.Substring(ID.ToString().Length + 1);
                        name = help.Substring(0, help.LastIndexOf(" disconnected"));
                        Logging.WriteServerLine(
                            String.Format("Player #{0} - \"{1}\" disconnected from the server.", ID, name),
                            serverID.ToString(), ConsoleColor.Gray);

                        if (PlayerList.ContainsKey(ID))
                        {
                            if (OnPlayerDisconnect != null)
                                OnPlayerDisconnect(PlayerList[ID]);

                            PlayerList.Remove(ID);
                        }
                    }
                    else if (args.Message.EndsWith("connected"))
                    {
                        help = help.Substring(ID.ToString().Length + 1);
                        name = help.Substring(0, help.LastIndexOf('(') - 1);
                        //name = args.Message.Substring(args.Message.IndexOf('#') + 2 + ID.ToString().Length, args.Message.LastIndexOf('(') - 2);
                        string IP = args.Message.Split('(')[1].Split(')')[0].Split(':')[0];
                        Logging.WriteServerLine(String.Format("New player connected: #{0} - {1} ({2})", ID, name, IP),
                            serverID.ToString());

                        PlayerInfo p = new PlayerInfo();
                        p.ID = ID;
                        p.Name = name;
                        p.IP = IP;

                        if (PlayerList.ContainsKey(ID))
                            PlayerList.Remove(ID);

                        PlayerList.Add(ID, p);

                        if (OnPlayerConnect != null)
                            OnPlayerConnect(p);
                    }
                }
                else if (args.Message.StartsWith("Verified GUID")) // There's our GUID!
                {
                    //  Verified GUID (3d22d90cf76ff44d6b5572d33605dbe5) of player #42 Harvey
                    string help = args.Message.Substring(args.Message.IndexOf('#') + 1);
                    int ID = int.Parse(help.Split(' ')[0]);
                    string GUID = args.Message.Split('(')[1].Split(')')[0];

                    if (PlayerList.ContainsKey(ID))
                    {
                        PlayerInfo player = PlayerList[ID];
                        player.GUID = GUID;
                        //Logging.WriteFancyLine("#" + serverID + ": PLAYER GUID: " + ID + "," + GUID);
                        Logging.WriteServerLine(
                            String.Format("Got GUID for player #{0} - {1} ({2})", ID, player.Name, player.IP),
                            serverID.ToString());

                        if (OnPlayerGUID != null)
                            OnPlayerGUID(player);
                    }
                }
                else if (args.Message.StartsWith("Players on server:")) // Player list result
                {
                    ParsePlayerList(args);
                }
                else
                {
                }

                // Check for chat message
                int chatType = -1;
                if (args.Message.StartsWith("(Global)"))
                    chatType = 0;
                else if (args.Message.StartsWith("(Side)"))
                    chatType = 1;
                else if (args.Message.StartsWith("(Group)"))
                    chatType = 3;
                else if (args.Message.StartsWith("(Vehicle)"))
                    chatType = 4;
                else if (args.Message.StartsWith("(Direct)"))
                    chatType = 5;

                if (chatType > -1)
                {
                    string name =
                        args.Message.Substring(0, args.Message.IndexOf(':')).Substring(args.Message.IndexOf(')') + 2);
                    string message = args.Message.Substring(args.Message.IndexOf(':') + 2);

                    PlayerInfo player = GetPlayerByName(name);

                    //:28 BattlEye Server: (Direct) Emil: Where's the car?
                    //string name = args.Message.Substring(args.Message.IndexOf(')') + 2, args.Message.IndexOf(':'));
                    //string message = args.Message.Substring(args.Message.IndexOf(':') + 2);

                    //Logging.WriteFancyLine("#" + serverID + ": <" + chatType + "> " + name + ": " + message);
                    Logging.WriteServerLine(String.Format("<{0}> {1}: {2}", chatType, name, message),
                        serverID.ToString());

                    if (player != null)
                    {
                        if (player.GUID == null)
                            return;
                        using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
                        {
                            dbClient.AddParameter("server_id", serverID);
                            dbClient.AddParameter("guid", player.GUID);
                            dbClient.AddParameter("name", player.Name);
                            dbClient.AddParameter("ip", player.IP);
                            dbClient.AddParameter("chat_type", chatType);
                            dbClient.AddParameter("text", message);

                            dbClient.ExecuteQuery(
                                "INSERT INTO chatlog VALUES(@server_id,NOW(),@guid,@name,@ip,@chat_type,@text)");
                        }
                        if (OnPlayerChat != null)
                            OnPlayerChat(player, message, chatType);
                    }

                    if (message.StartsWith("/")) // Command?
                    {
                        string[] commandArguments = message.Split(' ');
                        string command = commandArguments[0].ToLower().Replace("/", "");

                        switch (command)
                        {
                            case "testcmd":
                                _beClient.SendCommand(BattlEyeCommand.Say, "-1 Test command by AWA!");
                                break;
                            case "players":
                                _beClient.SendCommand(BattlEyeCommand.Players);
                                break;
                            case "ts":
                                _beClient.SendCommand(BattlEyeCommand.Say,
                                    player.ID + " The TeamSpeak address is ts.awkack.org:9990.");
                                break;
                            case "help":
                                _beClient.SendCommand(BattlEyeCommand.Say,
                                    String.Format("{0} Available commands: ", player.ID));
                                _beClient.SendCommand(BattlEyeCommand.Say,
                                    String.Format("{0} /help - shows available commands", player.ID));
                                _beClient.SendCommand(BattlEyeCommand.Say,
                                    String.Format("{0} /ts - shows the teamspeak address", player.ID));
                                break;
                        }
                    }
                }
            }
                // Lack of this fucking error handling has caused so much pain in my ass (error handled in BattleNET and disconnection)
            catch (Exception x)
            {
                Logging.WriteFancyLine(
                    String.Format("Error in message, content: \"{0}\"\r\n{1}", args.Message, x.ToString()),
                    ConsoleColor.Red);
            }
            // _beClient.SendCommand(BattlEyeCommand.Say, "-1 ... wants to be kicked!");
        }

        private void ParsePlayerList(BattlEyeMessageEventArgs args)
        {
            string[] players = args.Message.Split('\n');
            lock (PlayerList)
            {
                PlayerList.Clear();
                // players start on line 3, ends 1 before end
                for (int i = 3; i < players.Length - 1; i++)
                {
                    string[] playerString = players[i].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    PlayerInfo player = new PlayerInfo();
                    player.ID = int.Parse(playerString[0]);
                    player.IP = playerString[1].Split(':')[0];
                    player.GUID = playerString[3].Replace("(OK)", "");
                    player.Name = string.Join(" ", playerString, 4, playerString.Length - 4).Replace(" (Lobby)", "");
                    PlayerList.Add(player.ID, player);
                    Logging.WriteServerLine(
                        String.Format("Got player: #{0} - {1} ({2})", player.ID, player.Name, player.IP),
                        serverID.ToString());
                }
            }
        }

        public PlayerInfo GetPlayerByName(string name)
        {
            foreach (PlayerInfo p in PlayerList.Values)
                if (p.Name == name)
                    return p;
            return null;
        }

        private void PlayerGUIDVerifiedEvent(PlayerInfo player)
        {
            if (Program.IsAdmin(player)) // bypass fuck yeah
                return;

            using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
            {
                dbClient.AddParameter("guid", player.GUID);

                // Anti-ghosting
                DataRow result =
                    dbClient.ReadDataRow("SELECT instance,timeout_end FROM awa_serverhop WHERE guid = @guid");

                // try
                // {
                if (result == null || (DateTime.Parse(result[1].ToString()) - DateTime.Now).Minutes <= 0)
                {
                    // Not in whitelist, generate code and kick.
                    //string code = GetRandomString().ToLower().Replace("o", "").Replace("0", "").Replace("l", "").Replace("1", "");
                    //dbClient.AddParamWithValue("ip", player.IP);
                    //dbClient.AddParamWithValue("code", code);
                    //dbClient.AddParamWithValue("name", player.Name);
                    //dbClient.AddParamWithValue("id", serverID);

                    //dbClient.ExecuteQuery("REPLACE INTO awa_serverhop(guid,instance,timeout_end) VALUES(@guid,@id,DATE_ADD(NOW(), INTERVAL 1 HOUR))");

                    //_beClient.SendCommand("You can only play on 1 of the server ATM.");
                    // Logging.WriteFancyLine("#" + serverID + ": SENDING KICK/MESSAGE");
                    //TODO: kick
                }
                //else if ((DateTime.Parse(result[1].ToString()) - DateTime.Now).Minutes <= 0) // expired, delete it
                //{
                //    dbClient.ExecuteQuery("DELETE FROM awa_serverhop WHERE guid = @guid");
                //}
                if (result != null && (DateTime.Parse(result[1].ToString()) - DateTime.Now).Minutes >= 0 &&
                    Convert.ToInt32(result[0]) != serverID &&
                    Program.GetRConClient(int.Parse(result[0].ToString())).hiveID == hiveID)
                {
                    _beClient.SendCommand(BattlEyeCommand.Kick,
                        player.ID + " ANTI-GHOST: You need to wait " +
                        (DateTime.Parse(result["timeout_end"].ToString()) - DateTime.Now).Minutes + " minutes to play.");
                    //_beClient.SendCommand(BattlEyeCommand.Kick, playerID + " Whitelist code: \"" + row["code"] + "\"! http://awkack.org for more info");
                    Logging.WriteServerLine(
                        String.Format("Kicking player \"{0}\" because of anti-ghosting.", player.Name),
                        serverID.ToString(), ConsoleColor.Gray);
                }
                //}
                //catch (Exception x) { Logging.WriteFancyLine(x.ToString()); }
            }
        }

        private void PlayerDisconnectedEvent(PlayerInfo player)
        {
            if (string.IsNullOrEmpty(player.GUID))
                return;

            if (!Program.IsAdmin(player)) // admins don't need this crap
            {
                // Anti-ghosting stuff
                DateTime expireTime = DateTime.Now.AddMinutes(11);
                using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
                {
                    dbClient.AddParameter("guid", player.GUID);
                    dbClient.AddParameter("id", serverID);
                    dbClient.AddParameter("time", expireTime.ToString());
                    dbClient.ExecuteQuery(
                        "REPLACE INTO awa_serverhop(guid,instance,timeout_end) VALUES(@guid,@id,@time)");
                }
            }

            //if (!IsOrigins)
            //{
            //    string UID = GetUID(player.Name, GetDbClient());
            //    if (string.IsNullOrEmpty(UID)) // Couldn't get it
            //        return;
            //    foreach (DatabaseManager manager in Program.DBManager.GetAllHives().Values)
            //    {
            //        using (DatabaseClient dbClient = manager.GetClient())
            //        {
            //            dbClient.AddParameterWithValue("guid", player.GUID);
            //            dbClient.AddParameterWithValue("uid", UID);
            //            dbClient.ExecuteQuery("UPDATE profile SET guid = @guid WHERE unique_id = @uid");
            //        }
            //    }
            //}
        }

        //internal string GetUID(string name, DatabaseClient dbClient)
        //{
        //    dbClient.AddParameterWithValue("name", name);
        //    return dbClient.ReadString("SELECT profile.unique_id FROM survivor, profile WHERE profile.name = @name AND survivor.unique_id = profile.unique_id ORDER by survivor.last_updated DESC LIMIT 1");
        //}

        public void Stop()
        {
            _beClient.ReconnectOnPacketLoss = false;
            _beClient.BattlEyeConnected -= _beClient_BattlEyeConnected;
            _beClient.BattlEyeDisconnected -= ServerDisconnectEvent;
            _beClient.BattlEyeMessageReceived -= MessageFromServerEvent;
            _beClient.Disconnect();
        }

        public PlayerInfo GetPlayer(int ID)
        {
            if (PlayerList.ContainsKey(ID))
                return PlayerList[ID];
            return null;
        }

        public BattlEyeClient GetBattlEyeClient()
        {
            return _beClient;
        }
    }
}
