using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public static class ImpresorRecibo
    {
        public static void Imprimir(Pago pago, string nombreEstudiante, string nombreConcepto, List<PagoDetalle> detalles)
        {
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument
                {
                    PageWidth = 302,
                    PagePadding = new Thickness(10),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center
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

                // NUEVO: DESGLOSE DE PAGOS EN SU MONEDA REAL
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

                printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Recibo {pago.NumeroRecibo}");
            }
        }
    }
}