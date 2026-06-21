using System;

namespace ServPersonalCtr.Types
{
    public class DTOLicencias
    {
        public int LicenciaId { get; set; }
        public string NoLicencia { get; set; } = string.Empty;
        public int IdPersona { get; set; }
        public string EmpleadoCedula { get; set; } = string.Empty;
        public string EmpleadoNombreCompleto { get; set; } = string.Empty;
        public string PuestoTrabajo { get; set; } = string.Empty;
        public DateTime FecLicenciaIni { get; set; }
        public DateTime FecLicenciaFin { get; set; }
        public int TiempoLicencia { get; set; }
        public int DiaFaltantes { get; set; }
        public string Diagnostico { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
        public bool Auditoria { get; set; }
        public DateTime FechaRegistroSistema { get; set; }
        public int RegistradoPorId { get; set; }
        public string RegistradoPorNick { get; set; } = string.Empty;
    }
}
