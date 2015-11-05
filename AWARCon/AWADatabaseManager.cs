using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWARCon.MySQL;

namespace AWARCon
{
    class AWADatabaseManager
    {
        private DatabaseManager _mainDatabase;
        private DatabaseManager _webDatabase;

        private Dictionary<int, DatabaseManager> _hiveDatabases;

        internal string MySQLIP;
        internal int MySQLPort;
        internal string MySQLUser;
        internal string MySQLPassword;
        internal string MySQLDatabase;

        public AWADatabaseManager()
        {
            _hiveDatabases = new Dictionary<int, DatabaseManager>();
            ReadMySQLInfo();

            _mainDatabase = new DatabaseManager(MySQLIP, (int)MySQLPort, MySQLUser, MySQLPassword, MySQLDatabase);
            //_webDatabase = new DatabaseManager(Configuration.Entries["web.ip"], int.Parse(Configuration.Entries["web.port"]), Configuration.Entries["web.username"], Configuration.Entries["web.password"], Configuration.Entries["web.database"]);

            if (_mainDatabase.TestConnection())
            {
                Logging.WriteFancyLine("Main MySQL connection tested and working!", ConsoleColor.Green);
            }
            else
            {
                Logging.WriteFancyLine("Main MySQL doesn't work, bitch!", ConsoleColor.Red);
                Console.ReadKey(true);
                return;
            }

            //if (_webDatabase.TestConnection())
            //{
            //    Logging.WriteFancyLine("Web MySQL connection tested and working!", ConsoleColor.Green);
            //}
            //else
            //{
            //    Logging.WriteFancyLine("Web MySQL doesn't work, bitch!", ConsoleColor.Red);
            //    Console.ReadKey(true);
            //    return;
            //}

            int hiveCount = int.Parse(Configuration.Entries["hive.count"]);
            for (int i = 1; i <= hiveCount; i++)
            {
                DatabaseManager server = new DatabaseManager(Configuration.Entries["hive" + i + ".ip"], int.Parse(Configuration.Entries["hive" + i + ".port"]), Configuration.Entries["hive" + i + ".username"], Configuration.Entries["hive" + i + ".password"], Configuration.Entries["hive" + i + ".database"]);
                //server.Host = Configuration.Entries["hive" + i + ".ip"];
                //server.Port = int.Parse(Configuration.Entries["hive" + i + ".port"]);
                //server.User = Configuration.Entries["hive" + i + ".username"];
                //server.Password = Configuration.Entries["hive" + i + ".password"];

                //Database database = new Database(Configuration.Entries["hive" + i + ".database"], 0, 50);
                //database.Name = Configuration.Entries["hive" + i + ".database"];
                //database.minPoolSize = 0;
                //database.maxPoolSize = 50;

                if (server.TestConnection())
                {
                    Logging.WriteFancyLine(String.Format("Hive #{0} MySQL connection tested and working!", i), ConsoleColor.Green);
                }
                else
                {
                    Logging.WriteFancyLine("Hive #{0} MySQL doesn't work, bitch!", ConsoleColor.Red);
                    Console.ReadKey(true);
                    return;
                }

                _hiveDatabases.Add(i, server);
            }
            Logging.WriteFancyLine("All MySQL connections ready!", ConsoleColor.Green);
        }

        private void ReadMySQLInfo()
        {
            MySQLIP = Configuration.Entries["mysql.ip"];
            MySQLPort = int.Parse(Configuration.Entries["mysql.port"]);
            MySQLUser = Configuration.Entries["mysql.username"];
            MySQLPassword = Configuration.Entries["mysql.password"];
            MySQLDatabase = Configuration.Entries["mysql.database"];
        }

        public DatabaseClient GetDbClient(int hiveID)
        {
            DatabaseManager c = null;
            if (_hiveDatabases.TryGetValue(hiveID, out c))
            {
                try
                {
                    return c.GetClient();
                }
                catch
                {
                    Logging.WriteFancyLine("MySQL error!");
                }
            }
            return null;
        }
        public Dictionary<int, DatabaseManager> GetAllHives()
        {
            return _hiveDatabases;
        }
        public DatabaseClient GetGlobalClient()
        {
            return _mainDatabase.GetClient();
        }

        public DatabaseClient GetWebDBClient()
        {
            return _webDatabase.GetClient();
        }
    }
}
