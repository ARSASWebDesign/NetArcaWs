-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: postgresql; selection: all.
CREATE TABLE "NetArcaInvoiceRevisions" (
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


CREATE TABLE "NetArcaInvoices" (
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


CREATE TABLE "NetArcaInvoiceSeriesReservations" (
    "SeriesHash" character varying(64) NOT NULL,
    "TenantHash" character varying(64) NOT NULL,
    "KeyHash" character varying(64) NOT NULL,
    CONSTRAINT "PK_NetArcaInvoiceSeriesReservations" PRIMARY KEY ("SeriesHash")
);


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


CREATE INDEX "IX_NetArcaInvoices_TenantHash_CreatedUtcTicks" ON "NetArcaInvoices" ("TenantHash", "CreatedUtcTicks");


CREATE UNIQUE INDEX "UX_NetArcaInvoices_Fiscal" ON "NetArcaInvoices" ("FiscalHash");


CREATE UNIQUE INDEX "UX_NetArcaInvoices_Remote" ON "NetArcaInvoices" ("RemoteHash");


CREATE UNIQUE INDEX "IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash" ON "NetArcaInvoiceSeriesReservations" ("TenantHash", "KeyHash");


CREATE INDEX "IX_NetArcaWsaaTickets_State_UpdatedUtcTicks" ON "NetArcaWsaaTickets" ("State", "UpdatedUtcTicks");
