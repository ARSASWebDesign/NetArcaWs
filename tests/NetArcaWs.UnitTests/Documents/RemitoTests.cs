using System.Text.Json;
using AwesomeAssertions;
using NetArcaWs.Documents;
using Xunit;

namespace NetArcaWs.UnitTests.Documents;

public sealed class RemitoTests
{
    private static DatosEmisor Emisor(string condicion = "Responsable Inscripto") => new(
        30712345678, "Comercial Ejemplo SRL", null, "Av. Siempre Viva 123", condicion,
        ingresosBrutos: "901-123456-7", inicioActividades: new DateOnly(2010, 1, 1));

    private static DatosReceptor Receptor() => new(20123456789, "Cliente Ejemplo", "Calle Falsa 456", "Responsable Inscripto");

    private static DatosRemito RemitoR() => new(ClaseRemito.R, new DateOnly(2025, 4, 1), 12, 345,
        Receptor(), "Depósito central", [new("Tornillos", "Caja", 2m)],
        cai: "12345678901234", primerNumero: 300, ultimoNumero: 399, vencimiento: new DateOnly(2025, 12, 31),
        imprenta: new DatosImprentaRemito("Imprenta Ejemplo SA", 30711111119, new DateOnly(2025, 3, 1), 300, 399, "HAB-123"));

    [Fact]
    public void Remito_R_generado_es_inmutable_serializable_y_no_declara_autorizacion_fiscal()
    {
        var source = new[] { new LineaRemito("Tornillos", "Caja", 2m) };
        var data = RemitoR() with { Mercaderia = source };
        var generated = new Remito(Emisor()).GenerarComprobante(data);
        source[0] = new("Alterado", "Unidad", 99m);

        var roundtrip = JsonSerializer.Deserialize<RemitoGenerado>(JsonSerializer.Serialize(generated))!;

        roundtrip.Clase.Should().Be(ClaseRemito.R);
        roundtrip.CodigoTipoComprobante.Should().Be(91);
        roundtrip.Mercaderia.Should().ContainSingle().Which.Descripcion.Should().Be("Tornillos");
        roundtrip.Cai.Should().Be("12345678901234");
        roundtrip.Estado.Should().Be("Local");
        roundtrip.GetType().GetProperty("Resultado").Should().BeNull();
        roundtrip.ToString().Should().NotContain("Cliente Ejemplo");
        RemitoValidator.Validate(roundtrip);
        ((Action)(() => ((IList<LineaRemito>)roundtrip.Mercaderia)[0] = new("Alterado", "Unidad", 1m)))
            .Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Remito_R_historico_se_valida_con_vigencia_y_rango_de_emision()
    {
        var generated = new Remito(Emisor()).GenerarComprobante(RemitoR());
        generated.FechaEmision.Should().Be(new DateOnly(2025, 4, 1));
    }

    [Fact]
    public void Remito_R_rechaza_CAI_rango_fecha_o_impresor_inconsistentes()
    {
        var factory = new Remito(Emisor());
        ((Action)(() => factory.GenerarComprobante(RemitoR() with { Cai = "123" }))).Should().Throw<ArgumentException>();
        ((Action)(() => factory.GenerarComprobante(RemitoR() with { Numero = 400 }))).Should().Throw<ArgumentException>();
        ((Action)(() => factory.GenerarComprobante(RemitoR() with { FechaEmision = new DateOnly(2026, 1, 1) }))).Should().Throw<ArgumentException>();
        ((Action)(() => factory.GenerarComprobante(RemitoR() with { Imprenta = null }))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Remito_R_autoimpresor_explicito_omite_solo_los_datos_de_imprenta_y_rango()
    {
        var data = RemitoR() with { PrimerNumero = null, UltimoNumero = null, Imprenta = null, EsAutoimpresor = true };
        var generated = new Remito(Emisor()).GenerarComprobante(data);
        generated.EsAutoimpresor.Should().BeTrue();
        generated.Cai.Should().Be("12345678901234");
        ((Action)(() => new Remito(Emisor("Monotributo")).GenerarComprobante(data))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Remito_X_solo_se_genera_con_ambito_interno_y_sin_datos_fiscales_R()
    {
        var data = RemitoR() with
        {
            Clase = ClaseRemito.X, Numero = 22, Cai = null, PrimerNumero = null, UltimoNumero = null,
            Vencimiento = null, Imprenta = null, AmbitoInterno = "Parque Industrial Norte"
        };

        var generated = new Remito(Emisor()).GenerarComprobante(data);

        generated.CodigoTipoComprobante.Should().BeNull();
        generated.Cai.Should().BeNull();
        generated.AmbitoInterno.Should().Be("Parque Industrial Norte");
        generated.Leyenda.Should().Be("DOCUMENTO NO VALIDO COMO FACTURA");
        ((Action)(() => new Remito(Emisor()).GenerarComprobante(data with { AmbitoInterno = null }))).Should().Throw<ArgumentException>();
        ((Action)(() => new Remito(Emisor()).GenerarComprobante(data with { Cai = "12345678901234" }))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Remito_rechaza_perfiles_datos_de_receptor_y_mercaderia_incompletos()
    {
        var factory = new Remito(Emisor());
        ((Action)(() => new Remito(new DatosEmisor(123, "Emisor", null, "Domicilio", "RI")).GenerarComprobante(RemitoR()))).Should().Throw<ArgumentException>();
        ((Action)(() => factory.GenerarComprobante(RemitoR() with { Receptor = null! }))).Should().Throw<ArgumentException>();
        ((Action)(() => factory.GenerarComprobante(RemitoR() with { Receptor = Receptor() with { Domicilio = null } }))).Should().Throw<ArgumentException>();
        ((Action)(() => factory.GenerarComprobante(RemitoR() with { Mercaderia = [new("Caja", "", 0m)] }))).Should().Throw<ArgumentException>();
    }
}
