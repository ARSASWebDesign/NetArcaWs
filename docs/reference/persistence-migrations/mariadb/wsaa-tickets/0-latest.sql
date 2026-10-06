-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: mariadb; module: wsaa-tickets.
CREATE TABLE IF NOT EXISTS `__NetArcaWsTicketMigrations` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___NetArcaWsTicketMigrations` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
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

CREATE INDEX `IX_NetArcaWsaaTickets_State_UpdatedUtcTicks` ON `NetArcaWsaaTickets` (`State`, `UpdatedUtcTicks`);

INSERT INTO `__NetArcaWsTicketMigrations` (`MigrationId`, `ProductVersion`)
VALUES ('20261006000200_InitialWsaaTickets', '10.0.12');

COMMIT;
