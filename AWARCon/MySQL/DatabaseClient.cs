using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace AWARCon.MySQL
{
    sealed class DatabaseClient : IDisposable
    {
        private string _connectionString;

        private MySqlConnection _connection;
        private MySqlCommand _command;

        internal DatabaseClient(string connectionString)
        {
            this._connectionString = connectionString;
            this._connection = new MySqlConnection(_connectionString);

            Connect();
        }

        private void Connect()
        {
            try
            {
                _connection.Open();
                _command = _connection.CreateCommand();
            }
            catch (Exception x)
            {
                Logging.WriteFancyLine(String.Format("MySQL connect error:\r\n{0}", x.ToString()));
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                // Give the connection back to the pool
                _connection.Close();
            }
            if (_command != null)
            {
                _command.Dispose();
            }
        }

        #region Commands
        public void AddParameter(string parameter, object val)
        {
            if (_command.Parameters.Contains(parameter))
                _command.Parameters.RemoveAt(parameter);
            _command.Parameters.AddWithValue(parameter, val);
        }

        public void ExecuteQuery(string query)
        {
            try
            {
                _command.CommandText = query;
                _command.ExecuteScalar();
                _command.CommandText = null;
                _command.Parameters.Clear();
            }
            catch (Exception x) 
            { 
                Logging.WriteFancyLine(x.ToString(), ConsoleColor.Red);
                Logging.LogError("MYSQL EXECUTION ERROR: " + x.ToString());
            }
        }

        public DataSet ReadDataSet(string query)
        {
            try
            {
                DataSet dataSet = new DataSet();
                _command.CommandText = query;

                using (MySqlDataAdapter pAdapter = new MySqlDataAdapter(_command))
                {
                    pAdapter.Fill(dataSet);
                }
                _command.CommandText = null;
                _command.Parameters.Clear();

                return dataSet;
            }
            catch (Exception x) { Logging.WriteFancyLine(x.ToString()); return null; }
        }

        public DataTable ReadDataTable(string query)
        {
            try
            {
                DataTable dataTable = new DataTable();
                _command.CommandText = query;

                using (MySqlDataAdapter pAdapter = new MySqlDataAdapter(_command))
                {
                    pAdapter.Fill(dataTable);
                }
                _command.CommandText = null;
                _command.Parameters.Clear();

                return dataTable;
            }
            catch (Exception x) { Logging.WriteFancyLine(x.ToString()); return null; }
        }

        public DataRow ReadDataRow(string query)
        {
            return ReadDataRow(query, false);
        }

        public DataRow ReadDataRow(string query, bool pThrowException)
        {
            //try
            //{
                DataTable dataTable = ReadDataTable(query);
                if (dataTable != null && dataTable.Rows.Count > 0)
                    return dataTable.Rows[0];

                return null;
            //}
            //catch (Exception x)
            //{
            //    if (pThrowException)
            //        throw;
            //    else
            //    {
            //        Logging.WriteFancyLine(x.ToString());
            //        return null;
            //    }
            //}
        }

        public DataColumn ReadDataColumn(string query)
        {
            //try
            //{
                DataTable dataTable = ReadDataTable(query);
                if (dataTable != null && dataTable.Columns.Count > 0)
                    return dataTable.Columns[0];

                return null;
            //}
            //catch (Exception x) { Logging.WriteFancyLine(x.ToString()); return null; }
        }

        public String ReadString(string query)
        {
            try
            {
                _command.CommandText = query;
                object result = _command.ExecuteScalar();
                _command.CommandText = null;
                _command.Parameters.Clear();

                if (result != null)
                    return result.ToString();
                else
                    return "";
            }
            catch (Exception x)
            {
                Logging.WriteFancyLine(x.ToString(), ConsoleColor.Red);
                Logging.LogError("MYSQL READ ERROR: " + x.ToString());
                return "";
            }
        }

        public Int32 ReadInt32(string query)
        {
            //try
            //{
                _command.CommandText = query;
                //object o = _command.ExecuteScalar();
                object result = _command.ExecuteScalar();
                _command.CommandText = null;
                _command.Parameters.Clear();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            //}
            //catch { return 0; }
        }

        public bool Exists(string query)
        {
            bool found = false;
            //try
            //{
                _command.CommandText = query;
                MySqlDataReader dReader = _command.ExecuteReader();
                found = dReader.HasRows;
                dReader.Close();
                _command.CommandText = null;
                _command.Parameters.Clear();

            //}
            //catch (Exception ex) { Logging.WriteFancyLine(ex.Message + "\n(^^" + query + "^^)"); }
            return found;
        }
        #endregion
    }
}
