using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public static class ImpresorRecibo
    {
        /// <summary>
        /// Procesa la factura: guarda el PDF en el Escritorio y envía a la POS-80.
        /// </summary>
        public static void Imprimir(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles, string printerName = "POS-80")
        {
            try
            {
                // 1. Guardar copia en PDF en el Escritorio (Carpeta "Recibos Digitales")
                GuardarReciboPdfEnEscritorio(pago, nombreEstudiante, nombreConcepto, detalles);

                // 2. Construir texto plano para la impresora térmica
                string textoFactura = ConstruirFacturaTexto(pago, nombreEstudiante, nombreConcepto, detalles);

                // 3. Imprimir en físico en la POS-80 (una sola vez)
                ImprimirTextoDirecto(textoFactura, printerName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Aviso en proceso de recibo:\n{ex.Message}", "Aviso de Facturación", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Genera el archivo PDF silenciosamente en Escritorio\Recibos Digitales.
        /// </summary>
        private static void GuardarReciboPdfEnEscritorio(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles)
        {
            try
            {
                string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string carpeta = Path.Combine(escritorio, "Recibos Digitales");

                if (!Directory.Exists(carpeta))
                {
                    Directory.CreateDirectory(carpeta);
                }

                // Nombre limpio del archivo PDF
                string nombreLimpio = string.Join("_", pago.NumeroRecibo.Split(Path.GetInvalidFileNameChars()));
                string rutaPdf = Path.Combine(carpeta, $"{nombreLimpio}.pdf");

                if (File.Exists(rutaPdf))
                {
                    try { File.Delete(rutaPdf); } catch { }
                }

                using var printDoc = new PrintDocument();

                // Buscar la impresora virtual PDF de Windows
                string? pdfPrinter = PrinterSettings.InstalledPrinters.Cast<string>()
                    .FirstOrDefault(p => p.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0);

                printDoc.PrinterSettings.PrinterName = pdfPrinter ?? "Microsoft Print to PDF";
                printDoc.PrinterSettings.PrintToFile = true;
                printDoc.PrinterSettings.PrintFileName = rutaPdf;

                printDoc.PrintPage += (sender, e) =>
                {
                    var g = e.Graphics!;
                    using var fontTitulo = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);
                    using var fontSub = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Regular);
                    using var fontSeccion = new System.Drawing.Font("Segoe UI", 10.5f, System.Drawing.FontStyle.Bold);
                    using var fontNegrita = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
                    using var fontTexto = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);

                    float x = 50;
                    float y = 50;
                    float lineHeight = 20;

                    // Encabezado
                    g.DrawString("CEVENCA", fontTitulo, System.Drawing.Brushes.DarkBlue, x, y);
                    y += 26;
                    g.DrawString("Rif: J-507629082 | Registro MPPE: 0016-1904", fontSub, System.Drawing.Brushes.Gray, x, y);
                    y += 16;
                    g.DrawString("0414-4281527 / 0426-2355934 • Av. Las Ferias, C.C. Isora, Mezz.", fontSub, System.Drawing.Brushes.Gray, x, y);
                    y += 18;
                    g.DrawString("RECIBO DE PAGO DIGITAL", fontSeccion, System.Drawing.Brushes.Black, x, y);
                    y += 22;

                    g.DrawLine(System.Drawing.Pens.DarkBlue, x, y, 520, y);
                    y += 12;

                    // Datos del Recibo y Alumno
                    g.DrawString($"Nro. Recibo : {pago.NumeroRecibo}", fontNegrita, System.Drawing.Brushes.Black, x, y); y += lineHeight;
                    g.DrawString($"Fecha/Hora  : {pago.FechaPago:dd/MM/yyyy hh:mm tt}", fontTexto, System.Drawing.Brushes.Black, x, y); y += lineHeight;
                    g.DrawString($"Cédula      : {pago.EstudianteCedula}", fontTexto, System.Drawing.Brushes.Black, x, y); y += lineHeight;
                    g.DrawString($"Estudiante  : {nombreEstudiante}", fontNegrita, System.Drawing.Brushes.Black, x, y); y += lineHeight;
                    g.DrawString($"Concepto    : {nombreConcepto}", fontTexto, System.Drawing.Brushes.Black, x, y); y += lineHeight;

                    if (!string.IsNullOrWhiteSpace(pago.Observaciones))
                    {
                        g.DrawString($"Nota        : {pago.Observaciones}", fontTexto, System.Drawing.Brushes.DarkSlateGray, x, y);
                        y += lineHeight;
                    }

                    y += 6;
                    g.DrawLine(System.Drawing.Pens.LightGray, x, y, 520, y);
                    y += 12;

                    // Detalle de Abonos
                    g.DrawString("DETALLE DE PAGOS / ABONOS:", fontSeccion, System.Drawing.Brushes.Black, x, y);
                    y += lineHeight + 4;

                    foreach (var d in detalles)
                    {
                        string metodo = string.IsNullOrWhiteSpace(d.MetodoNombre) ? "Pago" : d.MetodoNombre;
                        string refStr = !string.IsNullOrWhiteSpace(d.Referencia) ? $" (Ref: {d.Referencia})" : "";

                        g.DrawString($"• {metodo}{refStr}", fontTexto, System.Drawing.Brushes.Black, x + 10, y);
                        g.DrawString($"{d.Monto:N2} {d.MonedaCodigo}", fontNegrita, System.Drawing.Brushes.Black, 400, y);
                        y += lineHeight;
                    }

                    y += 6;
                    g.DrawLine(System.Drawing.Pens.LightGray, x, y, 520, y);
                    y += 12;

                    // Total
                    g.DrawString("TOTAL PAGADO:", fontTitulo, System.Drawing.Brushes.Black, x + 10, y);
                    g.DrawString($"{pago.Total:N2} {pago.MonedaCodigo}", fontTitulo, System.Drawing.Brushes.DarkGreen, 360, y);
                    y += 35;

                    g.DrawLine(System.Drawing.Pens.DarkBlue, x, y, 520, y);
                    y += 15;

                    // Pie
                    g.DrawString("¡Gracias por su confianza!", fontNegrita, System.Drawing.Brushes.DarkSlateGray, x + 150, y);
                    y += 18;
                    g.DrawString("Comprobante de pago generado electrónicamente.", fontSub, System.Drawing.Brushes.Gray, x + 115, y);

                    e.HasMorePages = false;
                };

                printDoc.Print();
            }
            catch
            {
                // Si la impresora virtual de PDF presenta algún bloqueo temporal, 
                // no interrumpe el flujo principal del cobro ni la impresión física
            }
        }

        /// <summary>
        /// Formatea el texto optimizado para el corte de papel de 80mm.
        /// </summary>
        private static string ConstruirFacturaTexto(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles)
        {
            var sb = new StringBuilder();

            sb.AppendLine("                 CEVENCA");
            sb.AppendLine("           Rif: J-507629082");
            sb.AppendLine("       Registro MPPE: 0016-1904");
            sb.AppendLine("       0414-4281527 / 0426-2355934");
            sb.AppendLine("    Av. Las Ferias, C.C. Isora, Mezz.");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("             RECIBO DE PAGO");
            sb.AppendLine($" Recibo Nro : {pago.NumeroRecibo}");
            sb.AppendLine($" Fecha/Hora : {pago.FechaPago:dd/MM/yyyy hh:mm tt}");
            sb.AppendLine($" Cédula     : {pago.EstudianteCedula}");
            sb.AppendLine($" Estudiante : {nombreEstudiante}");
            sb.AppendLine($" Concepto   : {nombreConcepto}");
            if (!string.IsNullOrWhiteSpace(pago.Observaciones))
            {
                sb.AppendLine($" Nota       : {pago.Observaciones}");
            }
            sb.AppendLine("----------------------------------------");
            sb.AppendLine(" DETALLE DE ABONOS:");

            foreach (var d in detalles)
            {
                string metodo = string.IsNullOrWhiteSpace(d.MetodoNombre) ? "Pago" : d.MetodoNombre;
                string refStr = !string.IsNullOrWhiteSpace(d.Referencia) ? $" (Ref: {d.Referencia})" : "";
                sb.AppendLine($" - {metodo}{refStr}: {d.Monto:N2} {d.MonedaCodigo}");
            }

            sb.AppendLine("----------------------------------------");
            sb.AppendLine($" TOTAL: {pago.Total:N2} {pago.MonedaCodigo}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("       ¡Gracias por su confianza!");
            sb.AppendLine("  Conserve este comprobante para control.");
            sb.AppendLine("\n\n");

            return sb.ToString();
        }

        /// <summary>
        /// Envía el texto a la impresora térmica POS-80.
        /// </summary>
        private static void ImprimirTextoDirecto(string texto, string printerName)
        {
            try
            {
                using var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = printerName;

                if (!printDoc.PrinterSettings.IsValid)
                {
                    printDoc.PrinterSettings.PrinterName = new PrinterSettings().PrinterName;
                }

                printDoc.PrintPage += (sender, e) =>
                {
                    using var fuente = new System.Drawing.Font("Lucida Console", 8.5f, System.Drawing.FontStyle.Regular);
                    e.Graphics!.DrawString(texto, fuente, System.Drawing.Brushes.Black, new PointF(5, 5));
                    e.HasMorePages = false;
                };

                printDoc.Print();
            }
            catch (Exception ex)
            {
                throw new Exception($"No se pudo imprimir en '{printerName}'. Verifique que esté conectada.\nDetalle: {ex.Message}");
            }
        }
    }
}