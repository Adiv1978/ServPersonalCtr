using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L00
{
    public class GptHelper
    {
        private const string OpenAiResponsesUrl = "https://api.openai.com/v1/responses";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GptHelper(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Envía un documento PDF a OpenAI para analizar una licencia médica
        /// y retorna los datos estructurados para serializar en GPT_Licencias.
        /// </summary>
        /// <param name="pdfData">Contenido del PDF en bytes.</param>
        /// <param name="nombreArchivo">Nombre del archivo PDF.</param>
        /// <returns>Datos extraídos del documento.</returns>
        public async Task<GPT_Licencias> AnalizarLicenciaPdfAsync(byte[] pdfData, string nombreArchivo = "licencia.pdf")
        {
            if (pdfData == null || pdfData.Length == 0)
                throw new ArgumentException("Debe especificar el contenido del PDF.", nameof(pdfData));

            if (string.IsNullOrWhiteSpace(nombreArchivo))
                nombreArchivo = "licencia.pdf";

            if (!nombreArchivo.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                nombreArchivo += ".pdf";

            string apiKey = _configuration.GetValue<string>("OpenAI:ApiKey") ?? string.Empty;
            string model = _configuration.GetValue<string>("OpenAI:Model") ?? "gpt-5.5";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("No se ha configurado OpenAI:ApiKey en appsettings.json.");

            string base64Pdf = Convert.ToBase64String(pdfData);

            var requestBody = new
            {
                model,
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "input_file",
                                filename = nombreArchivo,
                                file_data = $"data:application/pdf;base64,{base64Pdf}"
                            },
                            new
                            {
                                type = "input_text",
                                text = """
                                Analiza este PDF de licencia médica y extrae los datos solicitados.

                                Reglas obligatorias:
                                - Devuelve únicamente el JSON solicitado.
                                - No agregues explicaciones ni markdown.
                                - No inventes datos.
                                - Las fechas deben estar en formato yyyy-MM-dd.
                                - EmpleadoCedula debe contener solo la cédula si aparece en el documento.
                                - EmpleadoNombreCompleto debe contener el nombre completo del empleado.
                                - FecLicenciaIni es la fecha inicial de la licencia.
                                - FecLicenciaFin es la fecha final de la licencia.
                                - TiempoLicencia debe ser la cantidad de días de licencia.
                                - Diagnostico debe contener el diagnóstico médico si aparece.
                                - Observacion debe contener cualquier nota adicional relevante.
                                - Si un texto no aparece, devuelve cadena vacía.
                                - Si una fecha no aparece, usa "0001-01-01".
                                """
                            }
                        }
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "gpt_licencias",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                EmpleadoCedula = new { type = "string" },
                                EmpleadoNombreCompleto = new { type = "string" },
                                FecLicenciaIni = new { type = "string" },
                                FecLicenciaFin = new { type = "string" },
                                TiempoLicencia = new { type = "integer" },
                                Diagnostico = new { type = "string" },
                                Observacion = new { type = "string" }
                            },
                            required = new[]
                            {
                                "EmpleadoCedula",
                                "EmpleadoNombreCompleto",
                                "FecLicenciaIni",
                                "FecLicenciaFin",
                                "TiempoLicencia",
                                "Diagnostico",
                                "Observacion"
                            }
                        }
                    }
                }
            };

            string jsonRequest = JsonSerializer.Serialize(requestBody);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, OpenAiResponsesUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Error al consultar OpenAI. Status: {response.StatusCode}. Respuesta: {responseContent}");

            string jsonLicencia = ExtraerJsonRespuesta(responseContent);

            if (string.IsNullOrWhiteSpace(jsonLicencia))
                throw new InvalidOperationException("OpenAI no retornó un JSON válido para la licencia.");

            GPT_Licencias? licencia = JsonSerializer.Deserialize<GPT_Licencias>(jsonLicencia, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return licencia ?? throw new InvalidOperationException("No se pudo deserializar la respuesta de OpenAI a GPT_Licencias.");
        }

        private static string ExtraerJsonRespuesta(string responseContent)
        {
            using JsonDocument doc = JsonDocument.Parse(responseContent);

            if (!doc.RootElement.TryGetProperty("output", out JsonElement outputArray))
                return string.Empty;

            foreach (JsonElement outputItem in outputArray.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out JsonElement contentArray))
                    continue;

                foreach (JsonElement contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out JsonElement textElement))
                        return textElement.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
    }
}
