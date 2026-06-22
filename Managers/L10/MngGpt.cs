using ServPersonalCtr.Managers.L00;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L10
{
    public class MngGpt
    {
        private readonly GptHelper _gptHelper;

        public MngGpt(GptHelper gptHelper)
        {
            _gptHelper = gptHelper;
        }

        /// <summary>
        /// Analiza un documento PDF de licencia médica consumiendo la capa L00.
        /// </summary>
        /// <param name="pdfData">Contenido del PDF en bytes.</param>
        /// <param name="nombreArchivo">Nombre del archivo PDF.</param>
        /// <returns>Datos extraídos del documento.</returns>
        public async Task<GPT_Licencias> AnalizarLicenciaPdfAsync(byte[] pdfData, string nombreArchivo = "licencia.pdf")
        {
            return await _gptHelper.AnalizarLicenciaPdfAsync(pdfData, nombreArchivo);
        }
    }
}
