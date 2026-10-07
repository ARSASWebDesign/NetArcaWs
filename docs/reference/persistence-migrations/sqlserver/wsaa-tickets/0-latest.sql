-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: sqlserver; module: wsaa-tickets.
IF OBJECT_ID(N'[dbo].[__NetArcaWsTicketMigrations]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__NetArcaWsTicketMigrations] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___NetArcaWsTicketMigrations] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [dbo].[NetArcaWsaaTickets] (
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

CREATE INDEX [IX_NetArcaWsaaTickets_State_UpdatedUtcTicks] ON [dbo].[NetArcaWsaaTickets] ([State], [UpdatedUtcTicks]);

INSERT INTO [dbo].[__NetArcaWsTicketMigrations] ([MigrationId], [ProductVersion])
VALUES (N'20261006000200_InitialWsaaTickets', N'10.0.12');

COMMIT;
GO
