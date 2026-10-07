-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: sqlite; selection: wsaa-tickets.
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
