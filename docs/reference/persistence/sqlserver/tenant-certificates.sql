-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: sqlserver; selection: tenant-certificates.
CREATE TABLE [NetArcaCertificateSlots] (
    [TenantHash] nvarchar(64) NOT NULL,
    [Cuit] bigint NOT NULL,
    [Environment] int NOT NULL,
    [ActiveVersionId] uniqueidentifier NOT NULL,
    [Generation] bigint NOT NULL,
    CONSTRAINT [PK_NetArcaCertificateSlots] PRIMARY KEY ([TenantHash], [Cuit], [Environment])
);
GO


CREATE TABLE [NetArcaCertificateVersions] (
    [TenantHash] nvarchar(64) NOT NULL,
    [Cuit] bigint NOT NULL,
    [Environment] int NOT NULL,
    [VersionId] uniqueidentifier NOT NULL,
    [CreatedAtUtcTicks] bigint NOT NULL,
    [ThumbprintSha256] nvarchar(64) NOT NULL,
    [NotBeforeUtcTicks] bigint NOT NULL,
    [NotAfterUtcTicks] bigint NOT NULL,
    [KeyId] nvarchar(128) NOT NULL,
    [Nonce] varbinary(max) NOT NULL,
    [Ciphertext] varbinary(max) NOT NULL,
    [Tag] varbinary(max) NOT NULL,
    CONSTRAINT [PK_NetArcaCertificateVersions] PRIMARY KEY ([TenantHash], [Cuit], [Environment], [VersionId])
);
GO


CREATE INDEX [IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotAfterUtcTicks] ON [NetArcaCertificateVersions] ([TenantHash], [Cuit], [Environment], [NotAfterUtcTicks]);
GO
