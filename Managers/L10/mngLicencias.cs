using System.Data;
using Npgsql;
using ServPersonalCtr.Managers.L00;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L10
{
    public class mngLicencias
    {
        private readonly DBGenericManager _dbManager;

        public mngLicencias(DBGenericManager dbManager)
        {
            _dbManager = dbManager;
        }

        /// <summary>
        /// Registra una nueva licencia médica.
        /// Retorna el ID autogenerado por la base de datos.
        /// </summary>
        public int SetLicencias(string token, int minutos, DTOLicencias licencia)
        {
            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", token),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos),
                new NpgsqlParameter("p_idpersona", licencia.IdPersona),
                
                // Forzamos explícitamente a que Npgsql las envíe como Date (sin hora)
                new NpgsqlParameter("p_feclicenciaini", NpgsqlTypes.NpgsqlDbType.Date) { Value = licencia.FecLicenciaIni },
                new NpgsqlParameter("p_feclicenciafin", NpgsqlTypes.NpgsqlDbType.Date) { Value = licencia.FecLicenciaFin },

                new NpgsqlParameter("p_diagnostico", licencia.Diagnostico),
                new NpgsqlParameter("p_nolicencia", licencia.NoLicencia),
                new NpgsqlParameter("p_auditoria", licencia.Auditoria),
                new NpgsqlParameter("p_observacion", licencia.Observacion ?? (object)DBNull.Value)
            };

            // Usamos ExecuteFunctionScalar porque la función retorna un valor único (ID)
            object result = _dbManager.ExecuteFunctionScalar("fn_setlicencias", parameters);
            return Convert.ToInt32(result);
        }
        /// <summary>
        /// Obtiene el listado de licencias aplicando filtros dinámicos.
        /// </summary>
        public List<DTOLicencias> GetLicencias(string token, int minutos, int idPersona = 0,
                                              DateTime? fecIni = null, DateTime? fecFin = null,
                                              DateTime? regDesde = null, DateTime? regHasta = null)
        {
            var list = new List<DTOLicencias>();
            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", token),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos),
                new NpgsqlParameter("p_idpersona", idPersona),
                
                // Especificamos explícitamente el tipo Date para las fechas de licencia
                new NpgsqlParameter("p_fecinicio", NpgsqlTypes.NpgsqlDbType.Date) { Value = fecIni ?? (object)DBNull.Value },
                new NpgsqlParameter("p_fecfin", NpgsqlTypes.NpgsqlDbType.Date) { Value = fecFin ?? (object)DBNull.Value },
                
                // Especificamos el tipo para las fechas de registro 
                // (Nota: Si tu función en Postgres espera 'date' en lugar de 'timestamp' para estos filtros, cambia NpgsqlDbType.Timestamp por NpgsqlDbType.Date)
                new NpgsqlParameter("p_fecregdesde", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = regDesde ?? (object)DBNull.Value },
                new NpgsqlParameter("p_fecreghasta", NpgsqlTypes.NpgsqlDbType.Timestamp) { Value = regHasta ?? (object)DBNull.Value }
            };

            DataTable dt = _dbManager.ExecuteFunctionDataTable("fn_getlicencia", parameters);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new DTOLicencias
                {
                    LicenciaId = Convert.ToInt32(row["licencia_id"]),
                    NoLicencia = row["nolicencia"].ToString() ?? string.Empty,
                    IdPersona = Convert.ToInt32(row["idpersona"]),
                    EmpleadoCedula = row["empleado_cedula"].ToString() ?? string.Empty,
                    EmpleadoNombreCompleto = row["empleado_nombre_completo"].ToString() ?? string.Empty,
                    PuestoTrabajo = row["puestotrabajo"].ToString() ?? string.Empty,
                    FecLicenciaIni = row["feclicenciaini"] is DateOnly dIni
                                     ? dIni.ToDateTime(TimeOnly.MinValue)
                                     : Convert.ToDateTime(row["feclicenciaini"]),
                    FecLicenciaFin = row["feclicenciafin"] is DateOnly dFin
                                     ? dFin.ToDateTime(TimeOnly.MinValue)
                                     : Convert.ToDateTime(row["feclicenciafin"]),
                    TiempoLicencia = Convert.ToInt32(row["tiempolicencia"]),
                    Diagnostico = row["diagnostico"].ToString() ?? string.Empty,
                    Observacion = row["observacion"].ToString() ?? string.Empty,
                    Auditoria = Convert.ToBoolean(row["auditoria"]),
                    FechaRegistroSistema = row["fecha_registro_sistema"] is DateOnly dReg
                                           ? dReg.ToDateTime(TimeOnly.MinValue)
                                           : Convert.ToDateTime(row["fecha_registro_sistema"]),
                    RegistradoPorId = Convert.ToInt32(row["registrado_por_id"]),
                    RegistradoPorNick = row["registrado_por_nick"].ToString() ?? string.Empty
                });
            }
            return list;
        }
    }
}
