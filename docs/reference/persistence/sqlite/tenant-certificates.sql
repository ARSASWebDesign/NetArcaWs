-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: sqlite; selection: tenant-certificates.
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
