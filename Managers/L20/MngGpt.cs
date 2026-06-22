using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L20
{
    public class MngGpt
    {
        private readonly L10.MngGpt _mngGptL10;

        public MngGpt(L10.MngGpt mngGptL10)
        {
            _mngGptL10 = mngGptL10;
        }

        /// <summary>
        /// Analiza un documento PDF de licencia médica consumiendo la capa L10.
        /// </summary>
        public async Task<GPT_Licencias> AnalizarLicenciaPdfAsync(byte[] pdfData, string nombreArchivo = "licencia.pdf")
        {
            return await _mngGptL10.AnalizarLicenciaPdfAsync(pdfData, nombreArchivo);
        }
    }
}
