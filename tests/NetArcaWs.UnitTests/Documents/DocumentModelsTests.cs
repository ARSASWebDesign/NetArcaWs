using System.Text.Json;
using System.Buffers.Binary;
using AwesomeAssertions;
using NetArcaWs.Documents;
using Xunit;

namespace NetArcaWs.UnitTests.Documents;

public sealed class DocumentModelsTests
{
    private static DatosEmisor Emisor(byte[]? logo = null) => new(
        30712345678, "Comercial Ejemplo SRL", "La Tienda", "Av. Siempre Viva 123",
        "Responsable Inscripto", logo);

    private static DatosReceptor Receptor() => new(20123456789, "Cliente Ejemplo", "Calle Falsa 456", "Consumidor Final");

    [Fact]
    public void Emisor_logo_es_defensivo_y_no_expone_datos_en_metadatos()
    {
        var source = CreatePng();
        var emisor = Emisor(source);
        source[0] = 0;
        var exposed = emisor.LogoPng!;
        exposed[0] = 0;

        emisor.LogoPng![0].Should().Be(0x89);
        emisor.ToString().Should().NotContain("30712345678");
        emisor.ToString().Should().NotContain("Comercial Ejemplo");
    }

    [Fact]
    public void Rechaza_logo_png_corrupto_o_mayor_a_un_mib()
    {
        ((Action)(() => Emisor([1, 2, 3]))).Should().Throw<ArgumentException>();
        ((Action)(() => Emisor(new byte[1024 * 1024 + 1]))).Should().Throw<ArgumentException>();

        var corrupt = CreatePng();
        corrupt[^5] ^= 0x20;
        ((Action)(() => Emisor(corrupt))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Acepta_png_con_metadatos_textuales_antes_de_la_imagen()
    {
        var png = CreatePng();
        var idat = Find(png, "IDAT");
        var text = Chunk("tEXt", "Title\0Logo"u8.ToArray());
        var withText = new byte[png.Length + text.Length];
        png.AsSpan(0, idat).CopyTo(withText);
        text.CopyTo(withText.AsSpan(idat));
        png.AsSpan(idat).CopyTo(withText.AsSpan(idat + text.Length));

        Emisor(withText).LogoPng.Should().NotBeNull();
    }

    private static int Find(byte[] png, string name)
    {
        for (var i = 8; i < png.Length - 8; i++)
            if (System.Text.Encoding.ASCII.GetString(png, i + 4, 4) == name) return i;
        throw new InvalidOperationException("Chunk not found.");
    }

    private static byte[] Chunk(string type, byte[] content)
    {
        var result = new byte[content.Length + 12];
        BinaryPrimitives.WriteUInt32BigEndian(result, (uint)content.Length);
        System.Text.Encoding.ASCII.GetBytes(type).CopyTo(result, 4);
        content.CopyTo(result, 8);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(content.Length + 8), Crc32(result.AsSpan(4, content.Length + 4)));
        return result;
    }

    private static byte[] CreatePng()
    {
        using var data = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(data, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write([0, 0x10, 0x20, 0x30, 0xff]);
        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, 1);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), 1);
        header[8] = 8;
        header[9] = 6;
        return new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }
            .Concat(Chunk("IHDR", header)).Concat(Chunk("IDAT", data.ToArray())).Concat(Chunk("IEND", [])).ToArray();
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xffffffff;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1));
        }
        return ~crc;
    }

    [Fact]
    public void Recibo_x_total_puede_serializarse_y_conservar_colecciones_inmutables()
    {
        var detalles = new[] { new LineaReciboX("Servicio", 100m, "ARS") };
        var pagos = new[] { new PagoReciboX("Transferencia", 100m, "ARS") };
        var datos = new DatosReciboX("RX-001", DateOnly.Parse("2026-10-13"), Receptor(), "ARS", 100m, detalles, pagos);
        var receipt = new ReciboX(Emisor()).GenerarComprobante(datos);
        detalles[0] = new("Alterado", 1m, "USD");
        pagos[0] = new("Alterado", 1m, "USD");

        var json = JsonSerializer.Serialize(receipt);
        var restored = JsonSerializer.Deserialize<ComprobanteGenerado>(json)!;

        restored.Version.Should().Be(1);
        restored.Tipo.Should().Be(TipoComprobanteGenerado.ReciboX);
        restored.Total.Should().Be(100m);
        restored.Moneda.Should().Be("ARS");
        restored.Detalles[0].Descripcion.Should().Be("Servicio");
        restored.Pagos[0].Medio.Should().Be("Transferencia");
        restored.ToString().Should().NotContain("30712345678");
        restored.ToString().Should().NotContain("Cliente Ejemplo");
    }

    [Fact]
    public void Recibo_x_parcial_permite_varias_imputaciones_con_montos_explicitos()
    {
        var datos = new DatosReciboX("RX-002", DateOnly.Parse("2026-10-13"), Receptor(), "ARS", 120m,
            [new("Repuestos", 120m, "ARS")], [new("Efectivo", 120m, "ARS")],
            [new("FC A 0001-00000042", 70m, "ARS"), new("FC B 0002-00000018", 30m, "ARS")]);

        var receipt = new ReciboX(Emisor()).GenerarComprobante(datos);

        receipt.Imputaciones.Count.Should().Be(2);
        receipt.Imputaciones.Sum(x => x.Importe).Should().Be(100m);
        receipt.Imputaciones[0].Referencia.Should().Be("FC A 0001-00000042");
    }

    [Fact]
    public void Recibo_x_rechaza_sumatorias_y_monedas_incoherentes()
    {
        var mismatch = new DatosReciboX("RX-003", DateOnly.Parse("2026-10-13"), Receptor(), "ARS", 100m,
            [new("Servicio", 99m, "ARS")], [new("Efectivo", 100m, "ARS")]);
        ((Action)(() => new ReciboX(Emisor()).GenerarComprobante(mismatch))).Should().Throw<ArgumentException>();

        var currency = new DatosReciboX("RX-004", DateOnly.Parse("2026-10-13"), Receptor(), "ARS", 100m,
            [new("Servicio", 100m, "ARS")], [new("Efectivo", 100m, "USD")]);
        ((Action)(() => new ReciboX(Emisor()).GenerarComprobante(currency))).Should().Throw<ArgumentException>();

        var overApplied = new DatosReciboX("RX-005", DateOnly.Parse("2026-10-13"), Receptor(), "ARS", 100m,
            [new("Servicio", 100m, "ARS")], [new("Efectivo", 100m, "ARS")],
            [new("FC 1", 80m, "ARS"), new("FC 2", 30m, "ARS")]);
        ((Action)(() => new ReciboX(Emisor()).GenerarComprobante(overApplied))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Recibo_x_rechaza_emisor_o_receptor_invalidos()
    {
        var data = new DatosReciboX("RX-006", DateOnly.Parse("2026-10-13"), Receptor(), "ARS", 10m,
            [new("Servicio", 10m, "ARS")], [new("Efectivo", 10m, "ARS")]);
        ((Action)(() => new ReciboX(new DatosEmisor(123, "Emisor", null, "Domicilio", "RI")).GenerarComprobante(data))).Should().Throw<ArgumentException>();
        ((Action)(() => new ReciboX(Emisor()).GenerarComprobante(data with { Receptor = null! }))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Modelo_fiscal_malformado_se_revalida_al_representarse()
    {
        var malformed = new ComprobanteGenerado(1, TipoComprobanteGenerado.Fiscal, null, null, "Authorized",
            Receptor(), "ARS", 10m, [new("Servicio", 11m, "ARS")], [], [], null, null, null,
            cotizacion: 1m, fecha: DateOnly.Parse("2026-10-13"));
        var restored = JsonSerializer.Deserialize<ComprobanteGenerado>(JsonSerializer.Serialize(malformed))!;
        ((Action)(() => ComprobanteValidator.Validate(restored))).Should().Throw<ArgumentException>();
    }
}
