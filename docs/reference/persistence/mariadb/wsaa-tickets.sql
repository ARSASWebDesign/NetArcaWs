-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.
-- Provider: mariadb; selection: wsaa-tickets.
ALTER DATABASE CHARACTER SET utf8mb4;


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
