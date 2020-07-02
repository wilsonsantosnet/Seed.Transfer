using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace Seed.Process.Transfer
{
    public class Connection
    {
        private readonly string _connectionString;
        public Connection(string connectionString)
        {
            _connectionString = connectionString;
        }


        public NpgsqlConnection GetInstancePG() {
            return new NpgsqlConnection(this._connectionString);
        }

        public SqlConnection GetInstanceSql()
        {
            return new SqlConnection(this._connectionString);
        }

    }
}
