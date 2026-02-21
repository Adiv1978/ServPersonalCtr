using System;

namespace ServPersonalCtr.Types
{
    public class DTOLicencias
    {
        // Datos de la Licencia
        public int LicenciaId { get; set; }
        public string NoLicencia { get; set; } = string.Empty;
        public int IdPersona { get; set; }

        // Datos del Empleado (Desde vw_licencias / Personal)
        public string EmpleadoCedula { get; set; } = string.Empty;
        public string EmpleadoNombreCompleto { get; set; } = string.Empty;
        public string PuestoTrabajo { get; set; } = string.Empty;

        // Fechas y Tiempos
        public DateTime FecLicenciaIni { get; set; }
        public DateTime FecLicenciaFin { get; set; }
        public int TiempoLicencia { get; set; }

        // Detalles Médicos y Auditoría
        public string Diagnostico { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public bool Auditoria { get; set; }

        // Datos de Registro (Desde vw_licencias / User)
        public DateTime FechaRegistroSistema { get; set; }
        public int RegistradoPorId { get; set; }
        public string RegistradoPorNick { get; set; } = string.Empty;
    }
}
