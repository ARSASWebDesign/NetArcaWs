-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: postgresql; selection: invoice-recovery.
CREATE TABLE "NetArcaInvoiceRecoveryJobs" (
    "TenantHash" character varying(64) NOT NULL,
    "KeyHash" character varying(64) NOT NULL,
    "TenantId" character varying(512) NOT NULL,
    "IdempotencyKey" character varying(512) NOT NULL,
    "Service" character varying(128) NOT NULL,
    "State" integer NOT NULL,
    "InvoiceVersion" bigint NOT NULL,
    "PayloadHash" character varying(64) NOT NULL,
    "CanonicalVersion" integer NOT NULL,
    "Environment" integer NOT NULL,
    "Cuit" bigint NOT NULL,
    "PointOfSale" integer NOT NULL,
    "VoucherType" integer NOT NULL,
    "VoucherNumber" bigint NOT NULL,
    "CredentialReference" character varying(512),
    "Attempt" integer NOT NULL,
    "NextAvailableMilliseconds" bigint NOT NULL,
    "LeaseUntilMilliseconds" bigint,
    "ClaimId" character varying(36),
    "Generation" bigint NOT NULL,
    "LastReason" integer NOT NULL,
    CONSTRAINT "PK_NetArcaInvoiceRecoveryJobs" PRIMARY KEY ("TenantHash", "KeyHash")
);


CREATE INDEX "IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_~" ON "NetArcaInvoiceRecoveryJobs" ("State", "NextAvailableMilliseconds", "LeaseUntilMilliseconds");


CREATE INDEX "IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailable~" ON "NetArcaInvoiceRecoveryJobs" ("TenantHash", "Service", "NextAvailableMilliseconds");
