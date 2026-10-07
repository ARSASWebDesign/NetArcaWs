-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: sqlite; module: invoicing.
CREATE TABLE IF NOT EXISTS "__NetArcaWsInvoiceMigrations" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___NetArcaWsInvoiceMigrations" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
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

CREATE INDEX "IX_NetArcaInvoices_TenantHash_CreatedUtcTicks" ON "NetArcaInvoices" ("TenantHash", "CreatedUtcTicks");

CREATE UNIQUE INDEX "UX_NetArcaInvoices_Fiscal" ON "NetArcaInvoices" ("FiscalHash");

CREATE UNIQUE INDEX "UX_NetArcaInvoices_Remote" ON "NetArcaInvoices" ("RemoteHash");

CREATE UNIQUE INDEX "IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash" ON "NetArcaInvoiceSeriesReservations" ("TenantHash", "KeyHash");

INSERT INTO "__NetArcaWsInvoiceMigrations" ("MigrationId", "ProductVersion")
VALUES ('20261006000100_InitialInvoicing', '10.0.12');

COMMIT;
