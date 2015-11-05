using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon
{
    class LogParser
    {
        public const string markerLine = "##### AWA LOG PARSER - DO NOT REMOVE! #####";
        //public const int checkInterval =

        private string _logPath;

        public LogParser(string logName)
        {
            _logPath = logName;
        }

        public void StartParsingThread()
        {
        }
    }
}
