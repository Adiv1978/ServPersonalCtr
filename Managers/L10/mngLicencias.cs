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
        /// Actualiza una licencia medica existente.
        /// </summary>
        public bool UpdateLicencia(DTOUpdateLicenciaRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            int minutos = ObtenerMinutosCaducidadSession();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = request.Token
                },
                new NpgsqlParameter("p_minutoscaducaseccion", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = minutos
                },
                new NpgsqlParameter("p_idlicencia", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = request.IdLicencia
                },
                new NpgsqlParameter("p_feclicenciaini", NpgsqlTypes.NpgsqlDbType.Date)
                {
                    Value = request.FecLicenciaIni
                },
                new NpgsqlParameter("p_feclicenciafin", NpgsqlTypes.NpgsqlDbType.Date)
                {
                    Value = request.FecLicenciaFin
                },
                new NpgsqlParameter("p_diagnostico", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = string.IsNullOrWhiteSpace(request.Diagnostico)
                        ? DBNull.Value
                        : request.Diagnostico
                },
                new NpgsqlParameter("p_auditoria", NpgsqlTypes.NpgsqlDbType.Boolean)
                {
                    Value = request.Auditoria
                },
                new NpgsqlParameter("p_observacion", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = string.IsNullOrWhiteSpace(request.Observacion)
                        ? DBNull.Value
                        : request.Observacion
                }
            };

            object updateResult = _dbManager.ExecuteFunctionScalar("fn_updatelicencia", parameters);
            return updateResult != null && Convert.ToBoolean(updateResult);
        }

        /// <summary>
        /// Obtiene el listado de licencias aplicando filtros dinámicos.
        /// </summary>
        public List<DTOSoporteDoc> GetLicenciaSoporteDoc(GetSoporteDocRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            int minutos = ObtenerMinutosCaducidadSession();
            var list = new List<DTOSoporteDoc>();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = request.Token
                },
                new NpgsqlParameter("p_minutoscaducaseccion", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = minutos
                },
                new NpgsqlParameter("p_idlicencia", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = request.IdLicencia
                }
            };

            DataTable dt = _dbManager.ExecuteFunctionDataTable("fn_getsoportedoc", parameters);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new DTOSoporteDoc
                {
                    NombreArchivo = row["nombrearchivo"].ToString() ?? string.Empty,
                    HashFile = row["hashfile"].ToString() ?? string.Empty
                });
            }

            return list;
        }

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
                list.Add(ConvertirLicencia(row));
            }

            return list;
        }

        /// <summary>
        /// Obtiene licencias cuya fecha de inicio pertenece al intervalo
        /// [FecIniA, FecIniB), usando fn_getlicenciaini.
        /// </summary>
        public List<DTOLicencias> GetLicenciaIni(DTOGetLicenciaIniRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.FecIniA == default || request.FecIniB == default)
                throw new ArgumentException("Las fechas inicial y final son requeridas.", nameof(request));

            if (request.FecIniB.Date <= request.FecIniA.Date)
                throw new ArgumentException("La fecha final debe ser mayor que la fecha inicial.", nameof(request));

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_fecini_a", NpgsqlTypes.NpgsqlDbType.Date)
                {
                    Value = request.FecIniA.Date
                },
                new NpgsqlParameter("p_fecini_b", NpgsqlTypes.NpgsqlDbType.Date)
                {
                    Value = request.FecIniB.Date
                }
            };

            DataTable dt = _dbManager.ExecuteFunctionDataTable("fn_getlicenciaini", parameters);
            var list = new List<DTOLicencias>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(ConvertirLicencia(row));
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
                list.Add(ConvertirLicencia(row));
            }

            return list;
        }

        /// <summary>
        /// Actualiza los datos principales de una licencia existente.
        /// Los minutos de sesión se obtienen desde appsettings.
        /// </summary>
        public bool UpdateLicencia(UpdateLicenciaRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            int minutos = ObtenerMinutosCaducidadSession();

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("p_tokenid", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = request.TokenId
                },
                new NpgsqlParameter("p_minutoscaducaseccion", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = minutos
                },
                new NpgsqlParameter("p_idlicencia", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = request.IdLicencia
                },
                new NpgsqlParameter("p_feclicenciaini", NpgsqlTypes.NpgsqlDbType.Date)
                {
                    Value = request.FecLicenciaIni
                },
                new NpgsqlParameter("p_feclicenciafin", NpgsqlTypes.NpgsqlDbType.Date)
                {
                    Value = request.FecLicenciaFin
                },
                new NpgsqlParameter("p_diagnostico", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = string.IsNullOrWhiteSpace(request.Diagnostico)
                        ? DBNull.Value
                        : request.Diagnostico
                },
                new NpgsqlParameter("p_auditoria", NpgsqlTypes.NpgsqlDbType.Boolean)
                {
                    Value = request.Auditoria
                },
                new NpgsqlParameter("p_observacion", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Value = string.IsNullOrWhiteSpace(request.Observacion)
                        ? DBNull.Value
                        : request.Observacion
                }
            };

            object result = _dbManager.ExecuteFunctionScalar("fn_updatelicencia", parameters);
            return Convert.ToBoolean(result);
        }

        private int ObtenerMinutosCaducidadSession()
        {
            int minutos = _configuration.GetValue<int>("MinutoCaducidadSession");

            if (minutos <= 0)
                minutos = 60;

            return minutos;
        }

        private static DTOLicencias ConvertirLicencia(DataRow row)
        {
            return new DTOLicencias
            {
                LicenciaId = Convert.ToInt32(row["licencia_id"]),
                NoLicencia = row["nolicencia"].ToString() ?? string.Empty,
                IdPersona = Convert.ToInt32(row["idpersona"]),
                EmpleadoCedula = row["empleado_cedula"].ToString() ?? string.Empty,
                EmpleadoNombreCompleto = row["empleado_nombre_completo"].ToString() ?? string.Empty,
                PuestoTrabajo = row["puestotrabajo"].ToString() ?? string.Empty,
                FecLicenciaIni = ConvertirFecha(row["feclicenciaini"]),
                FecLicenciaFin = ConvertirFecha(row["feclicenciafin"]),
                TiempoLicencia = Convert.ToInt32(row["tiempolicencia"]),
                DiaFaltantes = Convert.ToInt32(row["diafaltantes"]),
                Diagnostico = row["diagnostico"].ToString() ?? string.Empty,
                Observacion = row["observacion"].ToString() ?? string.Empty,
                Auditoria = Convert.ToBoolean(row["auditoria"]),
                FechaRegistroSistema = ConvertirFecha(row["fecha_registro_sistema"]),
                RegistradoPorId = Convert.ToInt32(row["registrado_por_id"]),
                RegistradoPorNick = row["registrado_por_nick"].ToString() ?? string.Empty
            };
        }

        private static DateTime ConvertirFecha(object valor)
        {
            return valor is DateOnly fecha
                ? fecha.ToDateTime(TimeOnly.MinValue)
                : Convert.ToDateTime(valor);
        }
    }
}
