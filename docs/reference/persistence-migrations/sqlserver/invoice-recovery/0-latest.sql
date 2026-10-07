-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: sqlserver; module: invoice-recovery.
IF OBJECT_ID(N'[dbo].[__NetArcaWsInvoiceRecoveryMigrations]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__NetArcaWsInvoiceRecoveryMigrations] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___NetArcaWsInvoiceRecoveryMigrations] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[NetArcaInvoiceRecoveryJobs] (
    [TenantHash] nvarchar(64) NOT NULL,
    [KeyHash] nvarchar(64) NOT NULL,
    [TenantId] nvarchar(512) NOT NULL,
    [IdempotencyKey] nvarchar(512) NOT NULL,
    [Service] nvarchar(128) NOT NULL,
    [State] int NOT NULL,
    [InvoiceVersion] bigint NOT NULL,
    [PayloadHash] nvarchar(64) NOT NULL,
    [CanonicalVersion] int NOT NULL,
    [Environment] int NOT NULL,
    [Cuit] bigint NOT NULL,
    [PointOfSale] int NOT NULL,
    [VoucherType] int NOT NULL,
    [VoucherNumber] bigint NOT NULL,
    [CredentialReference] nvarchar(512) NULL,
    [Attempt] int NOT NULL,
    [NextAvailableMilliseconds] bigint NOT NULL,
    [LeaseUntilMilliseconds] bigint NULL,
    [ClaimId] nvarchar(36) NULL,
    [Generation] bigint NOT NULL,
    [LastReason] int NOT NULL,
    CONSTRAINT [PK_NetArcaInvoiceRecoveryJobs] PRIMARY KEY ([TenantHash], [KeyHash])
);

CREATE INDEX [IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_LeaseUntilMilliseconds] ON [dbo].[NetArcaInvoiceRecoveryJobs] ([State], [NextAvailableMilliseconds], [LeaseUntilMilliseconds]);

CREATE INDEX [IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailableMilliseconds] ON [dbo].[NetArcaInvoiceRecoveryJobs] ([TenantHash], [Service], [NextAvailableMilliseconds]);

INSERT INTO [dbo].[__NetArcaWsInvoiceRecoveryMigrations] ([MigrationId], [ProductVersion])
VALUES (N'20261007000400_InitialInvoiceRecovery', N'10.0.12');

COMMIT;
GO
