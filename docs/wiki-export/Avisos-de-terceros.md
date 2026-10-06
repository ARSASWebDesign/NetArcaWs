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

## Test dependencies

| Package | Version | License |
| --- | ---: | --- |
| Microsoft.NET.Test.Sdk | 18.10.1 | MIT |
| xunit.v3 | 4.0.1 | Apache-2.0 |
| xunit.runner.visualstudio | 4.0.0 | Apache-2.0 |
| FluentAssertions | 7.2.1 | Apache-2.0 |

FluentAssertions 7.2.1 is licensed under Apache-2.0, not MIT. The v7 package is used in this scaffold; no claim of MIT licensing is made. See each package's NuGet Gallery page for its license and upstream details.
