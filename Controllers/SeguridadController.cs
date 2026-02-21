using Microsoft.AspNetCore.Mvc;
using ServPersonalCtr.Managers.L20;


namespace ServPersonalCtr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeguridadController : ControllerBase
    {
        private readonly mngSeguridad _seguridadL20;

        // Inyectamos la capa intermedia L20
        public SeguridadController(mngSeguridad seguridadL20)
        {
            _seguridadL20 = seguridadL20;
        }

        /// <summary>
        /// Endpoint para autenticación de usuarios.
        /// </summary>
        [HttpPost("Login")]
        public IActionResult Login([FromBody] Types.LoginRequest request)
        {
            try
            {
                var session = _seguridadL20.SetSeccion(request.Nick, request.Pass, request.Minutos);
                if (session == null)
                    return Unauthorized(new { message = "Credenciales incorrectas o usuario inactivo." });
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para validar si un token sigue siendo válido.
        /// </summary>
        [HttpGet("Validate")]
        public IActionResult Validate([FromQuery] string token, [FromQuery] int minutos, [FromQuery] short rolLevel)
        {
            try
            {
                var session = _seguridadL20.ValidateSeccion(token, minutos, rolLevel);
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
