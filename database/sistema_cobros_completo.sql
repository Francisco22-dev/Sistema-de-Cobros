-- ============================================
-- Sistema de Cobros 2.0 - Base de datos completa
-- MariaDB 10.4.x / phpMyAdmin
-- Ejecutar completo en una sola pasada
-- ============================================

CREATE DATABASE IF NOT EXISTS sistema_cobros
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE sistema_cobros;

-- ============================================
-- MÓDULO DE SEGURIDAD Y USUARIOS
-- ============================================

CREATE TABLE IF NOT EXISTS Roles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(20) NOT NULL UNIQUE,
    Descripcion VARCHAR(200)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Usuarios (
    Username VARCHAR(50) PRIMARY KEY,
    PasswordHash VARCHAR(64) NOT NULL,
    NombreCompleto VARCHAR(100) NOT NULL,
    Rol VARCHAR(20) NOT NULL,
    Email VARCHAR(100),
    Telefono VARCHAR(20),
    Activo BOOLEAN DEFAULT TRUE,
    UltimoAcceso DATETIME,
    IntentosFallidos INT DEFAULT 0,
    Bloqueado BOOLEAN DEFAULT FALSE,
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Auditoria (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Usuario VARCHAR(50) NOT NULL,
    Accion VARCHAR(50) NOT NULL,
    TablaAfectada VARCHAR(50) NOT NULL,
    RegistroId VARCHAR(50),
    DatosAnteriores TEXT,
    DatosNuevos TEXT,
    Computadora VARCHAR(100) NOT NULL,
    Ip VARCHAR(45),
    FechaHora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Exito BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;

-- ============================================
-- MÓDULO DE ESTUDIANTES Y CURSOS
-- ============================================

CREATE TABLE IF NOT EXISTS Estudiantes (
    Cedula VARCHAR(20) PRIMARY KEY,
    NombreCompleto VARCHAR(150) NOT NULL,
    Telefono VARCHAR(20) NOT NULL,
    Email VARCHAR(100),
    FechaNacimiento DATE,
    Direccion VARCHAR(255),
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Activo BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Cursos (
    Codigo VARCHAR(20) PRIMARY KEY,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(500),
    DuracionSemanas INT NOT NULL,
    PrecioInscripcion DECIMAL(12,2) DEFAULT 0,
    PrecioMensualidad DECIMAL(12,2) DEFAULT 0,
    Activo BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Grupos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CursoCodigo VARCHAR(20) NOT NULL,
    Codigo VARCHAR(30) NOT NULL UNIQUE,
    Nombre VARCHAR(100),
    FechaInicio DATE NOT NULL,
    FechaFin DATE,
    Estado ENUM('Planificado', 'En curso', 'Finalizado', 'Cancelado') DEFAULT 'Planificado',
    Activo BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (CursoCodigo) REFERENCES Cursos(Codigo)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Inscripciones (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EstudianteCedula VARCHAR(20) NOT NULL,
    GrupoId INT NOT NULL,
    FechaInscripcion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Estado ENUM('Activa', 'Suspendida', 'Finalizada', 'Cancelada') DEFAULT 'Activa',
    Observaciones VARCHAR(255),
    FOREIGN KEY (EstudianteCedula) REFERENCES Estudiantes(Cedula),
    FOREIGN KEY (GrupoId) REFERENCES Grupos(Id)
) ENGINE=InnoDB;

-- ============================================
-- MÓDULO DE MULTIMONEDA
-- ============================================

CREATE TABLE IF NOT EXISTS Monedas (
    Codigo VARCHAR(5) PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL,
    Simbolo VARCHAR(10) NOT NULL,
    EsPrincipal BOOLEAN DEFAULT FALSE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS TasasCambio (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Fecha DATE NOT NULL,
    MonedaOrigen VARCHAR(5) NOT NULL,
    MonedaDestino VARCHAR(5) NOT NULL,
    Tasa DECIMAL(15,6) NOT NULL,
    Fuente VARCHAR(50),
    RegistradoPor VARCHAR(50) NOT NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_tasa_diaria (Fecha, MonedaOrigen, MonedaDestino),
    FOREIGN KEY (MonedaOrigen) REFERENCES Monedas(Codigo),
    FOREIGN KEY (MonedaDestino) REFERENCES Monedas(Codigo),
    CHECK (Tasa > 0),
    CHECK (MonedaOrigen != MonedaDestino)
) ENGINE=InnoDB;

-- ============================================
-- MÓDULO DE COBRANZA Y FACTURACIÓN
-- ============================================

CREATE TABLE IF NOT EXISTS MetodosPago (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(20) UNIQUE NOT NULL,
    Nombre VARCHAR(50) NOT NULL,
    Activo BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS ConceptosPago (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Codigo VARCHAR(20) UNIQUE NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255),
    Activo BOOLEAN DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Pagos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    NumeroRecibo VARCHAR(30) UNIQUE NOT NULL,
    EstudianteCedula VARCHAR(20) NOT NULL,
    InscripcionId INT NOT NULL,
    ConceptoId INT NOT NULL,
    FechaPago DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaVencimiento DATE NOT NULL,
    Total DECIMAL(12,2) NOT NULL,
    TotalEnVes DECIMAL(12,2) NOT NULL,
    MonedaCodigo VARCHAR(5) NOT NULL,
    UsuarioRegistra VARCHAR(50) NOT NULL,
    Estado ENUM('Vigente', 'Vencido', 'Anulado', 'Devuelto') DEFAULT 'Vigente',
    Observaciones VARCHAR(500),
    FOREIGN KEY (EstudianteCedula) REFERENCES Estudiantes(Cedula),
    FOREIGN KEY (InscripcionId) REFERENCES Inscripciones(Id),
    FOREIGN KEY (ConceptoId) REFERENCES ConceptosPago(Id),
    FOREIGN KEY (MonedaCodigo) REFERENCES Monedas(Codigo)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS PagoDetalles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PagoId INT NOT NULL,
    MetodoPagoId INT NOT NULL,
    Monto DECIMAL(12,2) NOT NULL,
    MonedaCodigo VARCHAR(5) NOT NULL,
    TasaCambio DECIMAL(15,6) DEFAULT 1,
    MontoEnVes DECIMAL(12,2) NOT NULL,
    Referencia VARCHAR(100),
    FOREIGN KEY (PagoId) REFERENCES Pagos(Id),
    FOREIGN KEY (MetodoPagoId) REFERENCES MetodosPago(Id),
    FOREIGN KEY (MonedaCodigo) REFERENCES Monedas(Codigo)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Recibos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PagoId INT NOT NULL,
    Numero VARCHAR(30) UNIQUE NOT NULL,
    FechaEmision DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Impreso BOOLEAN DEFAULT FALSE,
    RutaDigital VARCHAR(500),
    FOREIGN KEY (PagoId) REFERENCES Pagos(Id)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS Devoluciones (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PagoId INT NOT NULL,
    Monto DECIMAL(12,2) NOT NULL,
    MonedaCodigo VARCHAR(5) NOT NULL,
    TipoDevolucion ENUM('Neta', 'ConCambio') NOT NULL DEFAULT 'Neta',
    TasaDevolucion DECIMAL(15,6),
    MontoEnVes DECIMAL(12,2) NOT NULL,
    DiferenciaVes DECIMAL(12,2) DEFAULT 0,
    Motivo VARCHAR(500) NOT NULL,
    FechaDevolucion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UsuarioRegistra VARCHAR(50) NOT NULL,
    Estado ENUM('Pendiente', 'Procesada', 'Rechazada') DEFAULT 'Pendiente',
    Observaciones VARCHAR(255),
    FOREIGN KEY (PagoId) REFERENCES Pagos(Id),
    FOREIGN KEY (MonedaCodigo) REFERENCES Monedas(Codigo)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS AjustesDevolucion (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    DevolucionId INT NOT NULL,
    PagoId INT NOT NULL,
    MontoOriginalVes DECIMAL(12,2) NOT NULL,
    MontoDevolucionVes DECIMAL(12,2) NOT NULL,
    DiferenciaVes DECIMAL(12,2) NOT NULL,
    TasaOriginal DECIMAL(15,6) NOT NULL,
    TasaDevolucion DECIMAL(15,6) NOT NULL,
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (DevolucionId) REFERENCES Devoluciones(Id),
    FOREIGN KEY (PagoId) REFERENCES Pagos(Id)
) ENGINE=InnoDB;

-- ============================================
-- MÓDULO DE CAJA
-- ============================================

CREATE TABLE IF NOT EXISTS Caja (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    FechaApertura DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaCierre DATETIME,
    UsuarioApertura VARCHAR(50) NOT NULL,
    UsuarioCierre VARCHAR(50),
    SaldoInicial DECIMAL(12,2) DEFAULT 0,
    SaldoInicialMoneda VARCHAR(5) DEFAULT 'VES',
    SaldoFinal DECIMAL(12,2),
    Estado ENUM('Abierta', 'Cerrada') DEFAULT 'Abierta',
    FOREIGN KEY (UsuarioApertura) REFERENCES Usuarios(Username),
    FOREIGN KEY (UsuarioCierre) REFERENCES Usuarios(Username)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS MovimientosCaja (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CajaId INT NOT NULL,
    Tipo ENUM('Ingreso', 'Egreso') NOT NULL,
    Monto DECIMAL(12,2) NOT NULL,
    MonedaCodigo VARCHAR(5) NOT NULL,
    Concepto VARCHAR(200) NOT NULL,
    Categoria VARCHAR(50),
    Referencia VARCHAR(100),
    FechaMovimiento DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UsuarioRegistra VARCHAR(50) NOT NULL,
    FOREIGN KEY (CajaId) REFERENCES Caja(Id),
    FOREIGN KEY (MonedaCodigo) REFERENCES Monedas(Codigo)
) ENGINE=InnoDB;

-- ============================================
-- MÓDULO DE CALENDARIO Y MOROSIDAD
-- ============================================

CREATE TABLE IF NOT EXISTS DiasFestivos (
    Fecha DATE PRIMARY KEY,
    Descripcion VARCHAR(200) NOT NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS CalendarioClases (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    GrupoId INT NOT NULL,
    FechaClase DATE NOT NULL,
    Estado ENUM('Programada', 'Realizada', 'Nula', 'Festivo') DEFAULT 'Programada',
    Observaciones VARCHAR(255),
    FOREIGN KEY (GrupoId) REFERENCES Grupos(Id),
    UNIQUE KEY uq_grupo_fecha (GrupoId, FechaClase)
) ENGINE=InnoDB;

-- ============================================
-- DATOS SEMILLA
-- ============================================

INSERT INTO Roles (Nombre, Descripcion) VALUES
('Admin', 'Acceso total al sistema'),
('Cajero', 'Gestión de cobros y caja'),
('Programador', 'Acceso técnico completo (rol oculto)')
ON DUPLICATE KEY UPDATE Nombre = Nombre;

INSERT INTO Usuarios (Username, PasswordHash, NombreCompleto, Rol) VALUES
('programador', '87274af01876341455b32d805946f272871bb42effa6604dccf28bb027afa82b', 'Programador', 'Programador'),
('admin', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Administrador', 'Admin')
ON DUPLICATE KEY UPDATE Username = Username;

INSERT INTO Monedas (Codigo, Nombre, Simbolo, EsPrincipal) VALUES
('VES', 'Bolívar Venezolano', 'Bs.', TRUE),
('USD', 'Dólar Estadounidense', '$', FALSE),
('EUR', 'Euro', '€', FALSE),
('USDT', 'Tether (USDT)', '₮', FALSE)
ON DUPLICATE KEY UPDATE Codigo = Codigo;

INSERT INTO MetodosPago (Codigo, Nombre) VALUES
('EFECTIVO', 'Efectivo'),
('TRANSFERENCIA', 'Transferencia bancaria'),
('ZELLE', 'Zelle'),
('BINANCE', 'Binance / USDT'),
('PAGO_MOVIL', 'Pago móvil'),
('DEBITO', 'Tarjeta de débito'),
('CREDITO', 'Tarjeta de crédito')
ON DUPLICATE KEY UPDATE Codigo = Codigo;

INSERT INTO ConceptosPago (Codigo, Nombre, Descripcion) VALUES
('INSCRIPCION', 'Inscripción', 'Pago por inscripción al curso'),
('MENSUALIDAD', 'Mensualidad', 'Pago mensual del curso'),
('SEMANAL', 'Semana', 'Pago semanal del curso'),
('MATERIAL', 'Material didáctico', 'Pago por material del curso'),
('CERTIFICACION', 'Certificación', 'Pago por certificación del curso'),
('OTRO', 'Otro', 'Otros conceptos de pago')
ON DUPLICATE KEY UPDATE Codigo = Codigo;

INSERT INTO DiasFestivos (Fecha, Descripcion) VALUES
('2025-01-01', 'Año Nuevo'),
('2025-04-19', 'Viernes Santo'),
('2025-05-01', 'Día del Trabajo'),
('2025-06-24', 'Batalla de Carabobo'),
('2025-07-05', 'Día de la Independencia'),
('2025-07-24', 'Natalicio de Simón Bolívar'),
('2025-10-12', 'Día de la Resistencia Indígena'),
('2025-12-24', 'Nochebuena'),
('2025-12-25', 'Navidad'),
('2025-12-31', 'Fin de Año')
ON DUPLICATE KEY UPDATE Fecha = Fecha;

-- ============================================
-- ÍNDICES ADICIONALES (compatibles con MariaDB 10.4)
-- ============================================

CREATE INDEX idx_estudiantes_nombre ON Estudiantes(NombreCompleto);
CREATE INDEX idx_inscripciones_estudiante ON Inscripciones(EstudianteCedula);
CREATE INDEX idx_inscripciones_grupo ON Inscripciones(GrupoId);
CREATE INDEX idx_pagos_fecha ON Pagos(FechaPago);
CREATE INDEX idx_pagos_estudiante ON Pagos(EstudianteCedula);
CREATE INDEX idx_pagos_inscripcion ON Pagos(InscripcionId);
CREATE INDEX idx_pagos_estado ON Pagos(Estado);
CREATE INDEX idx_pagos_vencimiento ON Pagos(FechaVencimiento);
CREATE INDEX idx_pagodetalles_pago ON PagoDetalles(PagoId);
CREATE INDEX idx_devoluciones_pago ON Devoluciones(PagoId);
CREATE INDEX idx_tasas_fecha ON TasasCambio(Fecha);
CREATE INDEX idx_movimientos_caja ON MovimientosCaja(CajaId, FechaMovimiento);
CREATE INDEX idx_calendario_fecha ON CalendarioClases(FechaClase);
CREATE INDEX idx_calendario_grupo ON CalendarioClases(GrupoId);
CREATE INDEX idx_auditoria_fecha ON Auditoria(FechaHora);
CREATE INDEX idx_auditoria_usuario ON Auditoria(Usuario);
CREATE INDEX idx_ajustes_devolucion_pago ON AjustesDevolucion(PagoId);

-- ============================================
-- FIN DEL SCRIPT
-- ============================================
