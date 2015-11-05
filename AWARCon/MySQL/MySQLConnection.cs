using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWARCon.MySQL
{
    sealed class DatabaseManager
    {
        internal string ConnectionString;

        internal DatabaseManager(string server, int port, string user, string password, string database)
        {
            ConnectionString = GenerateConnectionString(server, port, user, password, database);
        }

        internal bool TestConnection()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                conn.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                Logging.WriteFancyLine(ex.ToString());
                return false;
            }
        }

        private string GenerateConnectionString(string server, int port, string user, string password, string database)
        {
            StringBuilder connectionString = new StringBuilder();
            connectionString.Append(String.Format("server={0};", server));
            connectionString.Append(String.Format("port={0};", port));
            connectionString.Append(String.Format("user={0};", user));
            connectionString.Append(String.Format("password={0};", password));
            connectionString.Append(String.Format("database={0};", database));

            // Pooling settings
            connectionString.Append(String.Format("pooling={0};", true));
            connectionString.Append(String.Format("MinimumPoolSize={0};", 1));
            connectionString.Append(String.Format("maximumpoolsize={0};", 50));

            return connectionString.ToString();
        }

        internal DatabaseClient GetClient()
        {
            return new DatabaseClient(ConnectionString);
        }
    }
}
