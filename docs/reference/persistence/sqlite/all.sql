-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: sqlite; selection: all.
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


CREATE TABLE "NetArcaInvoiceRevisions" (
    "TenantHash" TEXT NOT NULL,
    "KeyHash" TEXT NOT NULL,
    "RevisionNumber" INTEGER NOT NULL,
    "TenantId" TEXT NOT NULL,
    "IdempotencyKey" TEXT NOT NULL,
    "Service" TEXT NOT NULL,
    "Environment" INTEGER NOT NULL,
    "Cuit" INTEGER NOT NULL,
    "PointOfSale" INTEGER NOT NULL,
    "VoucherType" INTEGER NOT NULL,
    "VoucherNumber" INTEGER NOT NULL,
    "RemoteRequestId" INTEGER NULL,
    "Payload" TEXT NOT NULL,
    "PayloadHash" TEXT NOT NULL,
    "SnapshotHash" TEXT NOT NULL,
    "CanonicalVersion" INTEGER NOT NULL,
    "State" INTEGER NOT NULL,
    "Version" INTEGER NOT NULL,
    "Attempt" INTEGER NOT NULL,
    "LeaseUntilMilliseconds" INTEGER NULL,
    "AuthorizationCode" TEXT NULL,
    "ResponseXml" TEXT NULL,
    CONSTRAINT "PK_NetArcaInvoiceRevisions" PRIMARY KEY ("TenantHash", "KeyHash", "RevisionNumber")
);


CREATE TABLE "NetArcaInvoices" (
    "TenantHash" TEXT NOT NULL,
    "KeyHash" TEXT NOT NULL,
    "TenantId" TEXT NOT NULL,
    "IdempotencyKey" TEXT NOT NULL,
    "Service" TEXT NOT NULL,
    "ServiceHash" TEXT NOT NULL,
    "Environment" INTEGER NOT NULL,
    "Cuit" INTEGER NOT NULL,
    "PointOfSale" INTEGER NOT NULL,
    "VoucherType" INTEGER NOT NULL,
    "VoucherNumber" INTEGER NOT NULL,
    "FiscalHash" TEXT NOT NULL,
    "RemoteHash" TEXT NULL,
    "RemoteRequestId" INTEGER NULL,
    "Payload" TEXT NOT NULL,
    "PayloadHash" TEXT NOT NULL,
    "CanonicalVersion" INTEGER NOT NULL,
    "State" INTEGER NOT NULL,
    "Version" INTEGER NOT NULL,
    "Attempt" INTEGER NOT NULL,
    "LeaseUntilMilliseconds" INTEGER NULL,
    "AuthorizationCode" TEXT NULL,
    "ResponseXml" TEXT NULL,
    "CreatedUtcTicks" INTEGER NOT NULL,
    CONSTRAINT "PK_NetArcaInvoices" PRIMARY KEY ("TenantHash", "KeyHash")
);


CREATE TABLE "NetArcaInvoiceSeriesReservations" (
    "SeriesHash" TEXT NOT NULL CONSTRAINT "PK_NetArcaInvoiceSeriesReservations" PRIMARY KEY,
    "TenantHash" TEXT NOT NULL,
    "KeyHash" TEXT NOT NULL
);


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


CREATE INDEX "IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotAfterUtcTicks" ON "NetArcaCertificateVersions" ("TenantHash", "Cuit", "Environment", "NotAfterUtcTicks");


CREATE INDEX "IX_NetArcaInvoices_TenantHash_CreatedUtcTicks" ON "NetArcaInvoices" ("TenantHash", "CreatedUtcTicks");


CREATE UNIQUE INDEX "UX_NetArcaInvoices_Fiscal" ON "NetArcaInvoices" ("FiscalHash");


CREATE UNIQUE INDEX "UX_NetArcaInvoices_Remote" ON "NetArcaInvoices" ("RemoteHash");


CREATE UNIQUE INDEX "IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash" ON "NetArcaInvoiceSeriesReservations" ("TenantHash", "KeyHash");


CREATE INDEX "IX_NetArcaWsaaTickets_State_UpdatedUtcTicks" ON "NetArcaWsaaTickets" ("State", "UpdatedUtcTicks");
