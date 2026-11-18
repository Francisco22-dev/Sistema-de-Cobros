using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public static class ImpresorRecibo
    {
        public static void Imprimir(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles, bool imprimirFisico, string printerName = "POS-80")
        {
            try
            {
                // 1. Guardar copia digital (PDF e HTML anti-corrupción)
                GuardarReciboDigital(pago, nombreEstudiante, nombreConcepto, detalles);

                // 2. Si el cajero confirmó imprimir en papel físico, envía a la POS-80
                if (imprimirFisico)
                {
                    string textoFactura = ConstruirFacturaTexto(pago, nombreEstudiante, nombreConcepto, detalles);
                    ImprimirTextoDirecto(textoFactura, printerName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Aviso de facturación:\n{ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void GuardarReciboDigital(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles)
        {
            string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string carpeta = Path.Combine(escritorio, "Recibos Digitales");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

            string nombreBase = string.Join("_", pago.NumeroRecibo.Split(Path.GetInvalidFileNameChars()));
            string rutaPdf = Path.Combine(carpeta, $"{nombreBase}.pdf");
            string rutaHtml = Path.Combine(carpeta, $"{nombreBase}.html");

            // Respaldo HTML (Formato 100% inmune a bloqueos de Windows y compartible)
            GenerarReciboHtml(pago, nombreEstudiante, nombreConcepto, detalles, rutaHtml);

            // Generar PDF usando spooler con verificación de cierre
            GenerarReciboPdfProtegido(pago, nombreEstudiante, nombreConcepto, detalles, rutaPdf);
        }

        private static void GenerarReciboHtml(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles, string rutaHtml)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Recibo " + pago.NumeroRecibo + "</title>");
                sb.AppendLine("<style>");
                sb.AppendLine("body { font-family: 'Courier New', monospace; width: 300px; margin: auto; padding: 15px; border: 1px dashed #ccc; }");
                sb.AppendLine(".center { text-align: center; } .bold { font-weight: bold; } .line { border-top: 1px dashed #000; margin: 8px 0; }");
                sb.AppendLine(".row { display: flex; justify-content: space-between; font-size: 13px; margin: 3px 0; }");
                sb.AppendLine("</style></head><body>");
                sb.AppendLine("<div class='center bold' style='font-size:18px;'>CEVENCA</div>");
                sb.AppendLine("<div class='center' style='font-size:11px;'>Rif: J-507629082<br>0414-4281527 / 0426-2355934<br>Av. Las Ferias, C.C. Isora, Mezz.</div>");
                sb.AppendLine("<div class='line'></div>");
                sb.AppendLine("<div class='center bold'>RECIBO DE PAGO</div>");
                sb.AppendLine("<div class='center bold'>" + pago.NumeroRecibo + "</div>");
                sb.AppendLine("<div class='row'><span>Fecha:</span><span>" + pago.FechaPago.ToString("dd/MM/yyyy hh:mm tt") + "</span></div>");
                sb.AppendLine("<div class='row'><span>Cédula:</span><span>" + pago.EstudianteCedula + "</span></div>");
                sb.AppendLine("<div class='row'><span>Alumno:</span><span class='bold'>" + nombreEstudiante + "</span></div>");
                sb.AppendLine("<div class='row'><span>Concepto:</span><span>" + nombreConcepto + "</span></div>");
                if (!string.IsNullOrWhiteSpace(pago.Observaciones))
                    sb.AppendLine("<div class='row'><span>Nota:</span><span>" + pago.Observaciones + "</span></div>");
                sb.AppendLine("<div class='line'></div>");
                sb.AppendLine("<div class='bold' style='font-size:12px;'>DETALLE DE ABONOS:</div>");
                foreach (var d in detalles)
                {
                    string m = string.IsNullOrWhiteSpace(d.MetodoNombre) ? "Pago" : d.MetodoNombre;
                    string r = !string.IsNullOrWhiteSpace(d.Referencia) ? " (Ref: " + d.Referencia + ")" : "";
                    sb.AppendLine("<div class='row'><span>• " + m + r + "</span><span>" + d.Monto.ToString("N2") + " " + d.MonedaCodigo + "</span></div>");
                }
                sb.AppendLine("<div class='line'></div>");
                sb.AppendLine("<div class='row bold' style='font-size:16px;'><span>TOTAL:</span><span>" + pago.Total.ToString("N2") + " " + pago.MonedaCodigo + "</span></div>");
                sb.AppendLine("<div class='line'></div>");
                sb.AppendLine("<div class='center' style='font-size:11px;'>¡Gracias por su confianza!<br>Comprobante oficial electrónico.</div>");
                sb.AppendLine("</body></html>");

                File.WriteAllText(rutaHtml, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private static void GenerarReciboPdfProtegido(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles, string rutaPdf)
        {
            try
            {
                if (File.Exists(rutaPdf))
                {
                    try { File.Delete(rutaPdf); } catch { }
                }

                using var printDoc = new PrintDocument();
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

                    float x = 50, y = 50, lineHeight = 20;

                    g.DrawString("CEVENCA", fontTitulo, System.Drawing.Brushes.DarkBlue, x, y); y += 26;
                    g.DrawString("Rif: J-507629082", fontSub, System.Drawing.Brushes.Gray, x, y); y += 16;
                    g.DrawString("0414-4281527 / 0426-2355934 • Av. Las Ferias, C.C. Isora, Mezz.", fontSub, System.Drawing.Brushes.Gray, x, y); y += 18;
                    g.DrawString("RECIBO DE PAGO DIGITAL", fontSeccion, System.Drawing.Brushes.Black, x, y); y += 22;

                    g.DrawLine(System.Drawing.Pens.DarkBlue, x, y, 520, y); y += 12;

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
                    g.DrawLine(System.Drawing.Pens.LightGray, x, y, 520, y); y += 12;

                    g.DrawString("DETALLE DE PAGOS / ABONOS:", fontSeccion, System.Drawing.Brushes.Black, x, y); y += lineHeight + 4;

                    foreach (var d in detalles)
                    {
                        string metodo = string.IsNullOrWhiteSpace(d.MetodoNombre) ? "Pago" : d.MetodoNombre;
                        string refStr = !string.IsNullOrWhiteSpace(d.Referencia) ? $" (Ref: {d.Referencia})" : "";
                        g.DrawString($"• {metodo}{refStr}", fontTexto, System.Drawing.Brushes.Black, x + 10, y);
                        g.DrawString($"{d.Monto:N2} {d.MonedaCodigo}", fontNegrita, System.Drawing.Brushes.Black, 400, y);
                        y += lineHeight;
                    }

                    y += 6;
                    g.DrawLine(System.Drawing.Pens.LightGray, x, y, 520, y); y += 12;

                    g.DrawString("TOTAL PAGADO:", fontTitulo, System.Drawing.Brushes.Black, x + 10, y);
                    g.DrawString($"{pago.Total:N2} {pago.MonedaCodigo}", fontTitulo, System.Drawing.Brushes.DarkGreen, 360, y); y += 35;

                    g.DrawLine(System.Drawing.Pens.DarkBlue, x, y, 520, y); y += 15;
                    g.DrawString("¡Gracias por su confianza!", fontNegrita, System.Drawing.Brushes.DarkSlateGray, x + 150, y);

                    e.HasMorePages = false;
                };

                printDoc.Print();

                // Espera activa para asegurar que Windows Spooler termine de escribir y libere el archivo
                int intentos = 0;
                while (intentos < 12)
                {
                    Thread.Sleep(200);
                    if (File.Exists(rutaPdf) && new FileInfo(rutaPdf).Length > 100)
                    {
                        try
                        {
                            using var fs = File.Open(rutaPdf, FileMode.Open, FileAccess.Read, FileShare.None);
                            break; // Archivo completamente liberado y no corrupto
                        }
                        catch (IOException) { }
                    }
                    intentos++;
                }
            }
            catch { }
        }

        private static string ConstruirFacturaTexto(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("                 CEVENCA");
            sb.AppendLine("           Rif: J-507629082");
            //sb.AppendLine("       Registro MPPE: 0016-1904");
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
                sb.AppendLine($" Nota       : {pago.Observaciones}");
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

        public static void ImprimirTextoDirecto(string texto, string printerName = "POS-80")
        {
            try
            {
                using var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = printerName;
                if (!printDoc.PrinterSettings.IsValid)
                    printDoc.PrinterSettings.PrinterName = new PrinterSettings().PrinterName;

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
                throw new Exception($"Error en POS-80: {ex.Message}");
            }
        }
    }
}