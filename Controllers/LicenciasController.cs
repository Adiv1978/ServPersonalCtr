using Microsoft.AspNetCore.Mvc;
using ServPersonalCtr.Managers.L20;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LicenciasController : ControllerBase
    {
        private readonly mngLicencias _licenciasL20;

        public LicenciasController(mngLicencias licenciasL20)
        {
            _licenciasL20 = licenciasL20;
        }

        /// <summary>
        /// Registra una nueva licencia médica.
        /// </summary>
        [HttpPost("Set")]
        public IActionResult SetLicencias([FromQuery] string token, [FromQuery] int minutos, [FromBody] DTOLicencias licencia)
        {
            try
            {
                int idGenerado = _licenciasL20.SetLicencias(token, minutos, licencia);
                return Ok(new { id = idGenerado, message = "Licencia registrada con éxito." });
            }
            catch (Exception ex)
            {
                // Captura errores de validación de PostgreSQL (ej: fechas inválidas o duplicados)
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el listado de licencias con filtros de fecha y personal.
        /// </summary>
        [HttpGet("Get")]
        public IActionResult GetLicencias(
            [FromQuery] string token,
            [FromQuery] int minutos,
            [FromQuery] int idPersona = 0,
            [FromQuery] DateTime? fecIni = null,
            [FromQuery] DateTime? fecFin = null,
            [FromQuery] DateTime? regDesde = null,
            [FromQuery] DateTime? regHasta = null)
        {
            try
            {
                var resultado = _licenciasL20.GetLicencias(token, minutos, idPersona, fecIni, fecFin, regDesde, regHasta);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetExcel")]
        public IActionResult GetExcel(
    [FromQuery] string token,
    [FromQuery] int minutos,
    [FromQuery] int idPersona = 0,
    [FromQuery] DateTime? fecIni = null,
    [FromQuery] DateTime? fecFin = null)
        {
            try
            {
                byte[] archivoExcel = _licenciasL20.GetLicenciasExcel(token, minutos, idPersona, fecIni, fecFin);
                string nombreArchivo = $"Reporte_Licencias_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(archivoExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
