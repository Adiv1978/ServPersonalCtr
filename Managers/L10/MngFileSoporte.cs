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
        /// Crea el archivo soporte PNG en disco y luego realiza el respaldo en Google Drive.
        /// Retorna el nombre del archivo y su hash.
        /// </summary>
        /// <param name="pngData">Contenido del archivo PNG en bytes.</param>
        /// <returns>DTO con nombre del archivo y hash generado.</returns>
        public async Task<DTOSoporteDoc> Create(byte[] pngData)
        {
            DTOSoporteDoc soporteDoc = _fileSoporteHelper.Create(pngData);

            await _fileSoporteHelper.SetBackup(
                soporteDoc.NombreArchivo,
                pngData
            );

            return soporteDoc;
        }

        /// <summary>
        /// Valida que el hash recibido coincida con el hash SHA256 del archivo almacenado.
        /// </summary>
        /// <param name="hash">Hash esperado del archivo.</param>
        /// <param name="nombreArchivo">Nombre del archivo almacenado.</param>
        /// <returns>True si el hash coincide.</returns>
        public bool ValidateHash(string hash, string nombreArchivo)
        {
            return _fileSoporteHelper.ValidateHash(hash, nombreArchivo);
        }

        /// <summary>
        /// Genera un PDF con el resumen de una licencia y sus imagenes de soporte.
        /// </summary>
        /// <param name="nombresArchivos">Nombres de archivos PNG ya almacenados.</param>
        /// <param name="licencia">Informacion de la licencia.</param>
        /// <returns>PDF generado en memoria.</returns>
        public byte[] BuildLicenciaPdf(List<string>? nombresArchivos, DTOLicencias licencia)
        {
            return _fileSoporteHelper.BuildLicenciaPdf(nombresArchivos, licencia);
        }
    }
}
