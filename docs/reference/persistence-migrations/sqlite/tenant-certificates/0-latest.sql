-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: sqlite; module: tenant-certificates.
CREATE TABLE IF NOT EXISTS "__NetArcaWsCertificateMigrations" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___NetArcaWsCertificateMigrations" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "NetArcaCertificateSlots" (
    "TenantHash" TEXT NOT NULL,
    "Cuit" INTEGER NOT NULL,
    "Environment" INTEGER NOT NULL,
    "ActiveVersionId" TEXT NOT NULL,
    "Generation" INTEGER NOT NULL,
    CONSTRAINT "PK_NetArcaCertificateSlots" PRIMARY KEY ("TenantHash", "Cuit", "Environment")
);

CREATE TABLE "NetArcaCertificateVersions" (
    "TenantHash" TEXT NOT NULL,
    "Cuit" INTEGER NOT NULL,
    "Environment" INTEGER NOT NULL,
    "VersionId" TEXT NOT NULL,
    "CreatedAtUtcTicks" INTEGER NOT NULL,
    "ThumbprintSha256" TEXT NOT NULL,
    "NotBeforeUtcTicks" INTEGER NOT NULL,
    "NotAfterUtcTicks" INTEGER NOT NULL,
    "KeyId" TEXT NOT NULL,
    "Nonce" BLOB NOT NULL,
    "Ciphertext" BLOB NOT NULL,
    "Tag" BLOB NOT NULL,
    CONSTRAINT "PK_NetArcaCertificateVersions" PRIMARY KEY ("TenantHash", "Cuit", "Environment", "VersionId")
);

CREATE INDEX "IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotAfterUtcTicks" ON "NetArcaCertificateVersions" ("TenantHash", "Cuit", "Environment", "NotAfterUtcTicks");

INSERT INTO "__NetArcaWsCertificateMigrations" ("MigrationId", "ProductVersion")
VALUES ('20261006000300_InitialTenantCertificates', '10.0.12');

COMMIT;
