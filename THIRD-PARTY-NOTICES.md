# Third-party notices

This port derives from PyAfipWs, Copyright (C) 2008-2021 Mariano Reingart,
licensed LGPL-3.0-or-later. The .NET adaptation changes cryptographic APIs,
transport, error handling and cache behavior; see docs/plans/hito-1.md.
Original source: https://github.com/reingart/pyafipws/tree/d595b072110accec9dae1ddb58165ab847b8520a

Versions and license declarations were checked against NuGet.org and restored
package manifests on 2026-10-06. The production dependency graph was inspected:
all 16 restored dependencies declare MIT. Additional transitive packages are
Microsoft.Extensions.Caching.Abstractions, Configuration, Configuration.Abstractions,
Configuration.Binder, DependencyInjection, DependencyInjection.Abstractions,
Diagnostics, Diagnostics.Abstractions, Logging, Logging.Abstractions,
Options.ConfigurationExtensions and Primitives, all version 10.0.12.
These NuGet dependencies are referenced, not embedded into this library's DLL.

## Library dependencies

| Package | Version | License |
| --- | ---: | --- |
| Microsoft.Extensions.Caching.Memory | 10.0.12 | MIT |
| Microsoft.Extensions.Http | 10.0.12 | MIT |
| Microsoft.Extensions.Options | 10.0.12 | MIT |
| System.Security.Cryptography.Pkcs | 10.0.12 | MIT |

## Test dependencies

| Package | Version | License |
| --- | ---: | --- |
| Microsoft.NET.Test.Sdk | 18.10.1 | MIT |
| xunit.v3 | 4.0.1 | Apache-2.0 |
| xunit.runner.visualstudio | 4.0.0 | Apache-2.0 |
| FluentAssertions | 7.2.1 | Apache-2.0 |

FluentAssertions 7.2.1 is licensed under Apache-2.0, not MIT. The v7 package is used in this scaffold; no claim of MIT licensing is made. See each package's NuGet Gallery page for its license and upstream details.
