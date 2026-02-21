namespace ServPersonalCtr.Types
{
    public class DTOPersonal
    {
        public int Id { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string PuestoTrabajo { get; set; } = string.Empty;
    }
}
