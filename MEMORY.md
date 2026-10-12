# MEMORY.md — Sistema de Cobros 2.0
Memoria del proyecto entre sesiones. Máximo ~50 líneas: resume o elimina lo que ya no
aporte.

## Estado actual
- Sistema preparado para red local (3 PCs) con MariaDB centralizada.
- Módulo de gestión de usuarios completo: login, CRUD, sesión, roles.
- Módulo de auditoría: registra todas las operaciones (INSERT, UPDATE, DELETE, LOGIN) con usuario, computadora y éxito/fallo.
- Ventana de login (LoginWindow) como punto de entrada.
- Ventana de gestión de usuarios (GestionUsuariosWindow) accesible solo para admin.
- Ventana de auditoría (AuditoriaWindow) accesible para admin y programador.
- Usuario "programador" oculto para admin, con mismos permisos, no eliminable.
- Usuario "admin" no eliminable ni desactivable.
- Contraseñas con hash SHA256.
- Compila con 0 errores, 0 advertencias.

## Decisiones (y por qué)
- Arquitectura en 3 capas (Entidades → CapaDatos → CapaNegocio → UI).
- Proyectos en net8.0 / net8.0-windows (SDK-style).
- `SesionService` como singleton para mantener la sesión activa.
- Usuario programador: username="programador", password="dev123".
- Usuario admin: username="admin", password="admin123".
- Soft delete: usuarios se desactivan (Activo=false) en lugar de eliminarse.
- Configuración de BD en App.config (IP, puerto, nombre PC).
- Script SQL manual en database/crear_tablas.sql.
- MySqlConnector como driver de base de datos.
- Paleta: azul (#2196F3), verde (#4CAF50), gris claro (#F5F5F5).

## Aprendizajes y errores a evitar
- `dotnet build` funciona (SDK-style). Antes fallaba con .NET Framework 4.8.
- Fechas: usar `DateTime.Now`, nunca UTC ni `toISOString()`.
- Las capas NO pueden usar `ImplicitUsings` ni `Nullable` si target es net48 (C# 7.3).
- Advertencias CS8604/CS8600/CS8618: usar `!` (null-forgiving) o hacer tipos nullable.
- ConfigurationManager requiere paquete NuGet en .NET 8.

## Base de datos (database/sistema_cobros_completo.sql) — VERSIÓN FINAL
- Script completo con 20 tablas + 3 vistas + 10 triggers + datos semilla. Compatible con MariaDB 10.x.
- Módulos cubiertos: seguridad/usuarios, estudiantes/cursos/grupos, multimoneda (VES/USD/EUR/USDT), cobranza con pagos mixtos, caja diaria, morosidad con calendario de clases y días festivos, devoluciones.
- Diseño cursos: `Cursos` (carrera) → `Grupos` (cohorte con fechas). Permite múltiples instancias del mismo curso.
- Pagos mixtos: tabla `PagoDetalles` vinculada a `Pagos` permite dividir un cobro en efectivo + transferencia + USDT.
- Pagos vinculados a `Inscripciones` (InscripcionId) para distinguir a qué cohorte pertenece cada cobro.
- Morosidad: `Pagos.FechaVencimiento` + `CalendarioClases` (Programada/Realizada/Nula/Festivo) + `DiasFestivos`. Vistas `VistaMorosidad` (calcula DiasEnMora, incluye suspendidos, resta devoluciones) y `VistaPasivoCorriente` (clases pagadas adelantado, usa TotalEnVes).
- Caja: `Caja` (apertura/cierre diario, SaldoInicialMoneda) + `MovimientosCaja` con categorías (Operativo, Directo, Personal, etc.).
- Triggers idempotentes (DROP IF EXISTS + CREATE): `trg_calcular_total_ves`, `trg_validar_total_pago`, `trg_validar_devolucion`, `trg_registrar_ajuste_devolucion`, `trg_validar_caja_abierta`, `trg_marcar_vencidos`, `trg_sincronizar_festivos`, `trg_actualizar_estado_devolucion`, `trg_validar_pago_ya_devuelto`, `trg_validar_tasa_moneda`, `trg_validar_fecha_clase`, `trg_validar_fecha_inscripcion`.
- Devoluciones: campo `TipoDevolucion` ('Neta' usa tasa original, 'ConCambio' usa tasa actual). Tabla `AjustesDevolucion` registra siempre (auditoría completa).
- `PagoDetalles.MonedaCodigo` debe coincidir con `Pagos.MonedaCodigo` (validado por trigger).
- `CalendarioClases` tiene UNIQUE (GrupoId, FechaClase) para evitar duplicados.
- `TasasCambio` tiene CHECK (Tasa > 0) y CHECK (MonedaOrigen != MonedaDestino).
- Índices idempotentes (DROP INDEX IF EXISTS + CREATE INDEX).
- `Pagos.TotalEnVes`: valor en bolívares del momento del pago. Necesario para devoluciones justas cuando la tasa cambia diariamente.
- Conceptos de pago: INSCRIPCION, MENSUALIDAD, SEMANAL, MATERIAL, CERTIFICACION, OTRO.
- Compatible con tablas existentes: `Usuarios` (Username PK) y `Auditoria` mantienen la estructura original.

## Próximos pasos
- Ejecutar script SQL completo en el servidor MariaDB antes de usar la app.
- Configurar App.config en cada PC con la IP del servidor y nombre de computadora.
- Considerar migración a MVVM cuando la UI crezga.
