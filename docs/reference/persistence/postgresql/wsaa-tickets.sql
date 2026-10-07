-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: postgresql; selection: wsaa-tickets.
CREATE TABLE "NetArcaWsaaTickets" (
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


CREATE INDEX "IX_NetArcaWsaaTickets_State_UpdatedUtcTicks" ON "NetArcaWsaaTickets" ("State", "UpdatedUtcTicks");
