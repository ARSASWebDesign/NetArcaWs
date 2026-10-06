-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: sqlserver; selection: wsaa-tickets.
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


CREATE INDEX [IX_NetArcaWsaaTickets_State_UpdatedUtcTicks] ON [NetArcaWsaaTickets] ([State], [UpdatedUtcTicks]);
GO
