using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon.Modules
{
    class VotekickModule
    {
        private Client _server;
        private Dictionary<string, List<string>> _reportedPlayers; // Value = who reported the player

        public VotekickModule(Client server)
        {
            _server = server;
            _reportedPlayers = new Dictionary<string, List<string>>();

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
                case "votekick":
                    if (_server.PlayerCount < 10) // easily abused
                    {
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} Votekick failed, needs 10+ players.", player.ID));
                        break;
                    }

                    string reportedPlayerName = parameters;
                    PlayerInfo reportedPlayer = _server.GetPlayerByName(reportedPlayerName);

                    if (reportedPlayer == null)
                    {
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} Cannot find that player!", player.ID));
                        break;
                    }
                    if (Program.IsAdmin(reportedPlayer)) // nop
                    {
                        _server.GetBattlEyeClient().SendCommand(String.Format("say {0} no, no... no kick admin... bad player...", player.ID));
                        break;
                    }

                    if (_reportedPlayers.ContainsKey(reportedPlayerName))
                    {
                        if (_reportedPlayers[reportedPlayerName].Contains(player.Name)) // Prevents double report
                            break;
                        _reportedPlayers[reportedPlayerName].Add(player.Name);

                        if (_reportedPlayers[reportedPlayerName].Count >= Math.Floor(_server.PlayerCount * 0.40)) // 40% of server voted on kick
                        {
                            _server.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Kick, String.Format("{0} Vote kick!", reportedPlayer.ID));
                            break;
                        }
                    }
                    else
                        _reportedPlayers.Add(reportedPlayerName, new List<string> { player.Name });

                    // Send status
                    _server.GetBattlEyeClient().SendCommand(String.Format("say -1 {0} voted for kicking {1} ({2} of {3} required votes)", player.Name, reportedPlayerName, _reportedPlayers[reportedPlayerName].Count, Math.Floor(_server.PlayerCount * 0.40)));
                    break;
                case "help":
                        break;
            }
        }

        private void OnPlayerDisconnect(PlayerInfo player)
        {
            if (_reportedPlayers.ContainsKey(player.Name))
                _reportedPlayers.Remove(player.Name);
        }
    }
}
