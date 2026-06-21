using System.Security.Cryptography;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L10
{
    public class FileSoporteHelper
    {
        private readonly IConfiguration _configuration;

        public FileSoporteHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Crea un archivo de soporte documental en disco, genera su hash SHA256
        /// y retorna el nombre del archivo junto con el hash generado.
        /// </summary>
        /// <param name="pdfData">Bytes del archivo a guardar.</param>
        /// <returns>DTO con nombre del archivo y hash SHA256.</returns>
        public DTOSoporteDoc Create(byte[] pdfData)
        {
            if (pdfData == null || pdfData.Length == 0)
                throw new ArgumentException("Debe especificar los datos del archivo.", nameof(pdfData));

            string rutaSoporteDoc = _configuration.GetValue<string>("rutadoportedoc") ?? "\\filesoporte\\";

            if (string.IsNullOrWhiteSpace(rutaSoporteDoc))
                rutaSoporteDoc = "\\filesoporte\\";

            if (!Directory.Exists(rutaSoporteDoc))
                Directory.CreateDirectory(rutaSoporteDoc);

            string nombreArchivo = $"SF_{DateTime.Now:ddMMyyyy_HHmmss}.png";
            string rutaCompleta = Path.Combine(rutaSoporteDoc, nombreArchivo);

            string hashFile = GenerarHashSha256(pdfData);

            File.WriteAllBytes(rutaCompleta, pdfData);

            return new DTOSoporteDoc
            {
                NombreArchivo = nombreArchivo,
                HashFile = hashFile
            };
        }

        private static string GenerarHashSha256(byte[] data)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(data);
            return Convert.ToHexString(hashBytes);
        }
    }
}
