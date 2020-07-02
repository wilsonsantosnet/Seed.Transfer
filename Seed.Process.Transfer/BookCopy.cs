using FastMember;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Seed.Process.Transfer
{
    internal class BookCopy<T>
    {
        private readonly string _connectionString;
        private readonly SqlConnection _conn;
        private readonly SqlTransaction _sqlTransaction;

        public BookCopy(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public BookCopy(SqlConnection conn, SqlTransaction sqlTransaction )
        {
            this._conn = conn;
            this._sqlTransaction = sqlTransaction;
        }

        public async Task<bool> Copy(IEnumerable<T> model, string distinationTableName)
        {
            var propertys = typeof(T).GetProperties();
            var storageParameters = GetStorageParameters(propertys);
            try
            {

                if (!string.IsNullOrEmpty(this._connectionString))
                {
                    using (var sqlCopy = new SqlBulkCopy(this._connectionString))
                    {
                        sqlCopy.DestinationTableName = distinationTableName;
                        using (var reader = ObjectReader.Create(model, storageParameters))
                        {
                            await sqlCopy.WriteToServerAsync(reader);
                        }
                    };
                }
                else {

                    var sqlCopy = new SqlBulkCopy(this._conn, SqlBulkCopyOptions.Default, this._sqlTransaction)
                    {
                        DestinationTableName = distinationTableName
                    };

                    using (var reader = ObjectReader.Create(model, storageParameters))
                    {
                        await sqlCopy.WriteToServerAsync(reader);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        protected virtual string[] GetStorageParameters(PropertyInfo[] propertys)
        {
            return propertys.Select(_ => _.Name).ToArray();
        }
    }
}