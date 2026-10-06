<!-- Source: THIRD-PARTY-NOTICES.md. Generated wiki mirror; edit the repository source. -->

# Third-party notices

This port derives from PyAfipWs, Copyright (C) 2008-2021 Mariano Reingart,
licensed LGPL-3.0-or-later. The .NET adaptation changes cryptographic APIs,
transport, error handling and cache behavior; see docs/plans/hito-1.md.
Original source: https://github.com/reingart/pyafipws/tree/d595b072110accec9dae1ddb58165ab847b8520a

Versions and license declarations were checked against NuGet.org and restored
package manifests on 2026-10-06. Production dependency graph licenses include
MIT and Apache-2.0. Additional transitive packages are
Microsoft.Extensions.Caching.Abstractions, Configuration, Configuration.Abstractions,
Configuration.Binder, DependencyInjection, DependencyInjection.Abstractions,
Diagnostics, Diagnostics.Abstractions, Logging, Logging.Abstractions,
Options.ConfigurationExtensions, Primitives, Diagnostics.HealthChecks.Abstractions,
FileProviders.Abstractions and Hosting.Abstractions, all version 10.0.12.
These NuGet dependencies are referenced, not embedded into this library's DLL.

## Library dependencies

| Package | Version | License |
| --- | ---: | --- |
| Microsoft.Extensions.Caching.Memory | 10.0.12 | MIT |
| Microsoft.Extensions.Http | 10.0.12 | MIT |
| Microsoft.Extensions.Options | 10.0.12 | MIT |
| Microsoft.Extensions.Diagnostics.HealthChecks | 10.0.12 | MIT |
| System.Security.Cryptography.Pkcs | 10.0.12 | MIT |
| Microsoft.Data.Sqlite | 10.0.12 | MIT |

### Transitive library dependencies added by SQLite

`Microsoft.Data.Sqlite` 10.0.12 resolves `Microsoft.Data.Sqlite.Core` 10.0.12
(MIT), `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12, `SQLitePCLRaw.core` 2.1.12,
`SQLitePCLRaw.provider.e_sqlite3` 2.1.12 and `SQLitePCLRaw.lib.e_sqlite3`
2.1.12. The SQLitePCLRaw packages declare Apache-2.0. The bundle includes the
native SQLite amalgamation; its upstream SQLite project is public domain, while
the SQLitePCLRaw packaging/provider code remains under the package's declared
Apache-2.0 license (Copyright 2014-2024 SourceGear, LLC). See the
[SQLitePCLRaw project notices](https://github.com/ericsink/SQLitePCL.raw/blob/v2.1.12/README.md),
[SQLite project copyright](https://www.sqlite.org/copyright.html), and the
[NuGet license declaration](https://www.nuget.org/packages/SQLitePCLRaw.lib.e_sqlite3/2.1.12).
The library references these packages and does not statically link them into
its own managed assembly; deployment assets can include their native runtime
library.

### Optional MySQL and MariaDB provider

`NetArcaWs.EntityFrameworkCore.MySql` 0.5.0 references
`Microting.EntityFrameworkCore.MySql` 10.0.12 (MIT), which depends on
`Microsoft.EntityFrameworkCore.Relational` 10.0.12 (MIT) and
`MySqlConnector` 2.6.2 (MIT). The provider is an optional package; consumers
that use SQLite or another EF provider do not acquire this dependency through
`NetArcaWs` or `NetArcaWs.EntityFrameworkCore`. The package declarations were
checked in the NuGet package manifests and Gallery on 2026-10-06. See the
[Microting package](https://www.nuget.org/packages/Microting.EntityFrameworkCore.MySql/10.0.12)
and [MySqlConnector package](https://www.nuget.org/packages/MySqlConnector/2.6.2).
`NetArcaWs.EntityFrameworkCore` also references
`Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational`
10.0.12 (MIT), plus `Microsoft.Extensions.DependencyInjection.Abstractions`
10.0.12 (MIT). The package-consumer smoke project uses
`Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 (MIT) as a development-only
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

`NetArcaWs.EntityFrameworkCore.SqlServer` 0.5.0 references
`Microsoft.EntityFrameworkCore.SqlServer` 10.0.12 (MIT), which resolves
`Microsoft.Data.SqlClient` 6.1.6 (MIT). SqlClient in turn references
`Microsoft.Data.SqlClient.SNI.runtime` 6.0.2, whose NuGet manifest points to
its included `LICENSE.txt` with the Microsoft Software License Terms; the
package contains platform-specific native SNI binaries for Windows. This
transitive native component is not licensed as MIT in this notice. The
NetArcaWs SQL Server package itself does not embed SqlClient or SNI files;
NuGet resolves their dependencies for consumers. Both provider packages are
optional and neither dependency is referenced by the core or base EF package.
The provider metadata and included SNI license file were checked in restored
NuGet artifacts on 2026-10-06. See
[EF Core SQL Server](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.12)
and [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/6.1.6),
plus SqlClient's [NOTICE](https://github.com/dotnet/SqlClient/blob/main/NOTICE.txt)
and [copyright/license notes](https://github.com/dotnet/SqlClient/blob/main/COPYRIGHT.md).

The native Build tool uses provider packages as development-only dependencies
to emit DDL from the actual EF models. The scripts do not bundle provider
assemblies into any NetArcaWs library package.

## Test dependencies

| Package | Version | License |
| --- | ---: | --- |
| Microsoft.NET.Test.Sdk | 18.10.1 | MIT |
| xunit.v3 | 4.0.1 | Apache-2.0 |
| xunit.runner.visualstudio | 4.0.0 | Apache-2.0 |
| AwesomeAssertions | 9.6.0 | Apache-2.0 |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.12 | MIT |

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
