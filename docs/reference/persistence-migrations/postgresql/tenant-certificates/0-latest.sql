-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: postgresql; module: tenant-certificates.
CREATE TABLE IF NOT EXISTS public."__NetArcaWsCertificateMigrations" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___NetArcaWsCertificateMigrations" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE public."NetArcaCertificateSlots" (
    "TenantHash" character varying(64) NOT NULL,
    "Cuit" bigint NOT NULL,
    "Environment" integer NOT NULL,
    "ActiveVersionId" uuid NOT NULL,
    "Generation" bigint NOT NULL,
    CONSTRAINT "PK_NetArcaCertificateSlots" PRIMARY KEY ("TenantHash", "Cuit", "Environment")
);

CREATE TABLE public."NetArcaCertificateVersions" (
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

CREATE INDEX "IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotA~" ON public."NetArcaCertificateVersions" ("TenantHash", "Cuit", "Environment", "NotAfterUtcTicks");

INSERT INTO public."__NetArcaWsCertificateMigrations" ("MigrationId", "ProductVersion")
VALUES ('20261006000300_InitialTenantCertificates', '10.0.12');

COMMIT;
