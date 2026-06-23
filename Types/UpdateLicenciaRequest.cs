namespace ServPersonalCtr.Types
{
    public class UpdateLicenciaRequest
    {
        public string TokenId { get; set; } = string.Empty;
        public int IdLicencia { get; set; }
        public DateTime FecLicenciaIni { get; set; }
        public DateTime FecLicenciaFin { get; set; }
        public string Diagnostico { get; set; } = string.Empty;
        public bool Auditoria { get; set; }
        public string Observacion { get; set; } = string.Empty;
    }
}
