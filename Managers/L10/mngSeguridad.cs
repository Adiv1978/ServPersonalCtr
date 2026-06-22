using System.Data;
using Npgsql;
using ServPersonalCtr.Managers.L00;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L10
{
    public class mngSeguridad
    {
        private readonly DBGenericManager _dbManager;
        private readonly IConfiguration _configuration;

        public mngSeguridad(DBGenericManager dbManager, IConfiguration configuration)
        {
            _dbManager = dbManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Realiza el login y genera una nueva sesión.
        /// </summary>
        public DTOSession SetSeccion(string nick, string pass)
        {
            int minutos = ObtenerMinutosCaducidadSession();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_nick", nick),
                new NpgsqlParameter("p_pass", pass),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos)
            };

            DataTable dt = _dbManager.ExecuteFunctionDataTable("fn_setseccion", parameters);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                return new DTOSession
                {
                    Token = row["token"].ToString() ?? string.Empty,
                    UsuarioId = Convert.ToInt32(row["usuarioid"]),
                    Rol = Convert.ToInt16(row["rol"])
                };
            }

            return null;
        }

        /// <summary>
        /// Valida si una sesión es vigente y si el usuario tiene el nivel de rol requerido.
        /// </summary>
        public DTOSession ValidateSeccion(string token, short rolLevel)
        {
            int minutos = ObtenerMinutosCaducidadSession();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", token),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos),
                new NpgsqlParameter("p_rollevel", rolLevel)
            };

            DataTable dt = _dbManager.ExecuteFunctionDataTable("fn_validateseccion", parameters);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                return new DTOSession
                {
                    Token = token,
                    UsuarioId = Convert.ToInt32(row["usuarioid"]),
                    Rol = Convert.ToInt16(row["rol"])
                };
            }

            return null;
        }

        /// <summary>
        /// Actualiza la contraseña del usuario verificando sus credenciales actuales.
        /// </summary>
        public bool UpdatePasswordUser(string token, string nick, string passActual, string passNuevo)
        {
            int minutos = ObtenerMinutosCaducidadSession();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", token),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos),
                new NpgsqlParameter("p_nick", nick),
                new NpgsqlParameter("p_pass_actual", passActual),
                new NpgsqlParameter("p_pass_nuevo", passNuevo)
            };

            object result = _dbManager.ExecuteFunctionScalar("fn_updatepassworduser", parameters);
            return result != null && Convert.ToBoolean(result);
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
