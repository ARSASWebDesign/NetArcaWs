<!-- Source: THIRD-PARTY-NOTICES.md. Generated wiki mirror; edit the repository source. -->

# Third-party notices

This port derives from PyAfipWs, Copyright (C) 2008-2021 Mariano Reingart,
licensed LGPL-3.0-or-later. The .NET adaptation changes cryptographic APIs,
transport, error handling and cache behavior; see docs/plans/hito-1.md.
Original source: https://github.com/reingart/pyafipws/tree/d595b072110accec9dae1ddb58165ab847b8520a

Versions and license declarations were checked against NuGet.org and the
restored package manifests on 2026-10-14. Production dependency graph licenses include
MIT and Apache-2.0. Additional transitive packages are
Microsoft.Extensions.Caching.Abstractions, Configuration, Configuration.Abstractions,
Configuration.Binder, DependencyInjection, DependencyInjection.Abstractions,
Diagnostics, Diagnostics.Abstractions, Logging, Logging.Abstractions,
Options.ConfigurationExtensions, Primitives, Diagnostics.HealthChecks.Abstractions,
FileProviders.Abstractions and Hosting.Abstractions, all version 10.0.13.
These NuGet dependencies are referenced, not embedded into this library's DLL.

## Library dependencies

| Package | Version | License |
| --- | ---: | --- |
| Microsoft.Extensions.Caching.Memory | 10.0.13 | MIT |
| Microsoft.Extensions.Http | 10.0.13 | MIT |
| Microsoft.Extensions.Options | 10.0.13 | MIT |
| Microsoft.Extensions.Diagnostics.HealthChecks | 10.0.13 | MIT |
| System.Security.Cryptography.Pkcs | 10.0.13 | MIT |
| Microsoft.Data.Sqlite | 10.0.13 | MIT |
| PDFsharp-MigraDoc | 6.2.4 | MIT |
| QRCoder | 1.8.1 | MIT |
| StbImageSharp | 2.30.16 | Unlicense OR MIT |

PDFsharp-MigraDoc 6.2.4 is the latest stable release in the 6.x line declared
MIT by its NuGet package manifest. Version 6.5.0 declares AGPL-3.0-only, which
introduces copyleft obligations and requires a separate licensing-policy
decision before adoption. AGPL permits commercial use subject to its terms.
See the [6.2.4 NuGet package](https://www.nuget.org/packages/PDFsharp-MigraDoc/6.2.4)
and [6.5.0 NuGet package](https://www.nuget.org/packages/PDFsharp-MigraDoc/6.5.0).

The three rendering dependencies are isolated in optional projects: HTML
references QRCoder; PDF references PDFsharp-MigraDoc, QRCoder, and StbImageSharp.
The core `NetArcaWs` package references none of these. PDF uses PDFsharp's
process-wide font resolver only to serve its embedded Noto Sans resources. The
renderer installs its immutable resolver on first use and does not replace an
already configured resolver. PNG normalization uses StbImageSharp in memory;
the renderer does not call System.Drawing APIs. QRCoder's transitive metadata
may include System.Drawing.Common and Microsoft.Win32.SystemEvents; those
remain transitive dependencies of the optional package graph.

QRCoder 1.8.1 also resolves System.Drawing.Common 6.0.0 and
Microsoft.Win32.SystemEvents 6.0.0 (both MIT) as transitive NuGet dependencies.
Both optional renderers use QRCoder's platform-neutral PNG implementation and
do not call System.Drawing, GDI+, or WPF APIs.

### Transitive library dependencies added by SQLite

`Microsoft.Data.Sqlite` 10.0.13 resolves `Microsoft.Data.Sqlite.Core` 10.0.13
(MIT), `SQLitePCLRaw.bundle_e_sqlite3` 2.1.13, `SQLitePCLRaw.core` 2.1.13,
`SQLitePCLRaw.provider.e_sqlite3` 2.1.13 and `SQLitePCLRaw.lib.e_sqlite3`
2.1.13. The SQLitePCLRaw packages declare Apache-2.0. The bundle includes the
native SQLite amalgamation; its upstream SQLite project is public domain, while
the SQLitePCLRaw packaging/provider code remains under the package's declared
Apache-2.0 license (Copyright 2014-2024 SourceGear, LLC). See the
[SQLitePCLRaw project notices](https://github.com/ericsink/SQLitePCL.raw/blob/v2.1.13/README.md),
[SQLite project copyright](https://www.sqlite.org/copyright.html), and the
[NuGet license declaration](https://www.nuget.org/packages/SQLitePCLRaw.lib.e_sqlite3/2.1.13).
The library references these packages and does not statically link them into
its own managed assembly; deployment assets can include their native runtime
library.

### Optional MySQL and MariaDB provider

`NetArcaWs.EntityFrameworkCore.MySql` 0.5.0 references
`Microting.EntityFrameworkCore.MySql` 10.0.12 (MIT), which depends on
`Microsoft.EntityFrameworkCore.Relational` 10.0.13 (MIT) and
`MySqlConnector` 2.6.2 (MIT). The provider is an optional package; consumers
that use SQLite or another EF provider do not acquire this dependency through
`NetArcaWs` or `NetArcaWs.EntityFrameworkCore`. NuGet lists 10.0.12 as the
latest stable Microting package; its declared license remains MIT. See the
[Microting package](https://www.nuget.org/packages/Microting.EntityFrameworkCore.MySql/10.0.12)
and [MySqlConnector package](https://www.nuget.org/packages/MySqlConnector/2.6.2).
`NetArcaWs.EntityFrameworkCore` also references
`Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational`
10.0.13 (MIT), plus `Microsoft.Extensions.DependencyInjection.Abstractions`
10.0.13 (MIT). The package-consumer smoke project uses
`Microsoft.EntityFrameworkCore.Sqlite` 10.0.13 (MIT) as a development-only
dependency; it is not included in either persistence package.

### Optional PostgreSQL and SQL Server providers

`NetArcaWs.EntityFrameworkCore.PostgreSql` 0.5.0 references
`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, whose restored NuGet manifest
declares the `PostgreSQL` license expression; its `Npgsql` 10.0.3 dependency
declares the same license. These are optional provider dependencies and are not
referenced by the core or base EF package. See the
[Npgsql EF provider](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3),
[Npgsql driver](https://www.nuget.org/packages/Npgsql/10.0.3), and
[PostgreSQL license](https://www.postgresql.org/about/licence/).

`NetArcaWs.EntityFrameworkCore.SqlServer` 0.6.0 references `Microsoft.EntityFrameworkCore.SqlServer` 10.0.13 (MIT) and pins `Microsoft.Data.SqlClient` 7.1.1 (MIT) as a direct dependency; `NetArcaWs.EntityFrameworkCore.Migrations.SqlServer` also pins the same driver version. SqlClient references `Microsoft.Data.SqlClient.SNI.runtime` 7.1.0, whose NuGet manifest points to its included `LICENSE.txt` with the Microsoft Software License Terms; the package contains platform-specific native SNI binaries for Windows. This transitive native component is not licensed as MIT in this notice. Neither NetArcaWs SQL Server package embeds SqlClient or SNI files; NuGet resolves their dependencies for consumers. The packages are optional and neither dependency is referenced by the core or base EF package. The provider metadata and included SNI license file were checked in restored NuGet artifacts on 2026-10-14. See
[EF Core SQL Server](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.13)
and [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/7.1.1),
plus SqlClient's [NOTICE](https://github.com/dotnet/SqlClient/blob/main/NOTICE.txt)
and [copyright/license notes](https://github.com/dotnet/SqlClient/blob/main/COPYRIGHT.md).

The five migration extras reference `Microsoft.EntityFrameworkCore.Design`
10.0.13 (MIT) with `PrivateAssets="all"`; EF Design is build-time tooling and
does not flow as a consumer dependency. The migration extras use the EF provider
packages documented above: Microsoft EF SQLite (MIT), Microting for MySQL and
MariaDB (MIT), Npgsql for PostgreSQL (PostgreSQL license), and Microsoft EF SQL
Server (MIT). `NetArcaWs.Build` is a local .NET maintenance executable and is
not packable. It uses provider packages to generate/check DDL and migration SQL
from the actual models and versioned migrations; they do not become dependencies
of the core or base EF package. No Python runtime or build system is introduced.
The SQL Server SNI native component remains under Microsoft Software License
Terms as stated above, not MIT.

## Test dependencies

| Package | Version | License |
| --- | ---: | --- |
| Microsoft.NET.Test.Sdk | 18.11.0 | MIT |
| xunit.v3 | 4.0.2 | Apache-2.0 |
| xunit.runner.visualstudio | 4.0.1 | Apache-2.0 |
| AwesomeAssertions | 9.6.0 | Apache-2.0 |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.13 | MIT |
| PdfPig | 0.1.16 | Apache-2.0 |
| ZXing.Net | 0.16.11 | Apache-2.0 |
| StbImageSharp | 2.30.16 | Unlicense OR MIT |

### Bundled font resources

The optional `NetArcaWs.Rendering.Pdf` package embeds static Noto Sans Regular
and Bold TrueType fonts from the Noto Sans 2.015 release (hinted Latin-Greek-
Cyrillic subset). Noto Sans is Copyright 2012-2024 Google LLC and licensed under
the SIL Open Font License 1.1; the complete license is included at
`src/NetArcaWs.Rendering.Pdf/Resources/OFL.txt` and in the NuGet package under
`licenses/NotoSans-OFL.txt`. Source release:
https://github.com/notofonts/latin-greek-cyrillic/releases/download/NotoSans-v2.015/NotoSans-v2.015.zip

SHA-256: Regular `478c558ea716033cd60c03438f628dfa75694dcf6b5f6d505a2f05fd2b4f3823`,
Bold `1df075a380fc7cb898acf64c1f7b3b4dd780de3caa860178bf929de35817a913`.

AwesomeAssertions is a community-maintained fork with an Apache-2.0 license,
which permits commercial use subject to its license conditions. See the
[package license](https://www.nuget.org/packages/AwesomeAssertions/9.6.0)
and [project background](https://awesomeassertions.org/about/).
It is referenced only by non-packable test projects with `PrivateAssets="all"`;
it is not a runtime dependency of NetArcaWs or NetArcaWs.Tool.

Earlier NetArcaWs revisions used FluentAssertions 7.2.1, also Apache-2.0.
That version did not impose a commercial license fee, and the published 0.5.0
packages do not include FluentAssertions. FluentAssertions 8 and later use
[different licensing terms](https://fluentassertions.com/introduction#licensing),
with free use for open-source projects and non-commercial use, but a paid license
for commercial use. We replaced it to avoid introducing those restrictions into
contributors' workflows through an upgrade. Dependency review rejects new
FluentAssertions dependencies, and Dependabot ignores that package.

NetArcaWs remains LGPL-3.0-or-later. This permits commercial use subject to the
LGPL obligations; it does not replace third-party licenses or exempt a user from
their conditions. Review the license of every new dependency and version,
including development tools, before merging an update.

## Tool and release package materials

The `NetArcaWs.Tool` package redistributes application dependencies. It includes
the project LGPL texts, this attribution file, and the reviewed per-package
license/copyright/NOTICE materials recorded in `licenses/dependencies.json`.
The inventory is generated from versioned NuGet lockfiles and restored package
metadata; its file hashes make local edits and content drift fail closed. For
MIT and Apache-2.0 packages whose archive has a NOTICE but omits the standard
license text, the project includes the pinned SPDX text while preserving the
package's own copyright metadata and notices.

The reviewed Microsoft.Data.SqlClient.SNI.runtime 7.1.0 license is recorded
only as a reference hash in `licenses/policy.json`. NetArcaWs release artifacts
must not contain that package's native SNI binaries. Consumers that separately
redistribute or deploy those components must review Microsoft's license terms
and their obligations. Managed networking is intended for testing and
troubleshooting, not production use. See `docs/license-distribution.md` for the
scope and limits of the automated audit.
