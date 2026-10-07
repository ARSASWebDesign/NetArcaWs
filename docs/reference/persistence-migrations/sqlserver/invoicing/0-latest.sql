-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: sqlserver; module: invoicing.
IF OBJECT_ID(N'[dbo].[__NetArcaWsInvoiceMigrations]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__NetArcaWsInvoiceMigrations] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___NetArcaWsInvoiceMigrations] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[NetArcaInvoiceRevisions] (
    [TenantHash] nvarchar(64) NOT NULL,
    [KeyHash] nvarchar(64) NOT NULL,
    [RevisionNumber] int NOT NULL,
    [TenantId] nvarchar(128) NOT NULL,
    [IdempotencyKey] nvarchar(128) NOT NULL,
    [Service] nvarchar(128) NOT NULL,
    [Environment] int NOT NULL,
    [Cuit] bigint NOT NULL,
    [PointOfSale] int NOT NULL,
    [VoucherType] int NOT NULL,
    [VoucherNumber] bigint NOT NULL,
    [RemoteRequestId] bigint NULL,
    [Payload] nvarchar(max) NOT NULL,
    [PayloadHash] nvarchar(64) NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    [CanonicalVersion] int NOT NULL,
    [State] int NOT NULL,
    [Version] bigint NOT NULL,
    [Attempt] int NOT NULL,
    [LeaseUntilMilliseconds] bigint NULL,
    [AuthorizationCode] nvarchar(max) NULL,
    [ResponseXml] nvarchar(max) NULL,
    CONSTRAINT [PK_NetArcaInvoiceRevisions] PRIMARY KEY ([TenantHash], [KeyHash], [RevisionNumber])
);

CREATE TABLE [dbo].[NetArcaInvoices] (
    [TenantHash] nvarchar(64) NOT NULL,
    [KeyHash] nvarchar(64) NOT NULL,
    [TenantId] nvarchar(128) NOT NULL,
    [IdempotencyKey] nvarchar(128) NOT NULL,
    [Service] nvarchar(128) NOT NULL,
    [ServiceHash] nvarchar(64) NOT NULL,
    [Environment] int NOT NULL,
    [Cuit] bigint NOT NULL,
    [PointOfSale] int NOT NULL,
    [VoucherType] int NOT NULL,
    [VoucherNumber] bigint NOT NULL,
    [FiscalHash] nvarchar(64) NOT NULL,
    [RemoteHash] nvarchar(64) NULL,
    [RemoteRequestId] bigint NULL,
    [Payload] nvarchar(max) NOT NULL,
    [PayloadHash] nvarchar(64) NOT NULL,
    [CanonicalVersion] int NOT NULL,
    [State] int NOT NULL,
    [Version] bigint NOT NULL,
    [Attempt] int NOT NULL,
    [LeaseUntilMilliseconds] bigint NULL,
    [AuthorizationCode] nvarchar(max) NULL,
    [ResponseXml] nvarchar(max) NULL,
    [CreatedUtcTicks] bigint NOT NULL,
    CONSTRAINT [PK_NetArcaInvoices] PRIMARY KEY ([TenantHash], [KeyHash])
);

CREATE TABLE [dbo].[NetArcaInvoiceSeriesReservations] (
    [SeriesHash] nvarchar(64) NOT NULL,
    [TenantHash] nvarchar(64) NOT NULL,
    [KeyHash] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_NetArcaInvoiceSeriesReservations] PRIMARY KEY ([SeriesHash])
);

CREATE INDEX [IX_NetArcaInvoices_TenantHash_CreatedUtcTicks] ON [dbo].[NetArcaInvoices] ([TenantHash], [CreatedUtcTicks]);

CREATE UNIQUE INDEX [UX_NetArcaInvoices_Fiscal] ON [dbo].[NetArcaInvoices] ([FiscalHash]);

CREATE UNIQUE INDEX [UX_NetArcaInvoices_Remote] ON [dbo].[NetArcaInvoices] ([RemoteHash]) WHERE [RemoteHash] IS NOT NULL;

CREATE UNIQUE INDEX [IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash] ON [dbo].[NetArcaInvoiceSeriesReservations] ([TenantHash], [KeyHash]);

INSERT INTO [dbo].[__NetArcaWsInvoiceMigrations] ([MigrationId], [ProductVersion])
VALUES (N'20261006000100_InitialInvoicing', N'10.0.12');

COMMIT;
GO
