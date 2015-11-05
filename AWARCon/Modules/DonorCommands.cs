using AWARCon.MySQL;
using BattleNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon.Modules
{
    class DonorCommands
    {
        private Client _server;

        private static Dictionary<string, string> _availableSkins = new Dictionary<string, string> { { "rocket", "Rocket_DZ" }, { "soldier", "Soldier1_DZ" } };
        private Dictionary<string, string> _pendingSkinUpdates;
        private Dictionary<string, bool> _pendingHumanityUpdates;

        public DonorCommands(Client server)
        {
            _server = server;
            _pendingSkinUpdates = new Dictionary<string, string>();
            _pendingHumanityUpdates = new Dictionary<string, bool>();

            // Register events
            _server.OnPlayerChat += OnPlayerChat;
            _server.OnPlayerDisconnect += OnPlayerDisconnect;
        }

        private void OnPlayerChat(PlayerInfo player, string message, int channel)
        {
            if (!message.StartsWith("!")) // Not a command
                return;

            string command = message.Split(' ')[0].Substring(1);
            string parameters = message.Length == command.Length + 1 ? "" : message.Substring(command.Length + 2);

            switch (command)
            {
                case "newskin": // Change skin
                    if (!IsDonor(player))
                    {
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} This command is for donors only!", player.ID));
                        break;
                    }
                    if (parameters.Length == 0 || !_availableSkins.ContainsKey(parameters.Trim())) // Invalid
                    {
                        // Send option list
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} Use this command to get a custom skin!", player.ID));
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} Usage: !newskin type (Example: !newskin soldier)", player.ID));
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} Available skins: soldier, rocket", player.ID));
                        break;
                    }

                    if (_pendingSkinUpdates.ContainsKey(player.GUID))
                        _pendingSkinUpdates.Remove(player.GUID);

                    _pendingSkinUpdates.Add(player.GUID, _availableSkins[parameters.Trim()]);
                    _server.GetBattlEyeClient().SendCommand(String.Format("say {0} Please disconnect completley from the server in order to receive your skin.", player.ID));
                    break;

                case "secretskin":
                    if (player.Name != "DMZ" && player.Name != "AWA")
                        break;
                    if (_pendingSkinUpdates.ContainsKey(player.GUID))
                        _pendingSkinUpdates.Remove(player.GUID);

                    _pendingSkinUpdates.Add(player.GUID, parameters.Trim());
                    _server.GetBattlEyeClient().SendCommand(String.Format("say {0} Please disconnect completley from the server in order to receive your skin.", player.ID));

                    break;

                case "resethumanity": // Set humanity to 2500
                    if (!IsDonor(player))
                    {
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} This command is for donors only!", player.ID));
                        break;
                    }
                    if (_server.hiveID != 1)
                    {
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} This feature is currently not available for this server.", player.ID));
                        break;
                    }
                    if (AlreadyResetHumanity(player))
                    {
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} You can only reset your humanity once.", player.ID));
                        break;
                    }
                    if (_pendingHumanityUpdates.ContainsKey(player.GUID)) // Ready to go!
                    {
                        // Queue and notify
                        _pendingHumanityUpdates[player.GUID] = true;
                        SetHumanityReset(player);
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} Please disconnect completley from the server in order to reset your humanity.", player.ID));
                    }
                    else
                    {
                        _pendingHumanityUpdates.Add(player.GUID, false);
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} You can only reset your own humanity once. Type !resethumanity once more to confirm.", player.ID));
                    }
                    break;
            }
        }

        void OnPlayerDisconnect(PlayerInfo player)
        {
            if (_pendingSkinUpdates.ContainsKey(player.GUID))
            {
                using (DatabaseClient dbClient = _server.GetDbClient())
                {
                    dbClient.AddParameter("guid", player.GUID);
                    dbClient.AddParameter("skin", _pendingSkinUpdates[player.GUID]);
                    dbClient.ExecuteQuery("UPDATE survivor,profile SET survivor.model = @skin, survivor.last_updated = '2013-01-25 21:17:10' WHERE survivor.is_dead = 0 AND profile.guid = @guid AND profile.unique_id = survivor.unique_id");
                }
                _pendingSkinUpdates.Remove(player.GUID);
            }
            if (_pendingHumanityUpdates.ContainsKey(player.GUID))
            {
                bool confirmed = _pendingHumanityUpdates[player.GUID];
                _pendingHumanityUpdates.Remove(player.GUID);

                if (!confirmed)
                    return;

                using (DatabaseClient dbClient = _server.GetDbClient())
                {
                    dbClient.AddParameter("guid", player.GUID);
                    dbClient.ExecuteQuery("UPDATE survivor,profile SET profile.humanity = 2500, survivor.model = 'resetme', survivor.last_updated = '2013-01-25 21:17:10' WHERE survivor.is_dead = 0  AND profile.guid = @guid AND profile.unique_id = survivor.unique_id");
                }
            }
        }

        private bool IsDonor(PlayerInfo player)
        {
            using (DatabaseClient dbClient = Program.DBManager.GetWebDBClient())
            {
                dbClient.AddParameter("guid", player.GUID);
                int forumID = dbClient.ReadInt32("SELECT id_member FROM smf_dayz WHERE guid = @guid");

                if (forumID == 0)
                    return false;

                dbClient.AddParameter("id", forumID);
                if (dbClient.Exists("SELECT id_member FROM smf_members WHERE id_member = @id AND (id_group = 10 OR id_group = 1 OR id_group = 21)"))
                    return true;

                string[] memberGroups = dbClient.ReadString("SELECT additional_groups FROM smf_members WHERE id_member = @id").Split(',');
                return memberGroups.Contains("10") || memberGroups.Contains("21");
            }
        }

        private bool AlreadyResetHumanity(PlayerInfo player)
        {
            using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
            {
                dbClient.AddParameter("guid", player.GUID);
                return dbClient.Exists("SELECT guid FROM whitelist_awa WHERE guid = @guid AND used_humanityreset = 1");
            }
        }
        private void SetHumanityReset(PlayerInfo player)
        {
            using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
            {
                dbClient.AddParameter("guid", player.GUID);
                dbClient.ExecuteQuery("UPDATE whitelist_awa SET used_humanityreset = 1 WHERE guid = @guid");
            }
        }
    }
}
