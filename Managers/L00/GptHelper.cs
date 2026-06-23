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
        /// Envia una imagen PNG a OpenAI para analizar una licencia medica
        /// y retorna los datos estructurados para serializar en GPT_Licencias.
        /// </summary>
        /// <param name="pdfData">Contenido de la imagen PNG en bytes.</param>
        /// <param name="nombreArchivo">Nombre del archivo PNG.</param>
        /// <returns>Datos extraidos de la imagen.</returns>
        public async Task<GPT_Licencias> AnalizarLicenciaPdfAsync(byte[] pdfData, string nombreArchivo = "licencia.png")
        {
            if (pdfData == null || pdfData.Length == 0)
                throw new ArgumentException("Debe especificar el contenido de la imagen PNG.", nameof(pdfData));

            if (string.IsNullOrWhiteSpace(nombreArchivo))
                nombreArchivo = "licencia.png";

            if (!nombreArchivo.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                nombreArchivo += ".png";

            string apiKey = (_configuration.GetValue<string>("OpenAI:ApiKey") ?? string.Empty).Trim();
            string model = (_configuration.GetValue<string>("OpenAI:Model") ?? "gpt-5.5").Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("No se ha configurado OpenAI:ApiKey en appsettings.json.");

            if (string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("No se ha configurado OpenAI:Model en appsettings.json.");

            string base64Image = Convert.ToBase64String(pdfData);

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
                                type = "input_text",
                                text = """
                                Analiza esta imagen PNG de una licencia medica y extrae los datos solicitados.

                                Reglas obligatorias:
                                - Devuelve unicamente el JSON solicitado.
                                - No agregues explicaciones ni markdown.
                                - No inventes datos.
                                - Las fechas deben estar en formato yyyy-MM-dd.
                                - EmpleadoCedula debe contener solo la cedula si aparece en el documento.
                                - EmpleadoNombreCompleto debe contener el nombre completo del empleado.
                                - FecLicenciaIni es la fecha inicial de la licencia.
                                - FecLicenciaFin es la fecha final de la licencia.
                                - TiempoLicencia debe ser la cantidad de dias de licencia.
                                - Diagnostico debe contener el diagnostico medico si aparece.
                                - Observacion debe contener cualquier nota adicional relevante.
                                - Si un texto no aparece, devuelve cadena vacia.
                                - Si una fecha no aparece, usa "0001-01-01".
                                - Si la imagen tiene texto poco legible, interpreta con cuidado sin inventar.
                                """
                            },
                            new
                            {
                                type = "input_image",
                                image_url = $"data:image/png;base64,{base64Image}",
                                detail = "high"
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
            request.Headers.Accept.ParseAdd("application/json");
            request.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Error al consultar OpenAI. Status: {(int)response.StatusCode} {response.StatusCode}. Respuesta: {responseContent}");

                string jsonLicencia = ExtraerJsonRespuesta(responseContent);

                if (string.IsNullOrWhiteSpace(jsonLicencia))
                    throw new InvalidOperationException("OpenAI no retorno un JSON valido para la licencia.");

                GPT_Licencias? licencia = JsonSerializer.Deserialize<GPT_Licencias>(jsonLicencia, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return licencia ?? throw new InvalidOperationException("No se pudo deserializar la respuesta de OpenAI a GPT_Licencias.");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("No fue posible conectar con la API de OpenAI.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("La solicitud a OpenAI excedio el tiempo de espera.", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("La respuesta de OpenAI no tiene un formato JSON valido.", ex);
            }
        }

        private static string ExtraerJsonRespuesta(string responseContent)
        {
            using JsonDocument doc = JsonDocument.Parse(responseContent);

            if (doc.RootElement.TryGetProperty("output_text", out JsonElement outputTextElement))
                return outputTextElement.GetString() ?? string.Empty;

            if (!doc.RootElement.TryGetProperty("output", out JsonElement outputArray))
                return string.Empty;

            foreach (JsonElement outputItem in outputArray.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out JsonElement contentArray))
                    continue;

                foreach (JsonElement contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("refusal", out JsonElement refusalElement))
                        throw new InvalidOperationException($"OpenAI rechazo la solicitud: {refusalElement.GetString()}");

                    if (contentItem.TryGetProperty("text", out JsonElement textElement))
                        return textElement.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
    }
}
