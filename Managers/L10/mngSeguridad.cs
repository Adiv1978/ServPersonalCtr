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

        public DTOSession SetSeccion(string nick, string pass, int minutos)
        {
            minutos = ObtenerMinutosCaducidadSession();

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

        public DTOSession SetSeccion(string nick, string pass)
        {
            return SetSeccion(nick, pass, 0);
        }

        public DTOSession ValidateSeccion(string token, int minutos, short rolLevel)
        {
            minutos = ObtenerMinutosCaducidadSession();

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

        public DTOSession ValidateSeccion(string token, short rolLevel)
        {
            return ValidateSeccion(token, 0, rolLevel);
        }

        public bool UpdatePasswordUser(string token, int minutos, string nick, string passActual, string passNuevo)
        {
            minutos = ObtenerMinutosCaducidadSession();

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

        public bool UpdatePasswordUser(string token, string nick, string passActual, string passNuevo)
        {
            return UpdatePasswordUser(token, 0, nick, passActual, passNuevo);
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
