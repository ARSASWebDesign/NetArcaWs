using System.Reflection;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class FiscalHomologationWsmtxcaCatalogueTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(6, 5)]
    [InlineData(2, 1)]
    [InlineData(7, 5)]
    public async Task Receiver_condition_lookup_sends_the_current_voucher_type(int voucherType, int vatCondition)
    {
        IWsmtxcav1Service service = DispatchProxy.Create<IWsmtxcav1Service, WsmtxcaCatalogueProxy>();
        var fake = (WsmtxcaCatalogueProxy)(object)service;
        fake.ReceiverVatCondition = vatCondition;
        var receiver = voucherType is 1 or 2
            ? new FiscalHomologationReceiver(80, 20_000_000_001, 1, FixtureReviewed: true)
            : new FiscalHomologationReceiver(99, 0, 5, FixtureReviewed: true);
        var channel = new FiscalHomologationWsmtxcaChannel(service, null!, receiver,
            new FiscalHomologationReceiver(99, 0, 5, FixtureReviewed: true));
        channel.ValidateScenario(new("wsmtxca", voucherType, $"wsmtxca-{voucherType}"), 2, receiver);

        Assert.True(await channel.IsVoucherTypeAvailableAsync(Tenant(), voucherType, CancellationToken.None));
        Assert.NotNull(fake.ReceiverRequest);
        ConsultaCondicionesIvaRequestType filter = Assert.IsType<ConsultaCondicionesIvaRequestType>(
            fake.ReceiverRequest!.ConsultaCondicionesIvaReceptorRequest);
        Assert.Equal(voucherType, filter.CodigoTipoComprobante);
    }

    private static ArcaTenantContext Tenant() => new("catalogue-test", 20_000_000_001,
        ArcaEnvironment.Homologation, WsaaCertificateContent.FromPem("synthetic certificate", "synthetic key"));

    public class WsmtxcaCatalogueProxy : DispatchProxy
    {
        public ConsultarCondicionesIvaReceptorRequestType? ReceiverRequest { get; private set; }
        public int ReceiverVatCondition { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            object?[] arguments = args ?? [];
            return targetMethod!.Name switch
            {
                nameof(IWsmtxcav1Service.consultarTiposComprobanteAsync) => Task.FromResult(Types()),
                nameof(IWsmtxcav1Service.consultarCondicionesIVAReceptorAsync) => ReceiverConditions(arguments),
                nameof(IWsmtxcav1Service.consultarPuntosVentaCAEAsync) => Task.FromResult(Points()),
                _ => throw new NotSupportedException($"Unexpected offline WSMTXCA lookup: {targetMethod.Name}.")
            };
        }

        private object ReceiverConditions(object?[] arguments)
        {
            ReceiverRequest = Assert.IsType<ConsultarCondicionesIvaReceptorRequestType>(arguments[1]);
            var response = new ConsultarCondicionesIvaReceptorResponseType();
            response.ArrayCondicionesIvaReceptor.Add(new CodigoDescripcionType
            { Codigo = checked((short)ReceiverVatCondition), Descripcion = "Synthetic receiver condition" });
            return Task.FromResult(response);
        }

        private static ConsultarTiposComprobanteResponseType Types()
        {
            var response = new ConsultarTiposComprobanteResponseType();
            foreach (int type in new[] { 1, 2, 6, 7 })
                response.ArrayTiposComprobante.Add(new CodigoDescripcionType { Codigo = checked((short)type), Descripcion = $"Tipo {type}" });
            return response;
        }

        private static ConsultarPuntosVentaCaeResponse Points()
        {
            var response = new ConsultarPuntosVentaCaeResponse();
            response.ArrayPuntosVenta.Add(new PuntoVentaType { NumeroPuntoVenta = 2, Bloqueado = SiNoSimpleType.N });
            return response;
        }
    }
}
