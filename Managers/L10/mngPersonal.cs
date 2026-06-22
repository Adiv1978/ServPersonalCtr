using System.Data;
using Npgsql;
using ServPersonalCtr.Managers.L00;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L10
{
    public class mngPersonal
    {
        private readonly DBGenericManager _dbManager;
        private readonly IConfiguration _configuration;

        public mngPersonal(DBGenericManager dbManager, IConfiguration configuration)
        {
            _dbManager = dbManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Registra o actualiza un empleado.
        /// Requiere Rol 1 validado en la base de datos.
        /// </summary>
        public int SetPersonal(string token, DTOPersonal personal)
        {
            int minutos = ObtenerMinutosCaducidadSession();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", token),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos),
                new NpgsqlParameter("p_id", personal.Id),
                new NpgsqlParameter("p_cedula", personal.Cedula),
                new NpgsqlParameter("p_nombre", personal.Nombre),
                new NpgsqlParameter("p_apellidos", personal.Apellidos),
                new NpgsqlParameter("p_puestotrabajo", personal.PuestoTrabajo)
            };

            object result = _dbManager.ExecuteFunctionScalar("fn_setpersonal", parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Obtiene el listado de personal filtrado.
        /// </summary>
        public List<DTOPersonal> GetPersonal(string token, int id = 0, string busqueda = "")
        {
            int minutos = ObtenerMinutosCaducidadSession();
            var personalList = new List<DTOPersonal>();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", token),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos),
                new NpgsqlParameter("p_id", id),
                new NpgsqlParameter("p_busqueda", busqueda ?? (object)DBNull.Value)
            };

            DataTable dt = _dbManager.ExecuteFunctionDataTable("fn_getpersona", parameters);

            foreach (DataRow row in dt.Rows)
            {
                personalList.Add(new DTOPersonal
                {
                    Id = Convert.ToInt32(row["id"]),
                    Cedula = row["cedula"].ToString() ?? string.Empty,
                    Nombre = row["nombre"].ToString() ?? string.Empty,
                    Apellidos = row["apellidos"].ToString() ?? string.Empty,
                    NombreCompleto = row["nombre_completo"].ToString() ?? string.Empty,
                    PuestoTrabajo = row["puestotrabajo"].ToString() ?? string.Empty
                });
            }

            return personalList;
        }

        private int ObtenerMinutosCaducidadSession()
        {
            int minutos = _configuration.GetValue<int>("MinutoCaducidadSession");

            if (minutos <= 0)
                minutos = 60;

            return minutos;
        }
    }
}
