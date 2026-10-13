using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    /// <summary>
    /// Utilidad nativa de WPF para generar e imprimir recibos térmicos de 80mm.
    /// </summary>
    public static class ImpresorRecibo
    {
        public static void Imprimir(Pago pago, string nombreEstudiante, string nombreConcepto, string metodoPago)
        {
            PrintDialog printDialog = new PrintDialog();

            // Preguntar al usuario a qué impresora enviarlo
            if (printDialog.ShowDialog() == true)
            {
                // 80mm son aproximadamente 302 píxeles en WPF (96 DPI)
                FlowDocument doc = new FlowDocument
                {
                    PageWidth = 302,
                    PagePadding = new Thickness(10),
                    FontFamily = new FontFamily("Consolas"), // Fuente monoespaciada ideal para tickets
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center
                };

                // Encabezado
                doc.Blocks.Add(new Paragraph(new Run("CEVENCA")) { FontWeight = FontWeights.Bold, FontSize = 16 });
                doc.Blocks.Add(new Paragraph(new Run("RIF: J-00000000-0\nTel: (0000) 000-0000")) { FontSize = 10, Margin = new Thickness(0, 0, 0, 15) });

                // Título del Recibo
                doc.Blocks.Add(new Paragraph(new Run("RECIBO DE PAGO")) { FontWeight = FontWeights.Bold, FontSize = 14 });
                doc.Blocks.Add(new Paragraph(new Run($"Nro: {pago.NumeroRecibo}")) { Margin = new Thickness(0, 0, 0, 15) });

                // Cuerpo del recibo (Alineado a la izquierda)
                var cuerpo = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 0, 0, 15) };
                cuerpo.Inlines.Add(new Run($"Fecha: {pago.FechaPago:dd/MM/yyyy HH:mm}\n"));
                cuerpo.Inlines.Add(new Run($"Cédula: {pago.EstudianteCedula}\n"));
                cuerpo.Inlines.Add(new Run($"Estudiante: {nombreEstudiante}\n"));
                cuerpo.Inlines.Add(new Run(new string('-', 32) + "\n"));
                cuerpo.Inlines.Add(new Run($"Concepto: {nombreConcepto}\n"));
                cuerpo.Inlines.Add(new Run($"Método: {metodoPago}\n"));
                cuerpo.Inlines.Add(new Run($"Moneda: {pago.MonedaCodigo}\n"));
                doc.Blocks.Add(cuerpo);

                // Totales (Centrado y grande)
                var total = new Paragraph { Margin = new Thickness(0, 0, 0, 20) };
                total.Inlines.Add(new Run("TOTAL PAGADO\n") { FontSize = 12 });
                total.Inlines.Add(new Run($"{pago.Total:N2} {pago.MonedaCodigo}") { FontSize = 18, FontWeight = FontWeights.Bold });
                doc.Blocks.Add(total);

                // Pie de página
                doc.Blocks.Add(new Paragraph(new Run("¡Gracias por su pago!\nConserve este recibo para cualquier reclamo."))
                {
                    FontSize = 10,
                    FontStyle = FontStyles.Italic
                });

                // Enviar a la impresora
                printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Recibo {pago.NumeroRecibo}");
            }
        }
    }
}