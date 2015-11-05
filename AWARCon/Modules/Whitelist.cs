using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleNET;
using AWARCon.MySQL;

namespace AWARCon.Modules
{
    class Whitelist
    {
        private Client _server;

        public Whitelist(Client server)
        {
            _server = server;

            // Register events
            _server.OnPlayerGUID += OnPlayerGUID;
        }
        public void OnPlayerGUID(PlayerInfo player)
        {
            if (Program.IsAdmin(player)) // bypass fuck yeah
                return;

            using (DatabaseClient dbClient = Program.DBManager.GetGlobalClient())
            {
                dbClient.AddParameter("guid", player.GUID);
                DataRow row = dbClient.ReadDataRow("SELECT code,confirmed FROM whitelist_awa WHERE guid = @guid");

                if (row == null)
                {
                    // Not in whitelist, generate code and kick.
                    string code = GetRandomString().ToLower().Replace("o", "").Replace("0", "").Replace("l", "").Replace("1", "");
                    dbClient.AddParameter("ip", player.IP);
                    dbClient.AddParameter("code", code);
                    dbClient.AddParameter("name", player.Name);
                    dbClient.AddParameter("id", 0);

                    dbClient.ExecuteQuery("INSERT INTO whitelist_awa(ip,code,guid,unique_id,name) VALUES(@ip,@code,@guid,@id,@name)");

                    _server.GetBattlEyeClient().SendCommand(BattlEyeCommand.Kick, string.Format("{0} Whitelist code: \"{1}\"! Go to awkack.org to register", player.ID, code));
                    Logging.WriteServerLine(String.Format("Kicking player \"{0}\" (not on whitelist).", player.Name), _server.serverID.ToString(), ConsoleColor.Gray);
                }
                else if ((bool)row["confirmed"] == false) // not confirmed..
                {
                    _server.GetBattlEyeClient().SendCommand(BattlEyeCommand.Kick, string.Format("{0} Whitelist code: \"{1}\"! Go to awkack.org to register", player.ID, row["code"]));
                    Logging.WriteServerLine(String.Format("Kicking player \"{0}\" (not confirmed in whitelist).", player.Name), _server.serverID.ToString(), ConsoleColor.Gray);
                }
            }
        }
        public static string GetRandomString()
        {
            string path = Path.GetRandomFileName();
            path = path.Replace(".", ""); // Remove period.
            return path;
        }
    }
}
