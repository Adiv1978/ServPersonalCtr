using System.Security.Cryptography;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L00
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

            string rutaSoporteDoc = ObtenerRutaSoporteDoc();

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

        /// <summary>
        /// Valida que el hash recibido coincida con el hash SHA256 del archivo almacenado.
        /// </summary>
        /// <param name="hash">Hash esperado del archivo.</param>
        /// <param name="nombreArchivo">Nombre del archivo almacenado.</param>
        /// <returns>True si el hash coincide.</returns>
        public bool ValidateHash(string hash, string nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Debe especificar el hash del archivo.", nameof(hash));

            if (string.IsNullOrWhiteSpace(nombreArchivo))
                throw new ArgumentException("Debe especificar el nombre del archivo.", nameof(nombreArchivo));

            string rutaSoporteDoc = ObtenerRutaSoporteDoc();
            string rutaCompleta = Path.Combine(rutaSoporteDoc, nombreArchivo);

            if (!File.Exists(rutaCompleta))
                throw new FileNotFoundException($"Archivo no encontrado: {nombreArchivo}", rutaCompleta);

            byte[] contenidoArchivo = File.ReadAllBytes(rutaCompleta);
            string hashCalculado = GenerarHashSha256(contenidoArchivo);

            if (!string.Equals(hashCalculado, hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"El hash del archivo {nombreArchivo} no coincide con el hash especificado.");

            return true;
        }

        /// <summary>
        /// Guarda una copia de respaldo del archivo PDF en Google Drive.
        /// Retorna el ID del archivo creado en Google Drive.
        /// </summary>
        /// <param name="nombreArchivo">Nombre con el que se guardará el archivo en Google Drive.</param>
        /// <param name="pdfData">Contenido del archivo PDF en bytes.</param>
        /// <returns>ID del archivo creado en Google Drive.</returns>
        public async Task<string> SetBackup(string nombreArchivo, byte[] pdfData)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                throw new ArgumentException("Debe especificar el nombre del archivo.", nameof(nombreArchivo));

            if (pdfData == null || pdfData.Length == 0)
                throw new ArgumentException("Debe especificar el contenido del archivo.", nameof(pdfData));

            string applicationName = _configuration.GetValue<string>("GoogleDrive:ApplicationName") ?? "ServPersonalCtr";
            string folderId = _configuration.GetValue<string>("GoogleDrive:FolderId") ?? string.Empty;
            string clientEmail = _configuration.GetValue<string>("GoogleDrive:ClientEmail") ?? string.Empty;
            string privateKey = _configuration.GetValue<string>("GoogleDrive:PrivateKey") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(clientEmail))
                throw new InvalidOperationException("No se ha configurado GoogleDrive:ClientEmail en appsettings.json.");

            if (string.IsNullOrWhiteSpace(privateKey))
                throw new InvalidOperationException("No se ha configurado GoogleDrive:PrivateKey en appsettings.json.");

            privateKey = privateKey.Replace("\\n", "\n");

            ServiceAccountCredential credential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(clientEmail)
                {
                    Scopes = new[] { DriveService.Scope.DriveFile }
                }.FromPrivateKey(privateKey)
            );

            using DriveService driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = nombreArchivo
            };

            if (!string.IsNullOrWhiteSpace(folderId))
                fileMetadata.Parents = new List<string> { folderId };

            await using MemoryStream stream = new MemoryStream(pdfData);

            FilesResource.CreateMediaUpload request = driveService.Files.Create(
                fileMetadata,
                stream,
                "application/pdf"
            );

            request.Fields = "id, name";

            Google.Apis.Upload.IUploadProgress uploadProgress = await request.UploadAsync();

            if (uploadProgress.Status != Google.Apis.Upload.UploadStatus.Completed)
                throw new InvalidOperationException($"No se pudo subir el archivo a Google Drive. Estado: {uploadProgress.Status}. Error: {uploadProgress.Exception?.Message}");

            return request.ResponseBody?.Id
                ?? throw new InvalidOperationException("Google Drive no retornó el ID del archivo creado.");
        }

        private string ObtenerRutaSoporteDoc()
        {
            string rutaSoporteDoc = _configuration.GetValue<string>("rutadoportedoc") ?? "\\filesoporte\\";

            if (string.IsNullOrWhiteSpace(rutaSoporteDoc))
                rutaSoporteDoc = "\\filesoporte\\";

            return rutaSoporteDoc;
        }

        private static string GenerarHashSha256(byte[] data)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(data);
            return Convert.ToHexString(hashBytes);
        }
    }
}
