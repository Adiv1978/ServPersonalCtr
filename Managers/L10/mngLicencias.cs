using Npgsql;
using ServPersonalCtr.Managers.L00;
using ServPersonalCtr.Types;
using System.Data;
using System.Text.Json;

namespace ServPersonalCtr.Managers.L10
{
    public class mngLicencias
    {
        private readonly DBGenericManager _dbManager;
        private readonly IConfiguration _configuration;

        public mngLicencias(DBGenericManager dbManager, IConfiguration configuration)
        {
            _dbManager = dbManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Registra una nueva licencia médica.
        /// Retorna el ID autogenerado por la base de datos.
        /// </summary>
        public int SetLicencias(
            string token,
            DTOLicencias licencia,
            List<DTOSoporteDoc>? soporteDoc = null)
        {
            if (licencia == null)
                throw new ArgumentNullException(nameof(licencia));

            int minutos = ObtenerMinutosCaducidadSession();
            soporteDoc ??= new List<DTOSoporteDoc>();

            var soportesDb = soporteDoc
                .Where(s =>
                    s != null &&
                    !string.IsNullOrWhiteSpace(s.NombreArchivo) &&
                    !string.IsNullOrWhiteSpace(s.HashFile))
                .Select(s => new
                {
                    nombrearchivo = s.NombreArchivo,
                    hashfile = s.HashFile
                })
                .ToList();

            string soportesJson = JsonSerializer.Serialize(soportesDb);

            var parameters = new List<NpgsqlParameter>{
                new NpgsqlParameter("p_tokenid", NpgsqlTypes.NpgsqlDbType.Text){
                    Value = token
                },
                new NpgsqlParameter("p_minutoscaducaseccion", NpgsqlTypes.NpgsqlDbType.Integer){
                    Value = minutos
                },
                new NpgsqlParameter("p_idpersona", NpgsqlTypes.NpgsqlDbType.Integer){
                    Value = licencia.IdPersona
                },
                new NpgsqlParameter("p_feclicenciaini", NpgsqlTypes.NpgsqlDbType.Date){
                    Value = licencia.FecLicenciaIni
                },
                new NpgsqlParameter("p_feclicenciafin", NpgsqlTypes.NpgsqlDbType.Date){
                    Value = licencia.FecLicenciaFin
                },
                new NpgsqlParameter("p_diagnostico", NpgsqlTypes.NpgsqlDbType.Text){
                    Value = string.IsNullOrWhiteSpace(licencia.Diagnostico)
                        ? DBNull.Value
                        : licencia.Diagnostico
                },
                new NpgsqlParameter("p_nolicencia", NpgsqlTypes.NpgsqlDbType.Varchar){
                    Value = string.IsNullOrWhiteSpace(licencia.NoLicencia)
                        ? DBNull.Value
                        : licencia.NoLicencia
                },
                new NpgsqlParameter("p_auditoria", NpgsqlTypes.NpgsqlDbType.Boolean){
                    Value = licencia.Auditoria
                },
                new NpgsqlParameter("p_observacion", NpgsqlTypes.NpgsqlDbType.Text){
                    Value = string.IsNullOrWhiteSpace(licencia.Observacion)
                        ? DBNull.Value
                        : licencia.Observacion
                },
                new NpgsqlParameter("p_soportes", NpgsqlTypes.NpgsqlDbType.Jsonb){
                    Value = soportesJson
                }
            };

            object result = _dbManager.ExecuteFunctionScalar("fn_setlicencias", parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Obtiene el listado de licencias aplicando filtros dinámicos.
        /// </summary>
        public List<DTOLicencias> GetLicencias(string token, int idPersona = 0,
                                              DateTime? fecIni = null, DateTime? fecFin = null,
                                              DateTime? regDesde = null, DateTime? regHasta = null)
        {
            int minutos = ObtenerMinutosCaducidadSession();
            var list = new List<DTOLicencias>();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", token),
                new NpgsqlParameter("p_minutoscaducaseccion", minutos),
                new NpgsqlParameter("p_idpersona", idPersona),
                new NpgsqlParameter("p_fecinicio", NpgsqlTypes.NpgsqlDbType.Date) { Value = fecIni ?? (object)DBNull.Value },
                new NpgsqlParameter("p_fecfin", NpgsqlTypes.NpgsqlDbType.Date) { Value = fecFin ?? (object)DBNull.Value },
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
                    DiaFaltantes = Convert.ToInt32(row["diafaltantes"]),
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

        /// <summary>
        /// Obtiene licencias activas con paginación usando fn_getlicenciasactivas.
        /// </summary>
        public List<DTOLicencias> GetLicenciasActivas(int numeroPagina, int tamanioPagina)
        {
            var list = new List<DTOLicencias>();
            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_numero_pagina", numeroPagina),
                new NpgsqlParameter("p_registros_por_pagina", tamanioPagina)
            };

            DataTable dt = _dbManager.ExecuteFunctionDataTable("fn_getlicenciasactivas", parameters);

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
                    DiaFaltantes = Convert.ToInt32(row["diafaltantes"]),
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

        private int ObtenerMinutosCaducidadSession()
        {
            int minutos = _configuration.GetValue<int>("MinutoCaducidadSession");

            if (minutos <= 0)
                minutos = 60;

            return minutos;
        }
    }
}
