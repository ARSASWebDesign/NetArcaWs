-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: sqlserver; selection: all.
CREATE TABLE [NetArcaInvoiceRevisions] (
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
GO


CREATE TABLE [NetArcaInvoices] (
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
GO


CREATE TABLE [NetArcaInvoiceSeriesReservations] (
    [SeriesHash] nvarchar(64) NOT NULL,
    [TenantHash] nvarchar(64) NOT NULL,
    [KeyHash] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_NetArcaInvoiceSeriesReservations] PRIMARY KEY ([SeriesHash])
);
GO


CREATE TABLE [NetArcaWsaaTickets] (
    [KeyHash] nvarchar(64) NOT NULL,
    [CertificateHash] nvarchar(64) NOT NULL,
    [Endpoint] nvarchar(512) NOT NULL,
    [Service] nvarchar(32) NOT NULL,
    [State] int NOT NULL,
    [OwnerId] nvarchar(36) NULL,
    [Fence] bigint NOT NULL,
    [Version] bigint NOT NULL,
    [LeaseUntilUtcTicks] bigint NULL,
    [ExpiresUtcTicks] bigint NULL,
    [KeyId] nvarchar(128) NULL,
    [Nonce] varbinary(max) NULL,
    [Ciphertext] varbinary(max) NULL,
    [Tag] varbinary(max) NULL,
    [UpdatedUtcTicks] bigint NOT NULL,
    CONSTRAINT [PK_NetArcaWsaaTickets] PRIMARY KEY ([KeyHash])
);
GO


CREATE INDEX [IX_NetArcaInvoices_TenantHash_CreatedUtcTicks] ON [NetArcaInvoices] ([TenantHash], [CreatedUtcTicks]);
GO


CREATE UNIQUE INDEX [UX_NetArcaInvoices_Fiscal] ON [NetArcaInvoices] ([FiscalHash]);
GO


CREATE UNIQUE INDEX [UX_NetArcaInvoices_Remote] ON [NetArcaInvoices] ([RemoteHash]) WHERE [RemoteHash] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash] ON [NetArcaInvoiceSeriesReservations] ([TenantHash], [KeyHash]);
GO


CREATE INDEX [IX_NetArcaWsaaTickets_State_UpdatedUtcTicks] ON [NetArcaWsaaTickets] ([State], [UpdatedUtcTicks]);
GO
