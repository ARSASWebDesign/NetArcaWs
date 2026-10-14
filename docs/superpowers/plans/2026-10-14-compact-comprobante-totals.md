# Comprobante rendering feedback

1. Add RED/GREEN coverage for four-column detail/summary rows, compact total and tax disclosures, consumer IVA bases, multiple rates, contacts, explicit document titles, export field grouping, and renderer versions.
2. Update HTML and PDF consistently: items or honest WSFE summaries, total before compact tax rows, known bases only, one legal consumer legend, source-preserved amounts/classifications, clear type/letter title, two-column WSFEX fields, escaped optional contacts, a stacked logo at preserved proportions, and named currency labels with export rate shown as es-AR `N2` while retaining its full source precision.
3. Extend the synthetic gallery with WSFE contacts, two MTXCA multi-rate examples (two items for A and three for B), and a wide logo; retain prior cases and update sample-count/smoke assertions.
4. Run rendering tests, regenerate the gallery with the .NET examples tool, inspect representative output, and commit only the scoped plan, renderers, fixtures, and tests.
