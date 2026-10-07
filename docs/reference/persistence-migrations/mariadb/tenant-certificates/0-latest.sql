-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.
-- Provider: mariadb; module: tenant-certificates.
CREATE TABLE IF NOT EXISTS `__NetArcaWsCertificateMigrations` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___NetArcaWsCertificateMigrations` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
CREATE TABLE `NetArcaCertificateSlots` (
    `TenantHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Cuit` bigint NOT NULL,
    `Environment` int NOT NULL,
    `ActiveVersionId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Generation` bigint NOT NULL,
    CONSTRAINT `PK_NetArcaCertificateSlots` PRIMARY KEY (`TenantHash`, `Cuit`, `Environment`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `NetArcaCertificateVersions` (
    `TenantHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Cuit` bigint NOT NULL,
    `Environment` int NOT NULL,
    `VersionId` char(36) COLLATE ascii_general_ci NOT NULL,
    `CreatedAtUtcTicks` bigint NOT NULL,
    `ThumbprintSha256` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `NotBeforeUtcTicks` bigint NOT NULL,
    `NotAfterUtcTicks` bigint NOT NULL,
    `KeyId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Nonce` longblob NOT NULL,
    `Ciphertext` longblob NOT NULL,
    `Tag` longblob NOT NULL,
    CONSTRAINT `PK_NetArcaCertificateVersions` PRIMARY KEY (`TenantHash`, `Cuit`, `Environment`, `VersionId`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotAf~` ON `NetArcaCertificateVersions` (`TenantHash`, `Cuit`, `Environment`, `NotAfterUtcTicks`);

INSERT INTO `__NetArcaWsCertificateMigrations` (`MigrationId`, `ProductVersion`)
VALUES ('20261006000300_InitialTenantCertificates', '10.0.12');

COMMIT;
