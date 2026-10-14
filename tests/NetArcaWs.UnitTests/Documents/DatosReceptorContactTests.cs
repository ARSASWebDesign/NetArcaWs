using System.Text.Json;
using AwesomeAssertions;
using NetArcaWs.Documents;
using Xunit;

namespace NetArcaWs.UnitTests.Documents;

public sealed class DatosReceptorContactTests
{
    [Fact]
    public void Json_anterior_sin_contactos_se_deserializa_con_contactos_nulos()
    {
        const string json = """{"Identificacion":20123456789,"Nombre":"Cliente Ejemplo","Domicilio":"Calle Falsa 456","CondicionTributaria":"Consumidor Final","CodigoTipoDocumento":null,"IdentificacionFiscal":null}""";

        var receptor = JsonSerializer.Deserialize<DatosReceptor>(json)!;

        receptor.Identificacion.Should().Be(20123456789);
        receptor.Telefono.Should().BeNull();
        receptor.Email.Should().BeNull();
    }

    [Fact]
    public void Json_conserva_telefono_y_email_del_receptor()
    {
        var receptor = new DatosReceptor(20123456789, "Cliente Ejemplo", "Calle Falsa 456", "Consumidor Final",
            Telefono: "+54 11 5555-0101", Email: "cliente@example.test");

        var restored = JsonSerializer.Deserialize<DatosReceptor>(JsonSerializer.Serialize(receptor))!;

        restored.Telefono.Should().Be("+54 11 5555-0101");
        restored.Email.Should().Be("cliente@example.test");
    }

    [Fact]
    public void ToString_no_expone_telefono_ni_email()
    {
        var receptor = new DatosReceptor(20123456789, "Cliente Ejemplo", "Calle Falsa 456", "Consumidor Final",
            Telefono: "+54 11 5555-0101", Email: "cliente@example.test");

        receptor.ToString().Should().NotContain("+54 11 5555-0101");
        receptor.ToString().Should().NotContain("cliente@example.test");
    }
}
