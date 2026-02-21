using System.Data;
using Npgsql;
using ServPersonalCtr.Managers.L00;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L10
{
    public class mngSeguridad
    {
        private readonly DBGenericManager _dbManager;

        public mngSeguridad(DBGenericManager dbManager)
        {
            _dbManager = dbManager;
        }

        /// <summary>
        /// Realiza el login y genera una nueva sesión.
        /// </summary>
        public DTOSession SetSeccion(string nick, string pass, int minutos)
        {
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
        public DTOSession ValidateSeccion(string token, int minutos, short rolLevel)
        {
            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", token),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos),
                new NpgsqlParameter("p_rollevel", rolLevel)
            };

            // La función de DB lanza excepciones (RAISE EXCEPTION) que L00 captura y relanza.
            DataTable dt = _dbManager.ExecuteFunctionDataTable("fn_validateseccion", parameters);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                return new DTOSession
                {
                    Token = token, // Devolvemos el mismo token validado
                    UsuarioId = Convert.ToInt32(row["usuarioid"]),
                    Rol = Convert.ToInt16(row["rol"])
                };
            }

            return null;
        }
    }
}
