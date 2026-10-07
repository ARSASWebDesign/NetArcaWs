-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: sqlserver; selection: invoice-recovery.
CREATE TABLE [NetArcaInvoiceRecoveryJobs] (
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
GO


CREATE INDEX [IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_LeaseUntilMilliseconds] ON [NetArcaInvoiceRecoveryJobs] ([State], [NextAvailableMilliseconds], [LeaseUntilMilliseconds]);
GO


CREATE INDEX [IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailableMilliseconds] ON [NetArcaInvoiceRecoveryJobs] ([TenantHash], [Service], [NextAvailableMilliseconds]);
GO
