using ServPersonalCtr.Managers.L00;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L10
{
    public class MngFileSoporte
    {
        private readonly FileSoporteHelper _fileSoporteHelper;

        public MngFileSoporte(FileSoporteHelper fileSoporteHelper)
        {
            _fileSoporteHelper = fileSoporteHelper;
        }

        /// <summary>
        /// Crea el archivo soporte en disco y luego realiza el respaldo en Google Drive.
        /// Retorna el nombre del archivo y su hash.
        /// </summary>
        /// <param name="pdfData">Contenido del archivo PDF en bytes.</param>
        /// <returns>DTO con nombre del archivo y hash generado.</returns>
        public async Task<DTOSoporteDoc> Create(byte[] pdfData)
        {
            DTOSoporteDoc soporteDoc = _fileSoporteHelper.Create(pdfData);

            await _fileSoporteHelper.SetBackup(
                soporteDoc.NombreArchivo,
                pdfData
            );

            return soporteDoc;
        }
    }
}
