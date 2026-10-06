-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: sqlite; module: wsaa-tickets.
CREATE TABLE IF NOT EXISTS "__NetArcaWsTicketMigrations" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___NetArcaWsTicketMigrations" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "NetArcaWsaaTickets" (
    "KeyHash" TEXT NOT NULL CONSTRAINT "PK_NetArcaWsaaTickets" PRIMARY KEY,
    "CertificateHash" TEXT NOT NULL,
    "Endpoint" TEXT NOT NULL,
    "Service" TEXT NOT NULL,
    "State" INTEGER NOT NULL,
    "OwnerId" TEXT NULL,
    "Fence" INTEGER NOT NULL,
    "Version" INTEGER NOT NULL,
    "LeaseUntilUtcTicks" INTEGER NULL,
    "ExpiresUtcTicks" INTEGER NULL,
    "KeyId" TEXT NULL,
    "Nonce" BLOB NULL,
    "Ciphertext" BLOB NULL,
    "Tag" BLOB NULL,
    "UpdatedUtcTicks" INTEGER NOT NULL
);

CREATE INDEX "IX_NetArcaWsaaTickets_State_UpdatedUtcTicks" ON "NetArcaWsaaTickets" ("State", "UpdatedUtcTicks");

INSERT INTO "__NetArcaWsTicketMigrations" ("MigrationId", "ProductVersion")
VALUES ('20261006000200_InitialWsaaTickets', '10.0.12');

COMMIT;
