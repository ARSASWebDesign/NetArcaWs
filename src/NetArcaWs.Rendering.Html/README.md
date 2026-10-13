# NetArcaWs.Rendering.Html

Optional LGPL-3.0-or-later package that renders a previously created
`ComprobanteGenerado` snapshot as a self-contained HTML document. It does not
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
var renderer = new ArcaWsGeneradorHTML(issuerProfile);
string html = renderer.GenerarHTMLComprobante(persistedDocument);
```

The output embeds its styles and QR image. User text is HTML-escaped and no
remote content is loaded. Fiscal QR data follows the ARCA QR specification.
Receipts X display `DOCUMENTO NO VALIDO COMO FACTURA` and no fiscal QR or CAE.

QRCoder is an optional dependency of this package. Its dependency graph
declares System.Drawing.Common, but this renderer uses the platform-neutral PNG
implementation and does not call System.Drawing APIs. See the bundled
`THIRD-PARTY-NOTICES.md` and `LICENSE` for notices and license terms.
