using ClosedXML.Excel;
using ServPersonalCtr.Managers.L10;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L20
{
    public class mngLicencias
    {
        private readonly ServPersonalCtr.Managers.L10.mngLicencias _mngLicenciasL10;
        private readonly MngFileSoporte _mngFileSoporte;

        // Inyectamos los Manager de la Capa L10
        public mngLicencias(
            ServPersonalCtr.Managers.L10.mngLicencias mngLicenciasL10,
            MngFileSoporte mngFileSoporte)
        {
            _mngLicenciasL10 = mngLicenciasL10;
            _mngFileSoporte = mngFileSoporte;
        }

        /// <summary>
        /// Acceso intermedio para registrar una nueva licencia.
        /// Recibe la licencia y una lista opcional de archivos PDF en bytes.
        /// Primero crea los soportes documentales y luego registra la licencia.
        /// </summary>
        public async Task<int> SetLicencias(
            string token,
            DTOLicencias licencia,
            List<byte[]>? archivosPdf = null)
        {
            var soportesDoc = new List<DTOSoporteDoc>();

            if (archivosPdf != null && archivosPdf.Count > 0)
            {
                foreach (byte[] archivoPdf in archivosPdf)
                {
                    if (archivoPdf == null || archivoPdf.Length == 0)
                        continue;

                    DTOSoporteDoc soporteDoc = await _mngFileSoporte.Create(archivoPdf);
                    soportesDoc.Add(soporteDoc);
                }
            }

            return _mngLicenciasL10.SetLicencias(token, licencia, soportesDoc);
        }

        /// <summary>
        /// Acceso intermedio para consultar licencias con filtros avanzados.
        /// </summary>
        public List<DTOLicencias> GetLicencias(string token, int idPersona = 0,
                                              DateTime? fecIni = null, DateTime? fecFin = null,
                                              DateTime? regDesde = null, DateTime? regHasta = null)
        {
            return _mngLicenciasL10.GetLicencias(token, idPersona, fecIni, fecFin, regDesde, regHasta);
        }

        public List<DTOLicencias> GetLicenciasActivas(int numeroPagina, int tamanioPagina)
        {
            return _mngLicenciasL10.GetLicenciasActivas(numeroPagina, tamanioPagina);
        }

        /// <summary>
        /// Obtiene las licencias y genera un archivo Excel en memoria.
        /// </summary>
        public byte[] GetLicenciasExcel(string token, int idPersona = 0,
                                        DateTime? fecIni = null, DateTime? fecFin = null,
                                        DateTime? regDesde = null, DateTime? regHasta = null)
        {
            var datos = _mngLicenciasL10.GetLicencias(token, idPersona, fecIni, fecFin, regDesde, regHasta);
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Hoja1");
                worksheet.Cell(1, 1).Value = "No. Licencia";
                worksheet.Cell(1, 2).Value = "Empleado";
                worksheet.Cell(1, 3).Value = "Cedula";
                worksheet.Cell(1, 4).Value = "Puesto";
                worksheet.Cell(1, 5).Value = "Desde";
                worksheet.Cell(1, 6).Value = "Hasta";
                worksheet.Cell(1, 7).Value = "Dias";
                worksheet.Cell(1, 8).Value = "Dias faltantes";
                worksheet.Cell(1, 9).Value = "Diagnostico";
                var headerRange = worksheet.Range("A1:I1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                int currentRow = 2;
                foreach (var lic in datos)
                {
                    worksheet.Cell(currentRow, 1).Value = lic.NoLicencia;
                    worksheet.Cell(currentRow, 2).Value = lic.EmpleadoNombreCompleto;
                    worksheet.Cell(currentRow, 3).Value = lic.EmpleadoCedula;
                    worksheet.Cell(currentRow, 4).Value = lic.PuestoTrabajo;
                    worksheet.Cell(currentRow, 5).Value = lic.FecLicenciaIni.ToShortDateString();
                    worksheet.Cell(currentRow, 6).Value = lic.FecLicenciaFin.ToShortDateString();
                    worksheet.Cell(currentRow, 7).Value = lic.TiempoLicencia;
                    worksheet.Cell(currentRow, 8).Value = lic.DiaFaltantes;
                    worksheet.Cell(currentRow, 9).Value = lic.Diagnostico;
                    currentRow++;
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] GetLicenciasActivasExcell(int numeroPagina, int tamanioPagina)
        {
            List<DTOLicencias> datos = _mngLicenciasL10.GetLicenciasActivas(numeroPagina, tamanioPagina);
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Hoja1");
                worksheet.Cell(1, 1).Value = "No. Licencia";
                worksheet.Cell(1, 2).Value = "Empleado";
                worksheet.Cell(1, 3).Value = "Cedula";
                worksheet.Cell(1, 4).Value = "Puesto";
                worksheet.Cell(1, 5).Value = "Desde";
                worksheet.Cell(1, 6).Value = "Hasta";
                worksheet.Cell(1, 7).Value = "Dias";
                worksheet.Cell(1, 8).Value = "Dias faltantes";
                worksheet.Cell(1, 9).Value = "Diagnostico";
                var headerRange = worksheet.Range("A1:I1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                int currentRow = 2;
                foreach (var lic in datos)
                {
                    worksheet.Cell(currentRow, 1).Value = lic.NoLicencia;
                    worksheet.Cell(currentRow, 2).Value = lic.EmpleadoNombreCompleto;
                    worksheet.Cell(currentRow, 3).Value = lic.EmpleadoCedula;
                    worksheet.Cell(currentRow, 4).Value = lic.PuestoTrabajo;
                    worksheet.Cell(currentRow, 5).Value = lic.FecLicenciaIni.ToShortDateString();
                    worksheet.Cell(currentRow, 6).Value = lic.FecLicenciaFin.ToShortDateString();
                    worksheet.Cell(currentRow, 7).Value = lic.TiempoLicencia;
                    worksheet.Cell(currentRow, 8).Value = lic.DiaFaltantes;
                    worksheet.Cell(currentRow, 9).Value = lic.Diagnostico;
                    currentRow++;
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
