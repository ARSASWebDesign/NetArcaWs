-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: postgresql; module: invoicing.
CREATE TABLE IF NOT EXISTS public."__NetArcaWsInvoiceMigrations" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___NetArcaWsInvoiceMigrations" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE public."NetArcaInvoiceRevisions" (
    "TenantHash" character varying(64) NOT NULL,
    "KeyHash" character varying(64) NOT NULL,
    "RevisionNumber" integer NOT NULL,
    "TenantId" character varying(128) NOT NULL,
    "IdempotencyKey" character varying(128) NOT NULL,
    "Service" character varying(128) NOT NULL,
    "Environment" integer NOT NULL,
    "Cuit" bigint NOT NULL,
    "PointOfSale" integer NOT NULL,
    "VoucherType" integer NOT NULL,
    "VoucherNumber" bigint NOT NULL,
    "RemoteRequestId" bigint,
    "Payload" text NOT NULL,
    "PayloadHash" character varying(64) NOT NULL,
    "SnapshotHash" character varying(64) NOT NULL,
    "CanonicalVersion" integer NOT NULL,
    "State" integer NOT NULL,
    "Version" bigint NOT NULL,
    "Attempt" integer NOT NULL,
    "LeaseUntilMilliseconds" bigint,
    "AuthorizationCode" text,
    "ResponseXml" text,
    CONSTRAINT "PK_NetArcaInvoiceRevisions" PRIMARY KEY ("TenantHash", "KeyHash", "RevisionNumber")
);

CREATE TABLE public."NetArcaInvoices" (
    "TenantHash" character varying(64) NOT NULL,
    "KeyHash" character varying(64) NOT NULL,
    "TenantId" character varying(128) NOT NULL,
    "IdempotencyKey" character varying(128) NOT NULL,
    "Service" character varying(128) NOT NULL,
    "ServiceHash" character varying(64) NOT NULL,
    "Environment" integer NOT NULL,
    "Cuit" bigint NOT NULL,
    "PointOfSale" integer NOT NULL,
    "VoucherType" integer NOT NULL,
    "VoucherNumber" bigint NOT NULL,
    "FiscalHash" character varying(64) NOT NULL,
    "RemoteHash" character varying(64),
    "RemoteRequestId" bigint,
    "Payload" text NOT NULL,
    "PayloadHash" character varying(64) NOT NULL,
    "CanonicalVersion" integer NOT NULL,
    "State" integer NOT NULL,
    "Version" bigint NOT NULL,
    "Attempt" integer NOT NULL,
    "LeaseUntilMilliseconds" bigint,
    "AuthorizationCode" text,
    "ResponseXml" text,
    "CreatedUtcTicks" bigint NOT NULL,
    CONSTRAINT "PK_NetArcaInvoices" PRIMARY KEY ("TenantHash", "KeyHash")
);

CREATE TABLE public."NetArcaInvoiceSeriesReservations" (
    "SeriesHash" character varying(64) NOT NULL,
    "TenantHash" character varying(64) NOT NULL,
    "KeyHash" character varying(64) NOT NULL,
    CONSTRAINT "PK_NetArcaInvoiceSeriesReservations" PRIMARY KEY ("SeriesHash")
);

CREATE INDEX "IX_NetArcaInvoices_TenantHash_CreatedUtcTicks" ON public."NetArcaInvoices" ("TenantHash", "CreatedUtcTicks");

CREATE UNIQUE INDEX "UX_NetArcaInvoices_Fiscal" ON public."NetArcaInvoices" ("FiscalHash");

CREATE UNIQUE INDEX "UX_NetArcaInvoices_Remote" ON public."NetArcaInvoices" ("RemoteHash");

CREATE UNIQUE INDEX "IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash" ON public."NetArcaInvoiceSeriesReservations" ("TenantHash", "KeyHash");

INSERT INTO public."__NetArcaWsInvoiceMigrations" ("MigrationId", "ProductVersion")
VALUES ('20261006000100_InitialInvoicing', '10.0.12');

COMMIT;
