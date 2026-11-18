using CapaDatos;
using Entidades;
using MySqlConnector;
using System;
using System.Collections.Generic;

namespace CapaNegocio
{
    public class InscripcionService
    {
        private readonly InscripcionRepository _repository;
        private readonly PagoRepository _pagoRepository;
        private readonly AuditorService _auditoria;

        public InscripcionService()
        {
            _repository = new InscripcionRepository();
            _pagoRepository = new PagoRepository();
            _auditoria = new AuditorService();
        }

        public List<InscripcionInfo> ObtenerInscripcionesActivas() => _repository.ObtenerInscripcionesActivas();
        public List<InscripcionInfo> ObtenerTodasLasInscripciones(string filtro = "", string estado = "") => _repository.ObtenerTodasLasInscripciones(filtro, estado);

        public bool TieneDeudaPendiente(string estudianteCedula)
        {
            return _pagoRepository.TieneDeuda(estudianteCedula, DateTime.Now.Date);
        }

        public void TrasladarEstudianteDeGrupo(int inscripcionId, int nuevoGrupoId, string grupoAnteriorCodigo, string grupoNuevoCodigo, string observacionActual, string? motivo)
        {
            try
            {
                // Regla 3: Anexar nota histórica automática en Observaciones
                string notaTraslado = $"Trasladado de {grupoAnteriorCodigo} a {grupoNuevoCodigo} el {DateTime.Now:dd/MM/yyyy}." +
                                      (!string.IsNullOrWhiteSpace(motivo) ? $" Motivo: {motivo.Trim()}." : "");

                string observacionFinal = string.IsNullOrWhiteSpace(observacionActual)
                    ? notaTraslado
                    : $"{observacionActual} | {notaTraslado}";

                _repository.CambiarGrupo(inscripcionId, nuevoGrupoId, observacionFinal);

                _auditoria.Registrar("TRASLADO_GRUPO", "Inscripciones", inscripcionId.ToString(),
                    $"Grupo anterior: {grupoAnteriorCodigo}", $"Nuevo grupo: {grupoNuevoCodigo} | {notaTraslado}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("TRASLADO_GRUPO", "Inscripciones", inscripcionId.ToString(), null, ex.Message, false);
                throw;
            }
        }

        public void CambiarEstadoInscripcion(int inscripcionId, string nuevoEstado, string? motivo)
        {
            try
            {
                _repository.ActualizarEstado(inscripcionId, nuevoEstado, motivo);
                _auditoria.Registrar("UPDATE_ESTADO", "Inscripciones", inscripcionId.ToString(), null,
                    $"Estado: {nuevoEstado} | Motivo: {motivo ?? "Sin motivo"}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("UPDATE_ESTADO", "Inscripciones", inscripcionId.ToString(), null, ex.Message, false);
                throw;
            }
        }

        public void RegistrarInscripcion(string estudianteCedula, int grupoId, string? observaciones = null)
        {
            try
            {
                if (TieneDeudaPendiente(estudianteCedula))
                {
                    throw new InvalidOperationException("No se puede inscribir: El estudiante tiene pagos vencidos pendientes.");
                }

                var inscripcion = new Inscripcion(estudianteCedula, grupoId)
                {
                    Observaciones = observaciones
                };

                _repository.Agregar(inscripcion);
                _auditoria.Registrar("INSERT", "Inscripciones", null, null,
                    $"Cédula:{estudianteCedula}, GrupoId:{grupoId}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("INSERT", "Inscripciones", null, null,
                    $"Cédula:{estudianteCedula}, GrupoId:{grupoId} - Rechazado: {ex.Message}", false);
                throw;
            }
        }
        // Agrega estos métodos en CapaNegocio/InscripcionService.cs:

        public (decimal TotalPagadoClasesUSD, int ClasesVistas, decimal DeudaUSD, decimal SaldoAdelantadoUSD, bool PagoMatricula)
            ObtenerBalanceEstudianteEnCurso(int inscripcionId, int grupoId, decimal precioClase)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            // 1. Clases impartidas en el grupo hasta hoy
            int clasesVistas = 0;
            using (var cmdClases = new MySqlCommand(
                "SELECT COUNT(*) FROM CalendarioClases WHERE GrupoId = @grupoId AND FechaClase <= CURDATE() AND Estado NOT IN ('Nula', 'Festivo')", conn))
            {
                cmdClases.Parameters.AddWithValue("@grupoId", grupoId);
                clasesVistas = Convert.ToInt32(cmdClases.ExecuteScalar());
            }

            // 2. Total pagado en clases
            decimal totalPagadoClases = 0;
            using (var cmdPagos = new MySqlCommand(
                "SELECT COALESCE(SUM(TotalEnVes / COALESCE((SELECT tc.Tasa FROM TasasCambio tc WHERE tc.Fecha = DATE(p.FechaPago) AND tc.MonedaOrigen = 'BCV' LIMIT 1), 1)), 0) " +
                "FROM Pagos p WHERE p.InscripcionId = @insId AND p.Estado = 'Vigente' AND p.ConceptoId IN (2, 3)", conn))
            {
                cmdPagos.Parameters.AddWithValue("@insId", inscripcionId);
                totalPagadoClases = Convert.ToDecimal(cmdPagos.ExecuteScalar());
            }

            // 3. Verificó si pagó matrícula
            bool pagoMatricula = false;
            using (var cmdMat = new MySqlCommand("SELECT COUNT(*) FROM Pagos WHERE InscripcionId = @insId AND Estado = 'Vigente' AND ConceptoId = 1", conn))
            {
                cmdMat.Parameters.AddWithValue("@insId", inscripcionId);
                pagoMatricula = Convert.ToInt32(cmdMat.ExecuteScalar()) > 0;
            }

            decimal costoClasesVistas = clasesVistas * precioClase;
            decimal deuda = Math.Max(0, costoClasesVistas - totalPagadoClases);
            decimal saldoAdelantado = Math.Max(0, totalPagadoClases - costoClasesVistas);

            return (totalPagadoClases, clasesVistas, deuda, saldoAdelantado, pagoMatricula);
        }

        public void ProcesarCambioDeCurso(
            InscripcionInfo inscripcionActual,
            int nuevoGrupoId,
            string nuevoGrupoCodigo,
            string nuevoCursoNombre,
            bool transferirTodoCredito,
            bool convalidarInscripcion,
            bool perdonarDeuda,
            decimal deudaUSD,
            string motivo,
            string usuarioAdminAutoriza)
        {
            string fechaHoy = DateTime.Now.ToString("dd/MM/yyyy");

            // Construcción de notas históricas
            string detalleDeuda = deudaUSD > 0
                ? (perdonarDeuda ? $"Deuda de {deudaUSD:N2} $ PERDONADA." : $"Deuda de {deudaUSD:N2} $ NO perdonada (pendiente).")
                : "Sin deuda previa.";

            string notaOrigen = $"Cambio de carrera a '{nuevoCursoNombre}' ([{nuevoGrupoCodigo}]) el {fechaHoy}. {detalleDeuda} Motivo: {motivo}. Autorizado por: {usuarioAdminAutoriza}";
            string notaDestino = $"Inscripción por traslado desde '{inscripcionActual.CursoNombre}'. Crédito clases transferido: {(transferirTodoCredito ? "SÍ" : "NO")}. Matrícula convalidada: {(convalidarInscripcion ? "SÍ" : "NO")}.";

            _repository.EjecutarCambioDeCurso(
                inscripcionActual.InscripcionId,
                nuevoGrupoId,
                inscripcionActual.Cedula,
                transferirTodoCredito,
                convalidarInscripcion,
                notaOrigen,
                notaDestino);

            // Auditoría estricta contra malversación
            string detalleAuditoria = $"Cambio de Carrera para {inscripcionActual.NombreCompleto} ({inscripcionActual.Cedula}). " +
                                      $"De: {inscripcionActual.CursoNombre} -> A: {nuevoCursoNombre} ([{nuevoGrupoCodigo}]). " +
                                      $"Deuda previa: {deudaUSD:N2} $ ({(perdonarDeuda ? "PERDONADA/CONDONADA" : "COBRABLE")}). " +
                                      $"Autorizado por Administrador: '{usuarioAdminAutoriza}'. Motivo: {motivo}";

            _auditoria.Registrar("CAMBIO_CURSO", "Inscripciones", inscripcionActual.InscripcionId.ToString(),
                $"Curso anterior: {inscripcionActual.CursoNombre}", detalleAuditoria, true);
        }
        // Agrega estos métodos en CapaNegocio/InscripcionService.cs:

        public (bool Existe, bool EstaActiva, string EstadoPrevio, string GrupoCodigo, string CursoNombre)
            ValidarHistorialInscripcion(string cedula, string cursoCodigo)
        {
            return _repository.VerificarHistorialEstudianteEnCurso(cedula, cursoCodigo);
        }

        public void ProcesarCambioCursoPorClases(
            InscripcionInfo inscripcionActual,
            int nuevoGrupoId,
            string nuevoGrupoCodigo,
            Curso cursoDestino,
            int clasesAReconocer,
            bool cobrarDiferenciaPrecio,
            decimal precioClaseOrigen,
            bool perdonarDeuda,
            decimal deudaUSD,
            string motivo,
            string usuarioAdminAutoriza)
        {
            decimal precioClaseDestino = cursoDestino.PrecioMensualidad;
            decimal montoCreditoFinalUSD = 0m;

            // LÓGICA DE RECONOCIMIENTO DE CLASES (1 a 1)
            if (clasesAReconocer > 0)
            {
                if (precioClaseDestino <= precioClaseOrigen)
                {
                    // Decisión 3: Se le reconocen las N clases completas, no se devuelven saldos sobrantes
                    montoCreditoFinalUSD = clasesAReconocer * precioClaseDestino;
                }
                else
                {
                    // Decisión 4: Curso nuevo más caro
                    if (cobrarDiferenciaPrecio)
                    {
                        // Solo se le transfiere el valor que pagó en el anterior, dejando la diferencia como deuda
                        montoCreditoFinalUSD = clasesAReconocer * precioClaseOrigen;
                    }
                    else
                    {
                        // Se le reconocen las N clases al 100% en el nuevo curso sin cobrarle extra
                        montoCreditoFinalUSD = clasesAReconocer * precioClaseDestino;
                    }
                }
            }

            // Decisión 5: Matrícula exonerada automáticamente
            decimal matriculaConvalidadaUSD = cursoDestino.PrecioInscripcion;

            string fechaHoy = DateTime.Now.ToString("dd/MM/yyyy");
            string detalleDeuda = deudaUSD > 0
                ? (perdonarDeuda ? $"Deuda de {deudaUSD:N2} $ PERDONADA." : $"Deuda de {deudaUSD:N2} $ NO perdonada (pendiente).")
                : "Sin deuda previa.";

            string notaOrigen = $"Cambio de carrera a '{cursoDestino.Nombre}' ([{nuevoGrupoCodigo}]) el {fechaHoy}. {detalleDeuda} Motivo: {motivo}. Autorizado por: {usuarioAdminAutoriza}";
            string notaDestino = $"Inscripción por traslado desde '{inscripcionActual.CursoNombre}'. Se reconocieron {clasesAReconocer} clase(s) ({montoCreditoFinalUSD:N2} $ acreditados). Matrícula convalidada al 100%.";

            _repository.EjecutarCambioCursoPorClases(
                inscripcionActual.InscripcionId,
                nuevoGrupoId,
                inscripcionActual.Cedula,
                montoCreditoFinalUSD,
                matriculaConvalidadaUSD,
                notaOrigen,
                notaDestino,
                inscripcionActual.CursoNombre);

            // Auditoría estricta contra malversación
            string detalleAuditoria = $"Cambio de Carrera para {inscripcionActual.NombreCompleto} ({inscripcionActual.Cedula}). " +
                                      $"De: {inscripcionActual.CursoNombre} -> A: {cursoDestino.Nombre} ([{nuevoGrupoCodigo}]). " +
                                      $"Clases reconocidas: {clasesAReconocer}. Matrícula exonerada: {matriculaConvalidadaUSD:N2} $. " +
                                      $"Deuda previa: {deudaUSD:N2} $ ({(perdonarDeuda ? "PERDONADA" : "COBRABLE")}). " +
                                      $"Autorizado por Administrador: '{usuarioAdminAutoriza}'. Motivo: {motivo}";

            _auditoria.Registrar("CAMBIO_CURSO_CLASES", "Inscripciones", inscripcionActual.InscripcionId.ToString(),
                $"Curso anterior: {inscripcionActual.CursoNombre}", detalleAuditoria, true);
        }
    }
}