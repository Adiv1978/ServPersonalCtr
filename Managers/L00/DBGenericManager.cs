using System.Data;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace ServPersonalCtr.Managers.L00
{
    public class DBGenericManager
    {
        private readonly string _connectionString;

        public DBGenericManager(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgresConnection");
        }

        /// <summary>
        /// Para funciones que retornan tablas o múltiples filas (SETOF, TABLE).
        /// </summary>
        public DataTable ExecuteFunctionDataTable(string functionName, List<NpgsqlParameter> parameters)
        {
            DataTable dt = new DataTable();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = new NpgsqlCommand(functionName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (parameters != null) cmd.Parameters.AddRange(parameters.ToArray());

                    conn.Open();
                    using (var da = new NpgsqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Para funciones que retornan un único valor (INT, BOOLEAN, TEXT).
        /// </summary>
        public object ExecuteFunctionScalar(string functionName, List<NpgsqlParameter> parameters)
        {
            object result = null;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                using (var cmd = new NpgsqlCommand(functionName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (parameters != null) cmd.Parameters.AddRange(parameters.ToArray());

                    conn.Open();
                    result = cmd.ExecuteScalar();
                }
            }
            return result;
        }
    }
}
