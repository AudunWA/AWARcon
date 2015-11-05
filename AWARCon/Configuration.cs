using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon
{
    public static class Configuration
    {
        internal static Dictionary<string, string> Entries;

        public static void ParseConfig(string path)
        {
            Entries = new Dictionary<string, string>();

            string[] lines = File.ReadAllLines(path);

            foreach (string entry in lines)
            {
                string line = entry.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//")) // starts with comment
                    continue;

                Entries.Add(line.Substring(0, line.IndexOf('=')), line.Substring(line.IndexOf('=') + 1));
            }
        }
    }
}
