namespace ServPersonalCtr.Types
{
    public class UpdatePasswordRequest
    {
        public string Token { get; set; } = string.Empty;
        public int Minutos { get; set; }
        public string Nick { get; set; } = string.Empty;
        public string PassActual { get; set; } = string.Empty;
        public string PassNuevo { get; set; } = string.Empty;
    }
}
