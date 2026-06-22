using System;

namespace ServPersonalCtr.Types
{
    public class GPT_Licencias
    {
        public string EmpleadoCedula { get; set; } = string.Empty;
        public string EmpleadoNombreCompleto { get; set; } = string.Empty;
        public DateTime FecLicenciaIni { get; set; }
        public DateTime FecLicenciaFin { get; set; }
        public int TiempoLicencia { get; set; }
        public string Diagnostico { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }
}
