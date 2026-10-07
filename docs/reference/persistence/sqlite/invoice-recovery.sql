-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: sqlite; selection: invoice-recovery.
CREATE TABLE "NetArcaInvoiceRecoveryJobs" (
    "TenantHash" TEXT NOT NULL,
    "KeyHash" TEXT NOT NULL,
    "TenantId" TEXT NOT NULL,
    "IdempotencyKey" TEXT NOT NULL,
    "Service" TEXT NOT NULL,
    "State" INTEGER NOT NULL,
    "InvoiceVersion" INTEGER NOT NULL,
    "PayloadHash" TEXT NOT NULL,
    "CanonicalVersion" INTEGER NOT NULL,
    "Environment" INTEGER NOT NULL,
    "Cuit" INTEGER NOT NULL,
    "PointOfSale" INTEGER NOT NULL,
    "VoucherType" INTEGER NOT NULL,
    "VoucherNumber" INTEGER NOT NULL,
    "CredentialReference" TEXT NULL,
    "Attempt" INTEGER NOT NULL,
    "NextAvailableMilliseconds" INTEGER NOT NULL,
    "LeaseUntilMilliseconds" INTEGER NULL,
    "ClaimId" TEXT NULL,
    "Generation" INTEGER NOT NULL,
    "LastReason" INTEGER NOT NULL,
    CONSTRAINT "PK_NetArcaInvoiceRecoveryJobs" PRIMARY KEY ("TenantHash", "KeyHash")
);


CREATE INDEX "IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_LeaseUntilMilliseconds" ON "NetArcaInvoiceRecoveryJobs" ("State", "NextAvailableMilliseconds", "LeaseUntilMilliseconds");


CREATE INDEX "IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailableMilliseconds" ON "NetArcaInvoiceRecoveryJobs" ("TenantHash", "Service", "NextAvailableMilliseconds");
