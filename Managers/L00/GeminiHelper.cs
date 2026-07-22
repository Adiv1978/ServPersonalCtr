using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L00
{
    public class GeminiHelper
    {
        private const string GeminiApiBaseUrl =
            "https://generativelanguage.googleapis.com/v1beta/models";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiHelper(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Envía una imagen PNG a Gemini y retorna los datos estructurados
        /// de la licencia médica.
        /// </summary>
        public async Task<GPT_Licencias> AnalizarLicenciaPdfAsync(
            byte[] imageData,
            string nombreArchivo = "licencia.png")
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Debe especificar el contenido de la imagen PNG.", nameof(imageData));

            if (string.IsNullOrWhiteSpace(nombreArchivo))
                nombreArchivo = "licencia.png";

            if (!nombreArchivo.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("El archivo debe ser una imagen PNG.", nameof(nombreArchivo));

            string apiKey = (_configuration.GetValue<string>("Gemini:ApiKey") ?? string.Empty).Trim();
            string model = (_configuration.GetValue<string>("Gemini:Model") ?? "gemini-3.5-flash").Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("No se ha configurado Gemini:ApiKey.");

            if (string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("No se ha configurado Gemini:Model.");

            string base64Image = Convert.ToBase64String(imageData);
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = ObtenerPrompt() },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = "image/png",
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseJsonSchema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            EmpleadoCedula = new { type = "string" },
                            EmpleadoNombreCompleto = new { type = "string" },
                            FecLicenciaIni = new { type = "string", format = "date" },
                            FecLicenciaFin = new { type = "string", format = "date" },
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
            };

            string requestUrl =
                $"{GeminiApiBaseUrl}/{Uri.EscapeDataString(model)}:generateContent";
            string jsonRequest = JsonSerializer.Serialize(requestBody);

            using HttpRequestMessage request = new(HttpMethod.Post, requestUrl);
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Error al consultar Gemini. Status: {(int)response.StatusCode} " +
                        $"{response.StatusCode}. Respuesta: {responseContent}");
                }

                string jsonLicencia = ExtraerJsonRespuesta(responseContent);

                if (string.IsNullOrWhiteSpace(jsonLicencia))
                    throw new InvalidOperationException("Gemini no retornó datos para la licencia.");

                GPT_Licencias? licencia = JsonSerializer.Deserialize<GPT_Licencias>(
                    jsonLicencia,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return licencia ?? throw new InvalidOperationException(
                    "No se pudo deserializar la respuesta de Gemini a GPT_Licencias.");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("No fue posible conectar con la API de Gemini.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("La solicitud a Gemini excedió el tiempo de espera.", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("La respuesta de Gemini no tiene un formato JSON válido.", ex);
            }
        }

        private static string ObtenerPrompt()
        {
            return """
                Analiza esta imagen PNG de una licencia médica y extrae los datos solicitados.

                Reglas obligatorias:
                - No inventes datos.
                - Las fechas deben estar en formato yyyy-MM-dd.
                - EmpleadoCedula debe contener solo la cédula si aparece en el documento.
                - EmpleadoNombreCompleto debe contener el nombre completo del empleado.
                - FecLicenciaIni es la fecha inicial de la licencia.
                - FecLicenciaFin es la fecha final de la licencia.
                - TiempoLicencia debe ser la cantidad de días de licencia.
                - Diagnostico debe contener el diagnóstico médico si aparece.
                - Observacion debe contener cualquier nota adicional relevante.
                - Si un texto no aparece, devuelve una cadena vacía.
                - Si una fecha no aparece, usa 0001-01-01.
                - Si la imagen tiene texto poco legible, interpreta con cuidado sin inventar.
                """;
        }

        private static string ExtraerJsonRespuesta(string responseContent)
        {
            using JsonDocument doc = JsonDocument.Parse(responseContent);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out JsonElement candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                string bloqueo = ExtraerMotivoBloqueo(root);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(bloqueo)
                        ? "Gemini no retornó candidatos en la respuesta."
                        : $"Gemini bloqueó la solicitud: {bloqueo}");
            }

            JsonElement candidate = candidates[0];

            if (!candidate.TryGetProperty("content", out JsonElement content) ||
                !content.TryGetProperty("parts", out JsonElement parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Gemini retornó una respuesta sin contenido.");
            }

            var text = new StringBuilder();

            foreach (JsonElement part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out JsonElement textElement))
                    text.Append(textElement.GetString());
            }

            return text.ToString();
        }

        private static string ExtraerMotivoBloqueo(JsonElement root)
        {
            if (root.TryGetProperty("promptFeedback", out JsonElement feedback) &&
                feedback.TryGetProperty("blockReason", out JsonElement blockReason))
            {
                return blockReason.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
