-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: mysql; selection: invoice-recovery.
ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `NetArcaInvoiceRecoveryJobs` (
    `TenantHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `KeyHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `TenantId` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `IdempotencyKey` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `Service` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `State` int NOT NULL,
    `InvoiceVersion` bigint NOT NULL,
    `PayloadHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CanonicalVersion` int NOT NULL,
    `Environment` int NOT NULL,
    `Cuit` bigint NOT NULL,
    `PointOfSale` int NOT NULL,
    `VoucherType` int NOT NULL,
    `VoucherNumber` bigint NOT NULL,
    `CredentialReference` varchar(512) CHARACTER SET utf8mb4 NULL,
    `Attempt` int NOT NULL,
    `NextAvailableMilliseconds` bigint NOT NULL,
    `LeaseUntilMilliseconds` bigint NULL,
    `ClaimId` varchar(36) CHARACTER SET utf8mb4 NULL,
    `Generation` bigint NOT NULL,
    `LastReason` int NOT NULL,
    CONSTRAINT `PK_NetArcaInvoiceRecoveryJobs` PRIMARY KEY (`TenantHash`, `KeyHash`)
) CHARACTER SET=utf8mb4;


CREATE INDEX `IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_L~` ON `NetArcaInvoiceRecoveryJobs` (`State`, `NextAvailableMilliseconds`, `LeaseUntilMilliseconds`);


CREATE INDEX `IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailableM~` ON `NetArcaInvoiceRecoveryJobs` (`TenantHash`, `Service`, `NextAvailableMilliseconds`);
