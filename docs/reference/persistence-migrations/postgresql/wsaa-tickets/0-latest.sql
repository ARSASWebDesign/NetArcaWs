-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: postgresql; module: wsaa-tickets.
CREATE TABLE IF NOT EXISTS "__NetArcaWsTicketMigrations" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___NetArcaWsTicketMigrations" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE public."NetArcaWsaaTickets" (
    "KeyHash" character varying(64) NOT NULL,
    "CertificateHash" character varying(64) NOT NULL,
    "Endpoint" character varying(512) NOT NULL,
    "Service" character varying(32) NOT NULL,
    "State" integer NOT NULL,
    "OwnerId" character varying(36),
    "Fence" bigint NOT NULL,
    "Version" bigint NOT NULL,
    "LeaseUntilUtcTicks" bigint,
    "ExpiresUtcTicks" bigint,
    "KeyId" character varying(128),
    "Nonce" bytea,
    "Ciphertext" bytea,
    "Tag" bytea,
    "UpdatedUtcTicks" bigint NOT NULL,
    CONSTRAINT "PK_NetArcaWsaaTickets" PRIMARY KEY ("KeyHash")
);

CREATE INDEX "IX_NetArcaWsaaTickets_State_UpdatedUtcTicks" ON public."NetArcaWsaaTickets" ("State", "UpdatedUtcTicks");

INSERT INTO "__NetArcaWsTicketMigrations" ("MigrationId", "ProductVersion")
VALUES ('20261006000200_InitialWsaaTickets', '10.0.12');

COMMIT;
