using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public static class ImpresorRecibo
    {
        public static void Imprimir(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles)
        {
            try
            {
                FlowDocument doc = new FlowDocument
                {
                    PageWidth = 302, // Ancho exacto de impresora térmica 80mm
                    PagePadding = new Thickness(10),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    ColumnWidth = 302
                };

                doc.Blocks.Add(new Paragraph(new Run("CEVENCA")) { FontWeight = FontWeights.Bold, FontSize = 16 });
                doc.Blocks.Add(new Paragraph(new Run("RIF: J-00000000-0\nTel: (0000) 000-0000")) { FontSize = 10, Margin = new Thickness(0, 0, 0, 15) });

                doc.Blocks.Add(new Paragraph(new Run("RECIBO DE PAGO")) { FontWeight = FontWeights.Bold, FontSize = 14 });
                doc.Blocks.Add(new Paragraph(new Run($"Nro: {pago.NumeroRecibo}")) { Margin = new Thickness(0, 0, 0, 15) });

                var cuerpo = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 0, 0, 15) };
                cuerpo.Inlines.Add(new Run($"Fecha: {pago.FechaPago:dd/MM/yyyy HH:mm}\n"));
                cuerpo.Inlines.Add(new Run($"Cédula: {pago.EstudianteCedula}\n"));
                cuerpo.Inlines.Add(new Run($"Estudiante: {nombreEstudiante}\n"));
                cuerpo.Inlines.Add(new Run($"Concepto: {nombreConcepto}\n"));
                cuerpo.Inlines.Add(new Run(new string('-', 32) + "\n"));

                cuerpo.Inlines.Add(new Run("DETALLE DE ABONOS:\n") { FontWeight = FontWeights.Bold });
                foreach (var d in detalles)
                {
                    cuerpo.Inlines.Add(new Run($"- {d.MetodoNombre}: {d.Monto:N2} {d.MonedaCodigo}\n"));
                }
                cuerpo.Inlines.Add(new Run(new string('-', 32) + "\n"));
                doc.Blocks.Add(cuerpo);

                var total = new Paragraph { Margin = new Thickness(0, 0, 0, 20) };
                total.Inlines.Add(new Run("TOTAL FACTURADO\n") { FontSize = 12 });
                total.Inlines.Add(new Run($"{pago.Total:N2} {pago.MonedaCodigo}") { FontSize = 18, FontWeight = FontWeights.Bold });
                doc.Blocks.Add(total);

                doc.Blocks.Add(new Paragraph(new Run("¡Gracias por su pago!\nConserve este recibo para cualquier reclamo.")) { FontSize = 10, FontStyle = FontStyles.Italic });

                // 1. GUARDADO DE IMAGEN (DIGITAL)
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Title = "Guardar Copia Digital del Recibo",
                    FileName = pago.NumeroRecibo, // Nombre por defecto
                    DefaultExt = ".png",
                    Filter = "Imagen PNG (*.png)|*.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    IDocumentPaginatorSource idpSourceImg = doc;
                    var paginatorImg = idpSourceImg.DocumentPaginator;
                    // Forzamos un alto fijo suficiente para la imagen digital
                    paginatorImg.PageSize = new Size(302, 700);

                    DocumentPage page = paginatorImg.GetPage(0);

                    // Creamos un lienzo visual con fondo blanco (para que no quede transparente/negro)
                    DrawingVisual visual = new DrawingVisual();
                    using (DrawingContext dc = visual.RenderOpen())
                    {
                        dc.DrawRectangle(Brushes.White, null, new Rect(page.Size));
                        dc.DrawRectangle(new VisualBrush(page.Visual), null, new Rect(page.Size));
                    }

                    RenderTargetBitmap rtb = new RenderTargetBitmap((int)page.Size.Width * 2, (int)page.Size.Height * 2, 192, 192, PixelFormats.Pbgra32);
                    rtb.Render(visual);

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    using (var fs = System.IO.File.Create(saveDialog.FileName))
                    {
                        encoder.Save(fs);
                    }
                }

                // 2. IMPRESIÓN FÍSICA SILENCIOSA A LA TÉRMICA (POS-80)
                PrintDialog printDialog = new PrintDialog();
                try
                {
                    var printServer = new LocalPrintServer();
                    // Buscar una impresora instalada cuyo nombre contenga "POS-80"
                    var posPrinter = printServer.GetPrintQueues().FirstOrDefault(q => q.Name.IndexOf("POS-80", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (posPrinter != null)
                    {
                        printDialog.PrintQueue = posPrinter;
                    }

                    // Altura infinita para que el rollo térmico corte donde termina el texto
                    IDocumentPaginatorSource idpSourcePrint = doc;
                    var paginatorPrint = idpSourcePrint.DocumentPaginator;
                    paginatorPrint.PageSize = new Size(302, 3000);

                    printDialog.PrintDocument(paginatorPrint, $"Recibo {pago.NumeroRecibo}");
                }
                catch
                {
                    // Si no tiene permisos de servidor de impresión, usamos la impresora por defecto de Windows
                    IDocumentPaginatorSource idpFallback = doc;
                    idpFallback.DocumentPaginator.PageSize = new Size(302, 3000);
                    printDialog.PrintDocument(idpFallback.DocumentPaginator, $"Recibo {pago.NumeroRecibo}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el recibo.\nDetalle: " + ex.Message, "Error de Impresora", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}