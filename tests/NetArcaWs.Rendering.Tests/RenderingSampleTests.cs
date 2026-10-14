using NetArcaWs.Documents;
using NetArcaWs.Rendering.Examples;
using NetArcaWs.Rendering.Html;
using NetArcaWs.Rendering.Pdf;
using StbImageSharp;
using UglyToad.PdfPig;
using Xunit;

namespace NetArcaWs.Rendering.Tests;

public sealed class RenderingSampleTests
{
    [Fact]
    public void Every_generated_sample_html_and_pdf_has_visible_synthetic_notice()
    {
        const string notice = "MUESTRA SINTETICA - SIN VALIDEZ FISCAL";
        string root = Path.Combine(Path.GetTempPath(), $"netarcaws-gallery-notice-{Guid.NewGuid():N}");
        try
        {
            string run = SampleGallery.Generate(root);
            string[] htmlFiles = Directory.GetFiles(run, "comprobante.html", SearchOption.AllDirectories);
            string[] pdfFiles = Directory.GetFiles(run, "comprobante.pdf", SearchOption.AllDirectories);
            Assert.Equal(36, htmlFiles.Length);
            Assert.Equal(36, pdfFiles.Length);
            foreach (string path in htmlFiles)
                Assert.Contains(notice, File.ReadAllText(path), StringComparison.Ordinal);
            foreach (string path in pdfFiles)
                Assert.Contains(notice, PdfText(File.ReadAllBytes(path)), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SampleLogo_ContainsCenteredReadableWordmarkWithSafePadding()
    {
        byte[] png = SampleGallery.CreateCases().Single(item => item.Id == "wsfe-a-ri-logo").Issuer.LogoPng!;
        ImageResult image = ImageResult.FromMemory(png, ColorComponents.RedGreenBlue);
        Assert.Equal(240, image.Width);
        Assert.Equal(240, image.Height);
        int[] blackXs = Enumerable.Range(0, image.Height)
            .SelectMany(y => Enumerable.Range(0, image.Width).Where(x => image.Data[(y * image.Width + x) * 3] < 32))
            .ToArray();
        Assert.NotEmpty(blackXs);
        Assert.InRange(blackXs.Min(), 20, 40);
        Assert.InRange(blackXs.Max(), 199, 219);
    }

    [Fact]
    public void ReceiptWithLogo_EmbedsLogoImage()
    {
        SampleCase sample = SampleGallery.CreateCases().Single(item => item.Id == "x-recibo-parcial");
        DatosEmisor withLogo = new(sample.Issuer.Identificacion, sample.Issuer.RazonSocial, "La Tienda", sample.Issuer.Domicilio,
            sample.Issuer.CondicionTributaria, SampleGallery.CreateCases().Single(item => item.Id == "wsfe-a-ri-logo").Issuer.LogoPng);
        byte[] pdf = new ArcaWsGeneradorPDF(withLogo).GenerarPDFComprobante(sample.Document);
        using PdfDocument document = PdfDocument.Open(pdf);
        Assert.NotEmpty(document.GetPages().Single().GetImages());
        Assert.Contains("Recibo", PdfText(pdf));

        DatosEmisor withoutLogo = new(sample.Issuer.Identificacion, sample.Issuer.RazonSocial, "La Tienda", sample.Issuer.Domicilio,
            sample.Issuer.CondicionTributaria);
        byte[] plainPdf = new ArcaWsGeneradorPDF(withoutLogo).GenerarPDFComprobante(sample.Document);
        double logoHeaderTop = IssuerNameTop(pdf);
        double plainHeaderTop = IssuerNameTop(plainPdf);
        Assert.InRange(Math.Abs(plainHeaderTop - logoHeaderTop), 0, 25);
    }

    [Fact]
    public void GeneratedGallery_EmbedsExpectedLogoAndQrImagesInEveryPdf()
    {
        string root = Path.Combine(Path.GetTempPath(), $"netarcaws-gallery-pdf-{Guid.NewGuid():N}");
        try
        {
            string run = SampleGallery.Generate(root);
            string[] pdfs = Directory.GetFiles(run, "comprobante.pdf", SearchOption.AllDirectories);
            Assert.Equal(36, pdfs.Length);
            foreach (string path in pdfs)
            {
                string scenario = Path.GetFileName(Path.GetDirectoryName(path))!;
                bool hasLogo = !scenario.Contains("-sin-logo-", StringComparison.Ordinal);
                bool isFiscal = !scenario.StartsWith("x-recibo-", StringComparison.Ordinal);
                int expectedImages = (hasLogo ? 1 : 0) + (isFiscal ? 1 : 0);
                using PdfDocument pdf = PdfDocument.Open(path);
                Assert.True(expectedImages == pdf.GetPages().First().GetImages().Count(),
                    $"Unexpected image count for {scenario}: expected {expectedImages}.");
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FiscalSampleScenarios_UseUniqueArcaIdentityAcrossServices()
    {
        var duplicates = SampleGallery.CreateCases()
            .Where(sample => sample.Document.Tipo == TipoComprobanteGenerado.Fiscal)
            .GroupBy(sample =>
            {
                IdentidadComprobante identity = sample.Document.Identidad!;
                return (identity.Ambiente, identity.Cuit, identity.PuntoVenta, identity.Tipo, identity.Numero);
            })
            .Where(group => group.Count() > 1)
            .Select(group => new { Identity = group.Key, Scenarios = group.Select(sample => sample.Id).ToArray() })
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void SqliteJsonRoundTrip_RendersSameFiscalContentWithoutNetwork()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"netarcaws-rendering-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string database = Path.Combine(directory, "sample.db");
            SampleCase sample = SampleGallery.CreateCases().Single(item => item.Id == "wsfe-a-ri-logo");
            ExampleDocumentStore.Save(database, sample.Document, sample.Issuer);
            (ComprobanteGenerado document, DatosEmisor issuer) = ExampleDocumentStore.Load(database);

            string beforeHtml = new ArcaWsGeneradorHTML(sample.Issuer).GenerarHTMLComprobante(sample.Document);
            string afterHtml = new ArcaWsGeneradorHTML(issuer).GenerarHTMLComprobante(document);
            byte[] beforePdf = new ArcaWsGeneradorPDF(sample.Issuer).GenerarPDFComprobante(sample.Document);
            byte[] afterPdf = new ArcaWsGeneradorPDF(issuer).GenerarPDFComprobante(document);

            Assert.Equal(beforeHtml, afterHtml);
            Assert.Equal(PdfText(beforePdf), PdfText(afterPdf));
            Assert.Equal(sample.Document.Identidad, document.Identidad);
            Assert.Equal(sample.Document.Iva, document.Iva);
            Assert.Equal(sample.Issuer.LogoPng, issuer.LogoPng);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void Cleanup_DeletesOnlyRecognizedExpiredRunsAndLeavesLinksAndFreshRunsAlone()
    {
        string root = Path.Combine(Path.GetTempPath(), $"netarcaws-gallery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string outside = Path.Combine(Path.GetTempPath(), $"netarcaws-outside-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outside);
        try
        {
            string expired = Path.Combine(root, "20260101T000000Z-11111111");
            string fresh = Path.Combine(root, "20260101T010000Z-22222222");
            string unknown = Path.Combine(root, "unknown");
            string unsafeRun = Path.Combine(root, "20260101T020000Z-33333333");
            Directory.CreateDirectory(expired);
            Directory.CreateDirectory(fresh);
            Directory.CreateDirectory(unknown);
            Directory.CreateDirectory(unsafeRun);
            ExampleDocumentStore.WriteRunManifest(expired, DateTimeOffset.UtcNow.AddHours(-73));
            ExampleDocumentStore.WriteRunManifest(fresh, DateTimeOffset.UtcNow.AddHours(-71));
            ExampleDocumentStore.WriteRunManifest(unsafeRun, DateTimeOffset.UtcNow.AddHours(-73));
            Directory.CreateSymbolicLink(Path.Combine(unsafeRun, "external"), outside);
            Directory.CreateSymbolicLink(Path.Combine(root, "linked"), outside);

            ExampleDocumentStore.CleanExpiredRuns(root, DateTimeOffset.UtcNow);

            Assert.False(Directory.Exists(expired));
            Assert.True(Directory.Exists(fresh));
            Assert.True(Directory.Exists(unknown));
            Assert.True(Directory.Exists(unsafeRun));
            Assert.True(Directory.Exists(outside));
            Assert.True(new DirectoryInfo(Path.Combine(root, "linked")).LinkTarget is not null);
            string linkedRoot = Path.Combine(Path.GetTempPath(), $"netarcaws-linked-root-{Guid.NewGuid():N}");
            Directory.CreateSymbolicLink(linkedRoot, outside);
            try { Assert.Equal(0, ExampleDocumentStore.CleanExpiredRuns(linkedRoot, DateTimeOffset.UtcNow)); }
            finally { Directory.Delete(linkedRoot); }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
    }

    private static string PdfText(byte[] bytes)
    {
        using PdfDocument document = PdfDocument.Open(bytes);
        return string.Join("\n", document.GetPages().Select(page => page.Text));
    }

    private static double IssuerNameTop(byte[] bytes)
    {
        using PdfDocument document = PdfDocument.Open(bytes);
        return document.GetPages().Single().GetWords().Single(word => word.Text == "Comercial").BoundingBox.Top;
    }
}
