using Microsoft.AspNetCore.Mvc;
using ServPersonalCtr.Managers.L20;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GptController : ControllerBase
    {
        private readonly MngGpt _gptL20;
        private readonly mngSeguridad _seguridadL20;

        public GptController(MngGpt gptL20, mngSeguridad seguridadL20)
        {
            _gptL20 = gptL20;
            _seguridadL20 = seguridadL20;
        }

        [HttpPost("AnalizarLicenciaPdfAsync")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AnalizarLicenciaPdfAsync(
            [FromQuery] string token,
            [FromQuery] int minutos,
            [FromQuery] short rolLevel,
            [FromForm] IFormFile archivoPdf)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return Unauthorized(new { message = "Debe especificar el token de sesión." });

                if (archivoPdf == null || archivoPdf.Length == 0)
                    return BadRequest(new { message = "Debe enviar un archivo PDF." });

                if (!archivoPdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "El archivo debe ser un PDF." });

                DTOSession session = _seguridadL20.ValidateSeccion(token, minutos, rolLevel);

                if (session == null || string.IsNullOrWhiteSpace(session.Token))
                    return Unauthorized(new { message = "Sesión inválida o expirada." });

                await using MemoryStream memoryStream = new MemoryStream();
                await archivoPdf.CopyToAsync(memoryStream);

                GPT_Licencias resultado = await _gptL20.AnalizarLicenciaPdfAsync(
                    memoryStream.ToArray(),
                    archivoPdf.FileName
                );

                return Ok(resultado);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
