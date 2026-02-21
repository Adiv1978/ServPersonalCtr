namespace ServPersonalCtr.Types
{
    public class DTOSession
    {
        public string Token { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public short Rol { get; set; }
    }
}
