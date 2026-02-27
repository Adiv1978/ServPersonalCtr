using System.Data;
using Npgsql;

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
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text; // Cambiado a Text

                    string paramPlaceholders = "";
                    if (parameters != null && parameters.Count > 0)
                    {
                        // Aseguramos que los nombres de los parámetros tengan el prefijo '@'
                        var paramNames = parameters.Select(p => p.ParameterName.StartsWith("@") ? p.ParameterName : "@" + p.ParameterName);
                        paramPlaceholders = string.Join(", ", paramNames);

                        cmd.Parameters.AddRange(parameters.ToArray());
                    }

                    // Armamos la consulta nativa de PostgreSQL
                    cmd.CommandText = $"SELECT * FROM {functionName}({paramPlaceholders})";

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
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text; // Cambiado a Text

                    string paramPlaceholders = "";
                    if (parameters != null && parameters.Count > 0)
                    {
                        var paramNames = parameters.Select(p => p.ParameterName.StartsWith("@") ? p.ParameterName : "@" + p.ParameterName);
                        paramPlaceholders = string.Join(", ", paramNames);

                        cmd.Parameters.AddRange(parameters.ToArray());
                    }

                    cmd.CommandText = $"SELECT * FROM {functionName}({paramPlaceholders})";

                    conn.Open();
                    result = cmd.ExecuteScalar();
                }
            }
            return result;
        }
    }
}