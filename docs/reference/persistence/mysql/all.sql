-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: mysql; selection: all.
ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `NetArcaInvoiceRevisions` (
    `TenantHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `KeyHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `RevisionNumber` int NOT NULL,
    `TenantId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `IdempotencyKey` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Service` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Environment` int NOT NULL,
    `Cuit` bigint NOT NULL,
    `PointOfSale` int NOT NULL,
    `VoucherType` int NOT NULL,
    `VoucherNumber` bigint NOT NULL,
    `RemoteRequestId` bigint NULL,
    `Payload` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PayloadHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `SnapshotHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CanonicalVersion` int NOT NULL,
    `State` int NOT NULL,
    `Version` bigint NOT NULL,
    `Attempt` int NOT NULL,
    `LeaseUntilMilliseconds` bigint NULL,
    `AuthorizationCode` longtext CHARACTER SET utf8mb4 NULL,
    `ResponseXml` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_NetArcaInvoiceRevisions` PRIMARY KEY (`TenantHash`, `KeyHash`, `RevisionNumber`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `NetArcaInvoices` (
    `TenantHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `KeyHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `TenantId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `IdempotencyKey` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Service` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ServiceHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Environment` int NOT NULL,
    `Cuit` bigint NOT NULL,
    `PointOfSale` int NOT NULL,
    `VoucherType` int NOT NULL,
    `VoucherNumber` bigint NOT NULL,
    `FiscalHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `RemoteHash` varchar(64) CHARACTER SET utf8mb4 NULL,
    `RemoteRequestId` bigint NULL,
    `Payload` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PayloadHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CanonicalVersion` int NOT NULL,
    `State` int NOT NULL,
    `Version` bigint NOT NULL,
    `Attempt` int NOT NULL,
    `LeaseUntilMilliseconds` bigint NULL,
    `AuthorizationCode` longtext CHARACTER SET utf8mb4 NULL,
    `ResponseXml` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedUtcTicks` bigint NOT NULL,
    CONSTRAINT `PK_NetArcaInvoices` PRIMARY KEY (`TenantHash`, `KeyHash`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `NetArcaInvoiceSeriesReservations` (
    `SeriesHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `TenantHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `KeyHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_NetArcaInvoiceSeriesReservations` PRIMARY KEY (`SeriesHash`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `NetArcaWsaaTickets` (
    `KeyHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CertificateHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Endpoint` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `Service` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `State` int NOT NULL,
    `OwnerId` varchar(36) CHARACTER SET utf8mb4 NULL,
    `Fence` bigint NOT NULL,
    `Version` bigint NOT NULL,
    `LeaseUntilUtcTicks` bigint NULL,
    `ExpiresUtcTicks` bigint NULL,
    `KeyId` varchar(128) CHARACTER SET utf8mb4 NULL,
    `Nonce` longblob NULL,
    `Ciphertext` longblob NULL,
    `Tag` longblob NULL,
    `UpdatedUtcTicks` bigint NOT NULL,
    CONSTRAINT `PK_NetArcaWsaaTickets` PRIMARY KEY (`KeyHash`)
) CHARACTER SET=utf8mb4;


CREATE INDEX `IX_NetArcaInvoices_TenantHash_CreatedUtcTicks` ON `NetArcaInvoices` (`TenantHash`, `CreatedUtcTicks`);


CREATE UNIQUE INDEX `UX_NetArcaInvoices_Fiscal` ON `NetArcaInvoices` (`FiscalHash`);


CREATE UNIQUE INDEX `UX_NetArcaInvoices_Remote` ON `NetArcaInvoices` (`RemoteHash`);


CREATE UNIQUE INDEX `IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash` ON `NetArcaInvoiceSeriesReservations` (`TenantHash`, `KeyHash`);


CREATE INDEX `IX_NetArcaWsaaTickets_State_UpdatedUtcTicks` ON `NetArcaWsaaTickets` (`State`, `UpdatedUtcTicks`);
