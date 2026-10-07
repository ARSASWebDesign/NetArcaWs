-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: postgresql; selection: tenant-certificates.
CREATE TABLE "NetArcaCertificateSlots" (
    "TenantHash" character varying(64) NOT NULL,
    "Cuit" bigint NOT NULL,
    "Environment" integer NOT NULL,
    "ActiveVersionId" uuid NOT NULL,
    "Generation" bigint NOT NULL,
    CONSTRAINT "PK_NetArcaCertificateSlots" PRIMARY KEY ("TenantHash", "Cuit", "Environment")
);


CREATE TABLE "NetArcaCertificateVersions" (
    "TenantHash" character varying(64) NOT NULL,
    "Cuit" bigint NOT NULL,
    "Environment" integer NOT NULL,
    "VersionId" uuid NOT NULL,
    "CreatedAtUtcTicks" bigint NOT NULL,
    "ThumbprintSha256" character varying(64) NOT NULL,
    "NotBeforeUtcTicks" bigint NOT NULL,
    "NotAfterUtcTicks" bigint NOT NULL,
    "KeyId" character varying(128) NOT NULL,
    "Nonce" bytea NOT NULL,
    "Ciphertext" bytea NOT NULL,
    "Tag" bytea NOT NULL,
    CONSTRAINT "PK_NetArcaCertificateVersions" PRIMARY KEY ("TenantHash", "Cuit", "Environment", "VersionId")
);


CREATE INDEX "IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotA~" ON "NetArcaCertificateVersions" ("TenantHash", "Cuit", "Environment", "NotAfterUtcTicks");
