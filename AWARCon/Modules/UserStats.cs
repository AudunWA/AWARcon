using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWARCon.MySQL;
using BattleNET;

namespace AWARCon.Modules
{
    class UserStats : Module
    {
        public UserStats(Client client) : base(client)
        {
        }

        protected override void RegisterEvents()
        {
            _server.OnPlayerGUID += OnPlayerGUID;
            _server.OnPlayerDisconnect += OnPlayerDisconnect;
            _server.GetBattlEyeClient().BattlEyeDisconnected += OnBattlEyeDisconneted;
        }

        private void OnPlayerDisconnect(PlayerInfo player)
        {
            if (!player.HasVerifiedGUID)
                return;

            TimeSpan timeOnline = DateTime.Now - player.LoginTime;

            using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
            {
                dbClient.AddParameter("guid", player.GUID);
                dbClient.AddParameter("time", timeOnline.TotalSeconds);
                dbClient.AddParameter("name", player.Name);
                dbClient.ExecuteQuery("UPDATE player SET time_online = time_online + @time, last_username = @name WHERE guid = @guid");

                dbClient.AddParameter("guid", player.GUID);
                dbClient.AddParameter("name", player.Name);
                dbClient.ExecuteQuery("INSERT INTO player_log(guid,player_name,time,type) VALUES(@guid,@name,NOW(),'logout')");
            }
        }

        private void OnPlayerGUID(PlayerInfo player)
        {
            int loginCount = 0;

            LoadPlayerStats(player);
            using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
            {
                dbClient.AddParameter("guid", player.GUID);
                dbClient.AddParameter("name", player.Name);
                dbClient.ExecuteQuery("INSERT INTO player_log(guid,player_name,time,type) VALUES(@guid,@name,NOW(),'login')");

                dbClient.AddParameter("guid", player.GUID);
                loginCount = dbClient.ReadInt32("SELECT COUNT(*) FROM player_log WHERE guid = @guid AND type = 'login'");
            }
            Task task = new Task(async delegate
            {

                await Task.Delay(10000);
                _server.GetBattlEyeClient().SendCommand(BattlEyeCommand.Say, String.Format("{0} Hey {1}, this is your {2} visit on the AWA Standalone Server!", player.ID, player.Name, NumberUtil.ToOrdinal(loginCount)));
                _server.GetBattlEyeClient().SendCommand(BattlEyeCommand.Say, String.Format("{0} Your total play time on the server: {1}", player.ID, player.TimePlayed.ToReadableString()));
            });
            task.Start();
        }

        private void OnBattlEyeDisconneted(BattlEyeDisconnectEventArgs args)
        {
            // Save player stats on disconnect
            lock (_server.PlayerList)
            {
                foreach (PlayerInfo player in _server.PlayerList.Values)
                {
                    OnPlayerDisconnect(player);
                }
            }
        }

        protected override void UnregisterEvents()
        {
            _server.OnPlayerGUID -= OnPlayerGUID;
        }

        private void LoadPlayerStats(PlayerInfo player)
        {
            using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
            {
                dbClient.AddParameter("guid", player.GUID);
                DataRow row = dbClient.ReadDataRow("SELECT * FROM player WHERE guid = @guid");

                if (row == null) // We insert
                {
                    dbClient.AddParameter("guid", player.GUID);
                    dbClient.AddParameter("name", player.Name);
                    dbClient.ExecuteQuery("INSERT INTO player VALUES(@guid,0,@name)");
                    player.TimePlayed = new TimeSpan();
                }
                else
                {
                    player.TimePlayed = TimeSpan.FromSeconds((int) row["time_online"]);
                }
            }
        }
    }
}
