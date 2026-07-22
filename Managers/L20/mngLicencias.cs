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

        public List<DTOLicencias> GetLicencias(string token, int idPersona = 0,
                                              DateTime? fecIni = null, DateTime? fecFin = null,
                                              DateTime? regDesde = null, DateTime? regHasta = null)
        {
            return _mngLicenciasL10.GetLicencias(token, idPersona, fecIni, fecFin, regDesde, regHasta);
        }

        public List<DTOLicencias> GetLicenciaIni(DTOGetLicenciaIniRequest request)
        {
            return _mngLicenciasL10.GetLicenciaIni(request);
        }

        public byte[] GetLicenciaIniExcel(DTOGetLicenciaIniRequest request)
        {
            List<DTOLicencias> datos = _mngLicenciasL10.GetLicenciaIni(request);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Licencias por fecha");

            worksheet.Cell(1, 1).Value = "No. Licencia";
            worksheet.Cell(1, 2).Value = "Empleado";
            worksheet.Cell(1, 3).Value = "Cedula";
            worksheet.Cell(1, 4).Value = "Puesto";
            worksheet.Cell(1, 5).Value = "Desde";
            worksheet.Cell(1, 6).Value = "Hasta";
            worksheet.Cell(1, 7).Value = "Dias";
            worksheet.Cell(1, 8).Value = "Diagnostico";
            worksheet.Cell(1, 9).Value = "Observacion";

            var headerRange = worksheet.Range("A1:I1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#08445B");

            int currentRow = 2;
            foreach (DTOLicencias licencia in datos)
            {
                worksheet.Cell(currentRow, 1).Value = licencia.NoLicencia;
                worksheet.Cell(currentRow, 2).Value = licencia.EmpleadoNombreCompleto;
                worksheet.Cell(currentRow, 3).Value = licencia.EmpleadoCedula;
                worksheet.Cell(currentRow, 4).Value = licencia.PuestoTrabajo;
                worksheet.Cell(currentRow, 5).Value = licencia.FecLicenciaIni;
                worksheet.Cell(currentRow, 6).Value = licencia.FecLicenciaFin;
                worksheet.Cell(currentRow, 7).Value = licencia.TiempoLicencia;
                worksheet.Cell(currentRow, 8).Value = licencia.Diagnostico;
                worksheet.Cell(currentRow, 9).Value = licencia.Observacion;
                currentRow++;
            }

            if (datos.Count > 0)
            {
                worksheet.Range(2, 5, currentRow - 1, 6).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Range(1, 1, currentRow - 1, 9).CreateTable();
            }

            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public List<DTOSoporteDoc> GetLicenciaSoporteDoc(GetSoporteDocRequest request)
        {
            return _mngLicenciasL10.GetLicenciaSoporteDoc(request);
        }

        public byte[] GetLicenciasDoc(string token, DTOLicencias licencia)
        {
            if (licencia == null)
                throw new ArgumentNullException(nameof(licencia));

            List<DTOSoporteDoc> soportes = _mngLicenciasL10.GetLicenciaSoporteDoc(new GetSoporteDocRequest
            {
                Token = token,
                IdLicencia = licencia.LicenciaId
            });

            List<string> nombresArchivos = soportes
                .Where(x => !string.IsNullOrWhiteSpace(x.NombreArchivo))
                .Select(x => x.NombreArchivo)
                .ToList();

            return _mngFileSoporte.BuildLicenciaPdf(nombresArchivos, licencia);
        }

        public List<DTOLicencias> GetLicenciasActivas(int numeroPagina, int tamanioPagina)
        {
            return _mngLicenciasL10.GetLicenciasActivas(numeroPagina, tamanioPagina);
        }

        /// <summary>
        /// Acceso intermedio para actualizar una licencia existente.
        /// </summary>
        public bool UpdateLicencia(DTOUpdateLicenciaRequest request)
        {
            return _mngLicenciasL10.UpdateLicencia(request);
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
