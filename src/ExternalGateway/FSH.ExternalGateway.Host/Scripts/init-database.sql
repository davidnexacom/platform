-- External Gateway Database Schema
-- Version: v1
-- Description: Schema for testing External Gateway with invoice data in SQL Server

-- Drop tables if they exist (for clean slate)
IF OBJECT_ID('InvoiceLines', 'U') IS NOT NULL DROP TABLE InvoiceLines;
IF OBJECT_ID('Invoices', 'U') IS NOT NULL DROP TABLE Invoices;
GO

-- Create Invoices table
CREATE TABLE Invoices (
    InvoiceId NVARCHAR(50) PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    InvoiceDate DATETIME NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    CustomerId NVARCHAR(50) NOT NULL,
    CustomerName NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Create InvoiceLines table
CREATE TABLE InvoiceLines (
    LineId NVARCHAR(50) PRIMARY KEY,
    InvoiceId NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_InvoiceLines_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE CASCADE
);
GO

-- Create indexes for performance
CREATE INDEX IX_Invoices_InvoiceNumber ON Invoices(InvoiceNumber);
CREATE INDEX IX_Invoices_CustomerId ON Invoices(CustomerId);
CREATE INDEX IX_Invoices_InvoiceDate ON Invoices(InvoiceDate);
CREATE INDEX IX_InvoiceLines_InvoiceId ON InvoiceLines(InvoiceId);
GO

-- Seed test data
PRINT 'Seeding test data...'

-- Invoice 1
INSERT INTO Invoices (InvoiceId, InvoiceNumber, InvoiceDate, TotalAmount, CustomerId, CustomerName)
VALUES ('INV-001', 'F2024-001', '2024-01-15', 2500.00, 'CUST-001', 'Acme Corporation');

INSERT INTO InvoiceLines (LineId, InvoiceId, Description, Quantity, UnitPrice, TotalPrice)
VALUES 
    ('LINE-001', 'INV-001', 'Software License - Premium', 1, 1500.00, 1500.00),
    ('LINE-002', 'INV-001', 'Support Contract - Annual', 1, 1000.00, 1000.00);
GO

-- Invoice 2
INSERT INTO Invoices (InvoiceId, InvoiceNumber, InvoiceDate, TotalAmount, CustomerId, CustomerName)
VALUES ('INV-002', 'F2024-002', '2024-01-20', 850.00, 'CUST-002', 'Tech Solutions Ltd');

INSERT INTO InvoiceLines (LineId, InvoiceId, Description, Quantity, UnitPrice, TotalPrice)
VALUES ('LINE-003', 'INV-002', 'Consulting Hours', 10, 85.00, 850.00);
GO

-- Invoice 3
INSERT INTO Invoices (InvoiceId, InvoiceNumber, InvoiceDate, TotalAmount, CustomerId, CustomerName)
VALUES ('INV-003', 'F2024-003', '2024-01-25', 3200.00, 'CUST-001', 'Acme Corporation');

INSERT INTO InvoiceLines (LineId, InvoiceId, Description, Quantity, UnitPrice, TotalPrice)
VALUES 
    ('LINE-004', 'INV-003', 'Cloud Hosting - Q1 2024', 3, 800.00, 2400.00),
    ('LINE-005', 'INV-003', 'Data Migration Services', 1, 800.00, 800.00);
GO

-- Invoice 4
INSERT INTO Invoices (InvoiceId, InvoiceNumber, InvoiceDate, TotalAmount, CustomerId, CustomerName)
VALUES ('INV-004', 'F2024-004', '2024-02-01', 1250.00, 'CUST-003', 'Global Industries');

INSERT INTO InvoiceLines (LineId, InvoiceId, Description, Quantity, UnitPrice, TotalPrice)
VALUES 
    ('LINE-006', 'INV-004', 'Database Backup Service', 12, 50.00, 600.00),
    ('LINE-007', 'INV-004', 'Security Audit', 1, 650.00, 650.00);
GO

-- Invoice 5 (for testing)
INSERT INTO Invoices (InvoiceId, InvoiceNumber, InvoiceDate, TotalAmount, CustomerId, CustomerName)
VALUES ('TEST-001', 'TEST-2024-001', '2024-02-10', 99.99, 'CUST-TEST', 'Test Customer');

INSERT INTO InvoiceLines (LineId, InvoiceId, Description, Quantity, UnitPrice, TotalPrice)
VALUES ('LINE-TEST-001', 'TEST-001', 'Test Product', 1, 99.99, 99.99);
GO

-- Verify data
PRINT 'Data seeded successfully!'
PRINT 'Total Invoices: ' + CAST((SELECT COUNT(*) FROM Invoices) AS VARCHAR)
PRINT 'Total Invoice Lines: ' + CAST((SELECT COUNT(*) FROM InvoiceLines) AS VARCHAR)
GO

PRINT 'Database initialization complete!'
GO
