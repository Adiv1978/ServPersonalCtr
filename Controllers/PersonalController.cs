using Microsoft.AspNetCore.Mvc;
using ServPersonalCtr.Managers.L20;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonalController : ControllerBase
    {
        private readonly mngPersonal _personalL20;

        // Inyectamos la capa intermedia L20
        public PersonalController(mngPersonal personalL20)
        {
            _personalL20 = personalL20;
        }

        /// <summary>
        /// Registra o actualiza un registro de personal.
        /// </summary>
        [HttpPost("Set")]
        public IActionResult SetPersonal([FromQuery] string token, [FromQuery] int minutos, [FromBody] DTOPersonal personal)
        {
            try
            {
                int idGenerado = _personalL20.SetPersonal(token, minutos, personal);
                return Ok(new { id = idGenerado, message = "Registro procesado correctamente." });
            }
            catch (Exception ex)
            {
                // Captura RAISE EXCEPTION de PostgreSQL (ej: "Cédula ya registrada")
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el listado de personal filtrado por ID o búsqueda general.
        /// </summary>
        [HttpGet("Get")]
        public IActionResult GetPersonal([FromQuery] string token, [FromQuery] int minutos, [FromQuery] int id = 0, [FromQuery] string busqueda = "")
        {
            try
            {
                var lista = _personalL20.GetPersonal(token, minutos, id, busqueda);
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
