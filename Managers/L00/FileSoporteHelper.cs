using System.Security.Cryptography;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L00
{
    public class FileSoporteHelper
    {
        private readonly IConfiguration _configuration;

        public FileSoporteHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Crea un archivo de soporte documental PNG en disco, genera su hash SHA256
        /// y retorna el nombre del archivo junto con el hash generado.
        /// </summary>
        /// <param name="pngData">Bytes del archivo PNG a guardar.</param>
        /// <returns>DTO con nombre del archivo y hash SHA256.</returns>
        public DTOSoporteDoc Create(byte[] pngData)
        {
            if (pngData == null || pngData.Length == 0)
                throw new ArgumentException("Debe especificar los datos del archivo.", nameof(pngData));

            string rutaSoporteDoc = ObtenerRutaSoporteDoc();

            if (!Directory.Exists(rutaSoporteDoc))
                Directory.CreateDirectory(rutaSoporteDoc);

            string nombreArchivo = $"SF_{DateTime.Now:ddMMyyyy_HHmmss_fff}.png";
            string rutaCompleta = Path.Combine(rutaSoporteDoc, nombreArchivo);

            string hashFile = GenerarHashSha256(pngData);

            File.WriteAllBytes(rutaCompleta, pngData);

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

            string rutaCompleta = ObtenerRutaCompletaArchivo(nombreArchivo);

            if (!File.Exists(rutaCompleta))
                throw new FileNotFoundException($"Archivo no encontrado: {nombreArchivo}", rutaCompleta);

            byte[] contenidoArchivo = File.ReadAllBytes(rutaCompleta);
            string hashCalculado = GenerarHashSha256(contenidoArchivo);

            if (!string.Equals(hashCalculado, hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"El hash del archivo {nombreArchivo} no coincide con el hash especificado.");

            return true;
        }

        /// <summary>
        /// Genera un PDF con la informacion de la licencia en la primera pagina
        /// y adjunta una imagen PNG por cada pagina adicional.
        /// </summary>
        /// <param name="nombresArchivos">Lista de nombres de archivos PNG almacenados.</param>
        /// <param name="licencia">Informacion de la licencia a incluir en el PDF.</param>
        /// <returns>PDF generado en memoria.</returns>
        public byte[] BuildLicenciaPdf(List<string>? nombresArchivos, DTOLicencias licencia)
        {
            if (licencia == null)
                throw new ArgumentNullException(nameof(licencia));

            List<byte[]> imagenes = CargarArchivosSoporte(nombresArchivos);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text("Resumen de Licencia Medica")
                        .FontSize(18)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);
                        column.Item().Text($"No. Licencia: {ValorTexto(licencia.NoLicencia)}");
                        column.Item().Text($"Licencia ID: {licencia.LicenciaId}");
                        column.Item().Text($"Empleado: {ValorTexto(licencia.EmpleadoNombreCompleto)}");
                        column.Item().Text($"Cedula: {ValorTexto(licencia.EmpleadoCedula)}");
                        column.Item().Text($"Puesto de trabajo: {ValorTexto(licencia.PuestoTrabajo)}");
                        column.Item().Text($"Fecha inicio: {FormatearFecha(licencia.FecLicenciaIni)}");
                        column.Item().Text($"Fecha fin: {FormatearFecha(licencia.FecLicenciaFin)}");
                        column.Item().Text($"Tiempo licencia: {licencia.TiempoLicencia}");
                        column.Item().Text($"Dias faltantes: {licencia.DiaFaltantes}");
                        column.Item().Text($"Auditoria: {(licencia.Auditoria ? "Si" : "No")}");
                        column.Item().Text($"Registrado por ID: {licencia.RegistradoPorId}");
                        column.Item().Text($"Registrado por nick: {ValorTexto(licencia.RegistradoPorNick)}");
                        column.Item().Text($"Fecha registro sistema: {FormatearFechaHora(licencia.FechaRegistroSistema)}");
                        column.Item().PaddingTop(10).Text("Diagnostico").SemiBold();
                        column.Item().Text(ValorTexto(licencia.Diagnostico));
                        column.Item().PaddingTop(10).Text("Observacion").SemiBold();
                        column.Item().Text(ValorTexto(licencia.Observacion));
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Pagina ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                });

                foreach (byte[] imagen in imagenes)
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .Text("Soporte Documental")
                            .FontSize(16)
                            .SemiBold();

                        page.Content()
                            .AlignCenter()
                            .AlignMiddle()
                            .Image(imagen)
                            .FitArea();

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Pagina ");
                                text.CurrentPageNumber();
                                text.Span(" de ");
                                text.TotalPages();
                            });
                    });
                }
            }).GeneratePdf();
        }

        /// <summary>
        /// Guarda una copia de respaldo del archivo PNG en Google Drive.
        /// Retorna el ID del archivo creado en Google Drive.
        /// </summary>
        /// <param name="nombreArchivo">Nombre con el que se guardara el archivo en Google Drive.</param>
        /// <param name="pngData">Contenido del archivo PNG en bytes.</param>
        /// <returns>ID del archivo creado en Google Drive.</returns>
        public async Task<string> SetBackup(string nombreArchivo, byte[] pngData)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo))
                throw new ArgumentException("Debe especificar el nombre del archivo.", nameof(nombreArchivo));

            if (pngData == null || pngData.Length == 0)
                throw new ArgumentException("Debe especificar el contenido del archivo.", nameof(pngData));

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

            await using MemoryStream stream = new MemoryStream(pngData);

            FilesResource.CreateMediaUpload request = driveService.Files.Create(
                fileMetadata,
                stream,
                "image/png"
            );

            request.Fields = "id, name";

            Google.Apis.Upload.IUploadProgress uploadProgress = await request.UploadAsync();

            if (uploadProgress.Status != Google.Apis.Upload.UploadStatus.Completed)
                throw new InvalidOperationException($"No se pudo subir el archivo a Google Drive. Estado: {uploadProgress.Status}. Error: {uploadProgress.Exception?.Message}");

            return request.ResponseBody?.Id
                ?? throw new InvalidOperationException("Google Drive no retorno el ID del archivo creado.");
        }

        private List<byte[]> CargarArchivosSoporte(List<string>? nombresArchivos)
        {
            var imagenes = new List<byte[]>();

            if (nombresArchivos == null || nombresArchivos.Count == 0)
                return imagenes;

            foreach (string nombreArchivo in nombresArchivos)
            {
                if (string.IsNullOrWhiteSpace(nombreArchivo))
                    continue;

                string rutaCompleta = ObtenerRutaCompletaArchivo(nombreArchivo);

                if (!File.Exists(rutaCompleta))
                    throw new FileNotFoundException($"Archivo no encontrado: {nombreArchivo}", rutaCompleta);

                imagenes.Add(File.ReadAllBytes(rutaCompleta));
            }

            return imagenes;
        }

        private string ObtenerRutaCompletaArchivo(string nombreArchivo)
        {
            string rutaSoporteDoc = ObtenerRutaSoporteDoc();
            string nombreSeguro = Path.GetFileName(nombreArchivo);
            return Path.Combine(rutaSoporteDoc, nombreSeguro);
        }

        private string ObtenerRutaSoporteDoc()
        {
            string rutaSoporteDoc = _configuration.GetValue<string>("rutadoportedoc") ?? "\\filesoporte\\";

            if (string.IsNullOrWhiteSpace(rutaSoporteDoc))
                rutaSoporteDoc = "\\filesoporte\\";

            return rutaSoporteDoc;
        }

        private static string FormatearFecha(DateTime fecha)
        {
            return fecha == default ? string.Empty : fecha.ToString("yyyy-MM-dd");
        }

        private static string FormatearFechaHora(DateTime fecha)
        {
            return fecha == default ? string.Empty : fecha.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static string ValorTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? string.Empty : valor;
        }

        private static string GenerarHashSha256(byte[] data)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(data);
            return Convert.ToHexString(hashBytes);
        }
    }
}
