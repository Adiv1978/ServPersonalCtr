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
        /// Registra una nueva licencia médica y sus archivos PDF de soporte.
        /// </summary>
        [HttpPost("Set")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SetLicencias(
            [FromQuery] string token,
            [FromForm] DTOLicencias licencia,
            [FromForm] List<IFormFile>? archivosPdf = null)
        {
            try
            {
                var archivosBytes = new List<byte[]>();

                if (archivosPdf != null && archivosPdf.Count > 0)
                {
                    foreach (IFormFile archivoPdf in archivosPdf)
                    {
                        if (archivoPdf == null || archivoPdf.Length == 0)
                            continue;

                        await using var memoryStream = new MemoryStream();
                        await archivoPdf.CopyToAsync(memoryStream);
                        archivosBytes.Add(memoryStream.ToArray());
                    }
                }

                int idGenerado = await _licenciasL20.SetLicencias(
                    token,
                    licencia,
                    archivosBytes
                );

                return Ok(new { id = idGenerado, message = "Licencia registrada con exito." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el listado de licencias con filtros de fecha y personal.
        /// </summary>
        [HttpGet("Get")]
        public IActionResult GetLicencias(
            [FromQuery] string token,
            [FromQuery] int idPersona = 0,
            [FromQuery] DateTime? fecIni = null,
            [FromQuery] DateTime? fecFin = null,
            [FromQuery] DateTime? regDesde = null,
            [FromQuery] DateTime? regHasta = null)
        {
            try
            {
                var resultado = _licenciasL20.GetLicencias(token, idPersona, fecIni, fecFin, regDesde, regHasta);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("GetSoporteDoc")]
        public IActionResult GetLicenciaSoporteDoc([FromBody] GetSoporteDocRequest request)
        {
            try
            {
                List<DTOSoporteDoc> resultado = _licenciasL20.GetLicenciaSoporteDoc(request);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("Update")]
        public IActionResult UpdateLicencia([FromBody] DTOUpdateLicenciaRequest request)
        {
            try
            {
                bool success = _licenciasL20.UpdateLicencia(request);

                if (success)
                    return Ok(new { message = "Licencia actualizada exitosamente." });

                return BadRequest(new { message = "No se pudo actualizar la licencia." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetExcel")]
        public IActionResult GetExcel(
            [FromQuery] string token,
            [FromQuery] int idPersona = 0,
            [FromQuery] DateTime? fecIni = null,
            [FromQuery] DateTime? fecFin = null)
        {
            try
            {
                byte[] archivoExcel = _licenciasL20.GetLicenciasExcel(token, idPersona, fecIni, fecFin);
                string nombreArchivo = $"Reporte_Licencias_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(archivoExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetActivas")]
        public IActionResult GetActivas(
            [FromQuery] int numeroPagina,
            [FromQuery] int tamanioPagina)
        {
            try
            {
                List<DTOLicencias> resultado = _licenciasL20.GetLicenciasActivas(numeroPagina, tamanioPagina);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetExcelActivas")]
        public IActionResult GetExcelActivas(
            [FromQuery] int numeroPagina,
            [FromQuery] int tamanioPagina)
        {
            try
            {
                byte[] archivoExcel = _licenciasL20.GetLicenciasActivasExcell(numeroPagina, tamanioPagina);
                string nombreArchivo = $"Reporte_Licencias_Activas_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(archivoExcel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
