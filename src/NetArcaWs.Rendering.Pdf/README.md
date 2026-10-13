# NetArcaWs.Rendering.Pdf

Optional LGPL-3.0-or-later package that renders a previously created
`ComprobanteGenerado` snapshot as PDF using .NET libraries. It does not
authorize, query, persist or retry fiscal operations. Only authorized fiscal
snapshots and local X receipts can be rendered.

The constructor takes an immutable `DatosEmisor`; the receiver always comes
from `ComprobanteGenerado.Receptor`. For historical reproduction, persist the
issuer profile and optional PNG logo alongside the snapshot. WSFE snapshots
contain fiscal totals, not invented item details. The initial scope covers
unitary CAE WSFEv1, WSFEXv1 and WSMTXCA snapshots plus local receipts X, subject
to the types explicitly accepted by the source adapters. This implementation
has local synthetic tests; it does not establish homologation or real ARCA
authorization.

```csharp
var renderer = new ArcaWsGeneradorPDF(issuerProfile);
byte[] pdf = renderer.GenerarPDFComprobante(persistedDocument);
```

PDFsharp uses a process-wide font resolver. This package automatically installs
its immutable embedded Noto Sans resolver on first PDF use; if another
component has already installed a different resolver, rendering fails clearly
instead of replacing that process configuration. The issuer profile and logo
are never stored in that global resolver. PNG normalization occurs in memory
through StbImageSharp; the renderer does not call System.Drawing APIs. QRCoder
and its transitive dependency metadata may still include System.Drawing.Common.

Fonts are from Noto Sans 2.015 and licensed under SIL OFL 1.1. The package
contains the OFL text under `licenses/NotoSans-OFL.txt`, as well as the
repository `LICENSE` and `THIRD-PARTY-NOTICES.md`.
