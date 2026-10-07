-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: sqlserver; module: tenant-certificates.
IF OBJECT_ID(N'[dbo].[__NetArcaWsCertificateMigrations]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__NetArcaWsCertificateMigrations] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___NetArcaWsCertificateMigrations] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[NetArcaCertificateSlots] (
    [TenantHash] nvarchar(64) NOT NULL,
    [Cuit] bigint NOT NULL,
    [Environment] int NOT NULL,
    [ActiveVersionId] uniqueidentifier NOT NULL,
    [Generation] bigint NOT NULL,
    CONSTRAINT [PK_NetArcaCertificateSlots] PRIMARY KEY ([TenantHash], [Cuit], [Environment])
);

CREATE TABLE [dbo].[NetArcaCertificateVersions] (
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

CREATE INDEX [IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotAfterUtcTicks] ON [dbo].[NetArcaCertificateVersions] ([TenantHash], [Cuit], [Environment], [NotAfterUtcTicks]);

INSERT INTO [dbo].[__NetArcaWsCertificateMigrations] ([MigrationId], [ProductVersion])
VALUES (N'20261006000300_InitialTenantCertificates', N'10.0.12');

COMMIT;
GO
