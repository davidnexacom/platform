-- Script de inicialización para base de datos de prueba
-- Esquema v1 (Legacy)

-- Crear base de datos si no existe
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ClientDB')
BEGIN
    CREATE DATABASE ClientDB;
END
GO

USE ClientDB;
GO

-- Tabla de clientes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'clientes')
BEGIN
    CREATE TABLE clientes (
        id_cliente NVARCHAR(50) PRIMARY KEY,
        nombre NVARCHAR(200) NOT NULL,
        email NVARCHAR(200) NULL,
        fecha_registro DATETIME DEFAULT GETDATE()
    );
END
GO

-- Tabla de facturas
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'facturas')
BEGIN
    CREATE TABLE facturas (
        id_factura NVARCHAR(50) PRIMARY KEY,
        numero_factura NVARCHAR(100) NOT NULL,
        id_cliente NVARCHAR(50) NOT NULL,
        fecha DATETIME NOT NULL DEFAULT GETDATE(),
        total DECIMAL(18,2) NOT NULL DEFAULT 0,
        FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente)
    );
END
GO

-- Tabla de líneas de factura
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'lineas_factura')
BEGIN
    CREATE TABLE lineas_factura (
        id_linea INT IDENTITY(1,1) PRIMARY KEY,
        id_factura NVARCHAR(50) NOT NULL,
        descripcion NVARCHAR(500) NOT NULL,
        cantidad DECIMAL(18,2) NOT NULL,
        precio_unitario DECIMAL(18,2) NOT NULL,
        total DECIMAL(18,2) NOT NULL,
        FOREIGN KEY (id_factura) REFERENCES facturas(id_factura)
    );
END
GO

-- Datos de prueba
-- Insertar cliente de prueba
IF NOT EXISTS (SELECT * FROM clientes WHERE id_cliente = 'CUST-001')
BEGIN
    INSERT INTO clientes (id_cliente, nombre, email)
    VALUES ('CUST-001', 'Cliente de Prueba Local', 'test@local.com');
END
GO

-- Insertar factura de prueba
IF NOT EXISTS (SELECT * FROM facturas WHERE id_factura = 'INV-001')
BEGIN
    INSERT INTO facturas (id_factura, numero_factura, id_cliente, fecha, total)
    VALUES ('INV-001', 'FAC-2026-001', 'CUST-001', GETDATE(), 1500.00);
END
GO

-- Insertar líneas de factura de prueba
IF NOT EXISTS (SELECT * FROM lineas_factura WHERE id_factura = 'INV-001')
BEGIN
    INSERT INTO lineas_factura (id_factura, descripcion, cantidad, precio_unitario, total)
    VALUES 
        ('INV-001', 'Producto A - Gateway Testing', 2, 250.00, 500.00),
        ('INV-001', 'Producto B - RabbitMQ Integration', 1, 500.00, 500.00),
        ('INV-001', 'Servicio C - Aspire Deployment', 5, 100.00, 500.00);
END
GO

-- Insertar más datos de prueba
IF NOT EXISTS (SELECT * FROM clientes WHERE id_cliente = 'CUST-002')
BEGIN
    INSERT INTO clientes (id_cliente, nombre, email)
    VALUES ('CUST-002', 'Segundo Cliente Test', 'cliente2@test.com');
    
    INSERT INTO facturas (id_factura, numero_factura, id_cliente, fecha, total)
    VALUES ('INV-002', 'FAC-2026-002', 'CUST-002', GETDATE(), 2500.00);
    
    INSERT INTO lineas_factura (id_factura, descripcion, cantidad, precio_unitario, total)
    VALUES 
        ('INV-002', 'Licencia Software', 1, 2000.00, 2000.00),
        ('INV-002', 'Soporte Técnico', 1, 500.00, 500.00);
END
GO

PRINT 'Base de datos inicializada correctamente con esquema v1';
PRINT 'Clientes creados: ' + CAST((SELECT COUNT(*) FROM clientes) AS NVARCHAR(10));
PRINT 'Facturas creadas: ' + CAST((SELECT COUNT(*) FROM facturas) AS NVARCHAR(10));
PRINT 'Líneas creadas: ' + CAST((SELECT COUNT(*) FROM lineas_factura) AS NVARCHAR(10));
GO
