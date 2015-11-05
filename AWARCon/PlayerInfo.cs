using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon
{
    internal class PlayerInfo
    {
        public int ID;
        public string Name;
        public string GUID;
        public string IP;
        public DateTime LoginTime;
        public TimeSpan TimePlayed;

        public bool HasVerifiedGUID
        {
            get { return !string.IsNullOrWhiteSpace(GUID); }
        }

        public PlayerInfo()
        {
            LoginTime = DateTime.Now;
        }
    }
}
