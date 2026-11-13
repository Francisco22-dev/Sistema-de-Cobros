using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public partial class CierreCajaWindow : Window
    {
        private readonly CajaService _cajaService = new CajaService();
        private readonly MantenimientoService _mantenimientoService = new MantenimientoService();
        public bool CajaCerradaExitosamente { get; private set; } = false;

        private List<ArqueoItem> _arqueoItems = new List<ArqueoItem>();
        private List<(string Curso, int Cantidad)> _inscripcionesPorCurso = new List<(string, int)>();
        private Caja? _cajaActual;

        // Bandera de control para no duplicar la impresión
        private bool _arqueoImpresoManualmente = false;

        public CierreCajaWindow()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                _cajaActual = _cajaService.ObtenerCajaAbierta();
                if (_cajaActual != null)
                {
                    string monSimbolo = _cajaActual.SaldoInicialMoneda == "USD" ? "$" : "Bs";
                    txtFondoInicial.Text = $"Fondo Inicial con el que abrió la caja: {_cajaActual.SaldoInicial:N2} {monSimbolo}";
                }

                // 1. Ingresos del turno real
                _arqueoItems = _cajaService.GenerarArqueo();
                dgArqueo.ItemsSource = _arqueoItems;

                decimal fisicoBs = _arqueoItems.Where(a => a.Moneda == "BS" && a.Metodo.Contains("Efectivo")).Sum(a => a.Total);
                decimal bancoBs = _arqueoItems.Where(a => a.Moneda == "BS" && !a.Metodo.Contains("Efectivo")).Sum(a => a.Total);

                decimal fisicoUsd = _arqueoItems.Where(a => (a.Moneda == "BCV" || a.Moneda == "USD") && (a.Metodo.Contains("Divisas") || a.Metodo.Contains("Efectivo"))).Sum(a => a.Total);
                decimal bancoUsd = _arqueoItems.Where(a => (a.Moneda == "BCV" || a.Moneda == "USD") && a.Metodo.Contains("Zelle")).Sum(a => a.Total);

                txtFisicoBs.Text = $"Físico: {fisicoBs:N2} BS";
                txtBancoBs.Text = $"Banco: {bancoBs:N2} BS";
                txtTotalBs.Text = $"TOTAL BS: {fisicoBs + bancoBs:N2} BS";

                txtFisicoUsd.Text = $"Físico: {fisicoUsd:N2} $";
                txtBancoUsd.Text = $"Banco: {bancoUsd:N2} $";
                txtTotalUsd.Text = $"TOTAL $: {fisicoUsd + bancoUsd:N2} $";

                // 2. Inscripciones del turno
                _inscripcionesPorCurso = _cajaService.ObtenerInscripcionesDelTurno();
                int totalIns = _inscripcionesPorCurso.Sum(x => x.Cantidad);
                txtTotalInscripciones.Text = $"Total de Inscripciones Hoy: {totalIns}";

                var lineasCursos = _inscripcionesPorCurso.Select(x => $"• {x.Curso}: {x.Cantidad} alumno(s)").ToList();
                if (lineasCursos.Count == 0) lineasCursos.Add("• No hubo inscripciones cobradas en este turno.");
                lstInscripcionesCursos.ItemsSource = lineasCursos;
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError("Error cargando arqueo: " + ex.Message);
            }
        }

        private string ObtenerRutaDestinoPdf()
        {
            string escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string carpeta = Path.Combine(escritorio, "arqueo de caja");

            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            string nombrePdf = $"Arqueo_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return Path.Combine(carpeta, nombrePdf);
        }

        private void GenerarPdfArqueo(string rutaArchivoPdf)
        {
            using var printDoc = new PrintDocument();

            string? pdfPrinter = PrinterSettings.InstalledPrinters.Cast<string>()
                .FirstOrDefault(p => p.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0);

            printDoc.PrinterSettings.PrinterName = pdfPrinter ?? "Microsoft Print to PDF";
            printDoc.PrinterSettings.PrintToFile = true;
            printDoc.PrinterSettings.PrintFileName = rutaArchivoPdf;

            printDoc.PrintPage += (sender, e) =>
            {
                var g = e.Graphics!;
                using var fontTitulo = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);
                using var fontSub = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Regular);
                using var fontSeccion = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold);
                using var fontNegrita = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
                using var fontTexto = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);

                float x = 50;
                float y = 50;
                float lineHeight = 20;

                // 1. Encabezado
                g.DrawString("CEVENCA", fontTitulo, System.Drawing.Brushes.DarkBlue, x, y);
                y += 26;
                g.DrawString("Rif: J-507629082 | Registro MPPE: 0016-1904", fontSub, System.Drawing.Brushes.Gray, x, y);
                y += 16;
                g.DrawString("COMPROBANTE OFICIAL DE CIERRE Y ARQUEO DE CAJA", fontSeccion, System.Drawing.Brushes.Black, x, y);
                y += 24;

                g.DrawLine(System.Drawing.Pens.DarkBlue, x, y, 520, y);
                y += 12;

                // 2. Información del Turno
                g.DrawString($"Fecha de Apertura : {_cajaActual?.FechaApertura:dd/MM/yyyy hh:mm tt}", fontTexto, System.Drawing.Brushes.Black, x, y); y += lineHeight;
                g.DrawString($"Fecha de Cierre   : {DateTime.Now:dd/MM/yyyy hh:mm tt}", fontTexto, System.Drawing.Brushes.Black, x, y); y += lineHeight;
                g.DrawString($"Cajero Responsable: {_cajaActual?.UsuarioApertura}", fontTexto, System.Drawing.Brushes.Black, x, y); y += lineHeight;
                g.DrawString($"Fondo Inicial     : {_cajaActual?.SaldoInicial:N2} {_cajaActual?.SaldoInicialMoneda}", fontNegrita, System.Drawing.Brushes.DarkSlateGray, x, y); y += lineHeight + 8;

                g.DrawLine(System.Drawing.Pens.LightGray, x, y, 520, y);
                y += 12;

                // 3. Tabla de Ingresos
                g.DrawString("INGRESOS GENERADOS EN EL TURNO (RECAUDACIÓN):", fontSeccion, System.Drawing.Brushes.Black, x, y);
                y += lineHeight + 4;

                if (_arqueoItems.Count == 0)
                {
                    g.DrawString("• No se registraron cobros durante este turno.", fontTexto, System.Drawing.Brushes.Gray, x + 10, y);
                    y += lineHeight;
                }
                else
                {
                    foreach (var item in _arqueoItems)
                    {
                        g.DrawString($"• {item.Metodo}", fontTexto, System.Drawing.Brushes.Black, x + 10, y);
                        g.DrawString($"{item.Total:N2} {item.Moneda}", fontNegrita, System.Drawing.Brushes.Black, 380, y);
                        y += lineHeight;
                    }
                }
                y += 6;
                g.DrawLine(System.Drawing.Pens.LightGray, x, y, 520, y);
                y += 10;

                // 4. Totales Consolidados
                g.DrawString(txtTotalBs.Text, fontNegrita, System.Drawing.Brushes.DarkOrange, x + 10, y); y += lineHeight;
                g.DrawString(txtTotalUsd.Text, fontNegrita, System.Drawing.Brushes.DarkGreen, x + 10, y); y += lineHeight + 10;

                g.DrawLine(System.Drawing.Pens.LightGray, x, y, 520, y);
                y += 12;

                // 5. Inscripciones Cobradas Hoy
                int totalIns = _inscripcionesPorCurso.Sum(k => k.Cantidad);
                g.DrawString($"INSCRIPCIONES COBRADAS HOY (TOTAL: {totalIns})", fontSeccion, System.Drawing.Brushes.Black, x, y);
                y += lineHeight + 4;

                if (_inscripcionesPorCurso.Count == 0)
                {
                    g.DrawString("• No hubo cobros de inscripción en este turno.", fontTexto, System.Drawing.Brushes.Gray, x + 10, y);
                    y += lineHeight;
                }
                else
                {
                    foreach (var ins in _inscripcionesPorCurso)
                    {
                        g.DrawString($"* {ins.Curso}", fontTexto, System.Drawing.Brushes.Black, x + 10, y);
                        g.DrawString($"{ins.Cantidad} alumno(s)", fontNegrita, System.Drawing.Brushes.Black, 380, y);
                        y += lineHeight;
                    }
                }

                y += 45;

                // 6. Firmas de Control
                g.DrawLine(System.Drawing.Pens.Black, x + 20, y, x + 180, y);
                g.DrawLine(System.Drawing.Pens.Black, x + 280, y, x + 440, y);
                y += 6;
                g.DrawString("Firma del Cajero", fontSub, System.Drawing.Brushes.Black, x + 55, y);
                g.DrawString("Firma de Administración", fontSub, System.Drawing.Brushes.Black, x + 295, y);

                e.HasMorePages = false;
            };

            printDoc.Print();
        }

        private void BtnExportarDigital_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string rutaPdf = ObtenerRutaDestinoPdf();
                GenerarPdfArqueo(rutaPdf);
                _arqueoImpresoManualmente = true; // Se marca como impreso manualmente

                AlertaWindow.MostrarExito($"Arqueo guardado en PDF exitosamente en el Escritorio:\n{rutaPdf}");

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = rutaPdf, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al generar el PDF del arqueo:\n{ex.Message}");
            }
        }

        private void BtnConfirmarCierre_Click(object sender, RoutedEventArgs e)
        {
            if (AlertaWindow.SolicitarConfirmacion("¿Desea cerrar la caja definitivamente por el día de hoy?", "Cierre de Caja"))
            {
                try
                {
                    string rutaPdf = ObtenerRutaDestinoPdf();

                    // Regla: Solo genera el PDF al cerrar si el usuario NO lo imprimió antes manualmente
                    if (!_arqueoImpresoManualmente)
                    {
                        GenerarPdfArqueo(rutaPdf);
                    }

                    _cajaService.CerrarCaja();
                    CajaCerradaExitosamente = true;

                    string mensaje = _arqueoImpresoManualmente
                        ? "La caja ha sido cerrada correctamente."
                        : $"La caja ha sido cerrada correctamente.\nEl arqueo en PDF se guardó en:\n{rutaPdf}";

                    AlertaWindow.MostrarExito(mensaje);

                    // AVISO DE RESPALDO DE BASE DE DATOS POST-CIERRE
                    if (AlertaWindow.SolicitarConfirmacion("¿Desea realizar el respaldo de seguridad de la base de datos ahora?", "Respaldo Recomendado"))
                    {
                        var saveDialog = new SaveFileDialog
                        {
                            Title = "Guardar Respaldo de Base de Datos",
                            FileName = $"Respaldo_Cierre_{DateTime.Now:yyyyMMdd_HHmm}.sql",
                            Filter = "Archivo SQL (*.sql)|*.sql"
                        };

                        if (saveDialog.ShowDialog() == true)
                        {
                            _mantenimientoService.GenerarRespaldo(saveDialog.FileName);
                            AlertaWindow.MostrarExito("Respaldo guardado exitosamente. Puede retirarse.");
                        }
                    }

                    this.Close();
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}