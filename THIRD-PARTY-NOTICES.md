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
