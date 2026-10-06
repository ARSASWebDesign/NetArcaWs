using System.Reflection;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Xml.Serialization;
using AwesomeAssertions;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Transport;
using Xunit;

namespace NetArcaWs.Tests.Services;

public sealed class WsdlContractTests
{
    private static readonly (Type Contract, string File)[] Services =
    [
        (typeof(IWsfev1Service), "wsfev1-production.wsdl"),
        (typeof(IWsfexv1Service), "wsfexv1-production.wsdl"),
        (typeof(IWsmtxcav1Service), "wsmtxca-production.wsdl"),
        (typeof(IWscdcService), "wscdc-production.wsdl"),
        (typeof(IWsfecredService), "wsfecred-production.wsdl"),
        (typeof(IWscpeService), "wscpe-production.wsdl"),
        (typeof(IPadronA4Service), "padron-a4-production.wsdl"),
        (typeof(IPadronA5Service), "padron-a5-production.wsdl"),
        (typeof(IPadronA10Service), "padron-a10-production.wsdl"),
        (typeof(IPadronA13Service), "padron-a13-production.wsdl")
    ];

    [Fact]
    public void Every_generated_operation_and_xml_root_matches_the_pinned_production_wsdl()
    {
        XNamespace wsdl = "http://schemas.xmlsoap.org/wsdl/";
        XNamespace soap = "http://schemas.xmlsoap.org/wsdl/soap/";
        foreach ((Type contract, string file) in Services)
        {
            XDocument document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "contracts", file));
            XElement binding = document.Descendants(wsdl + "binding").Single(x => x.Element(soap + "binding") is not null);
            XName portTypeQName = ResolveQName(binding, (string)binding.Attribute("type")!);
            XElement portType = document.Descendants(wsdl + "portType").Single(x =>
                (string)x.Attribute("name")! == portTypeQName.LocalName &&
                (string)document.Root!.Attribute("targetNamespace")! == portTypeQName.NamespaceName);
            var messages = document.Descendants(wsdl + "message").ToDictionary(x => (string)x.Attribute("name")!, StringComparer.Ordinal);
            var actions = binding.Elements(wsdl + "operation").ToDictionary(
                x => (string)x.Attribute("name")!,
                x => (string?)x.Element(soap + "operation")?.Attribute("soapAction") ?? "",
                StringComparer.Ordinal);
            var wsdlOperations = portType.Elements(wsdl + "operation").ToDictionary(x => (string)x.Attribute("name")!, StringComparer.Ordinal);
            MethodInfo[] methods = contract.GetMethods();
            methods.Should().HaveCount(wsdlOperations.Count, contract.Name);
            methods.Select(m => m.Name[..^"Async".Length]).Should().BeEquivalentTo(wsdlOperations.Keys);

            foreach (MethodInfo method in methods)
            {
                string operation = method.Name[..^"Async".Length];
                XElement wsdlOperation = wsdlOperations[operation];
                string inputMessage = LocalName((string)wsdlOperation.Element(wsdl + "input")!.Attribute("message")!);
                string outputMessage = LocalName((string)wsdlOperation.Element(wsdl + "output")!.Attribute("message")!);
                string? inputElement = MessageElementOrNull(messages[inputMessage], wsdl);
                string outputElement = MessageElement(messages[outputMessage], wsdl);
                Type responseType = method.ReturnType.GetGenericArguments().Single();
                string targetNamespace = (string)document.Root!.Attribute("targetNamespace")!;
                Type? requestType = method.GetParameters().Select(p => p.ParameterType)
                    .FirstOrDefault(t => t != typeof(CancellationToken) && t != typeof(ArcaEnvironment) && t != typeof(ArcaTenantContext));
                XmlRootAttribute? requestRoot = inputElement is null ? null : requestType is not null
                    ? requestType.GetCustomAttribute<XmlRootAttribute>()
                    : typeof(IWsfev1Service).Assembly.GetTypes().Where(t => t.GetCustomAttribute<XmlRootAttribute>() is not null)
                        .Select(t => t.GetCustomAttribute<XmlRootAttribute>()!)
                        .SingleOrDefault(x => x.ElementName == inputElement && x.Namespace == targetNamespace);
                XmlRootAttribute? responseRoot = responseType.GetCustomAttribute<XmlRootAttribute>();
                if (inputElement is not null)
                {
                    requestRoot.Should().NotBeNull(operation);
                    requestRoot!.ElementName.Should().Be(inputElement, operation);
                    requestRoot.Namespace.Should().Be(targetNamespace, operation);
                }
                else
                {
                    method.GetParameters().Should().NotContain(p => p.ParameterType != typeof(CancellationToken) && p.ParameterType != typeof(ArcaEnvironment) && p.ParameterType != typeof(ArcaTenantContext), "the pinned dummy WSDL body is empty");
                }
                responseRoot.Should().NotBeNull(operation);
                responseRoot.ElementName.Should().Be(outputElement, operation);
                responseRoot.Namespace.Should().Be(targetNamespace, operation);
                actions.Should().ContainKey(operation);
            }
        }
    }

    [Fact]
    public async Task Every_generated_method_sends_the_soap_action_declared_by_its_wsdl()
    {
        using var certificate = TestCertificates.CreateWithPrivateKey("wsdl-action-contract");
        var content = WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), certificate.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem());
        var tenant = new ArcaTenantContext("wsdl-test", 30123456789, ArcaEnvironment.Production, content);
        var transport = new RecordingSoapTransport();
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create());
        object[] services =
        [
            new Wsfev1Service(transport, tickets), new Wsfexv1Service(transport, tickets),
            new Wsmtxcav1Service(transport, tickets), new PadronA4Service(transport, tickets),
            new WscdcService(transport, tickets), new WsfecredService(transport, tickets),
            new WscpeService(transport, tickets),
            new PadronA5Service(transport, tickets), new PadronA10Service(transport, tickets), new PadronA13Service(transport, tickets)
        ];
        foreach ((Type contract, string file) in Services)
        {
            XDocument document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "contracts", file));
            XNamespace wsdl = "http://schemas.xmlsoap.org/wsdl/";
            XNamespace soap = "http://schemas.xmlsoap.org/wsdl/soap/";
            XElement binding = document.Descendants(wsdl + "binding").Single(x => x.Element(soap + "binding") is not null);
            Dictionary<string, string> expectedActions = binding.Elements(wsdl + "operation")
                .ToDictionary(x => (string)x.Attribute("name")!, x => (string?)x.Element(soap + "operation")?.Attribute("soapAction") ?? "", StringComparer.Ordinal);
            object service = services.Single(s => contract.IsAssignableFrom(s.GetType()));
            foreach (MethodInfo operation in contract.GetMethods())
            {
                object?[] arguments = operation.GetParameters().Select(parameter =>
                    parameter.ParameterType == typeof(ArcaTenantContext) ? tenant :
                    parameter.ParameterType == typeof(ArcaEnvironment) ? ArcaEnvironment.Production :
                    parameter.ParameterType == typeof(CancellationToken) ? TestContext.Current.CancellationToken :
                    Activator.CreateInstance(parameter.ParameterType)).ToArray();
                await (Task)operation.Invoke(service, arguments)!;
                string operationName = operation.Name[..^"Async".Length];
                transport.LastCall.Action.Should().Be(expectedActions[operationName], $"{contract.Name}.{operation.Name}");
                if (operationName == "dummy" &&
                    (contract == typeof(IWsmtxcav1Service) || contract == typeof(IWsfecredService) || contract == typeof(IWscpeService)))
                    transport.LastCall.Request.Should().BeNull("the pinned dummy operation has an empty SOAP body");
            }
        }
    }

    [Fact]
    public void Every_generated_xml_root_type_can_be_serialized_and_read_back()
    {
        Assembly assembly = typeof(IWsfev1Service).Assembly;
        Type[] types = assembly.GetTypes().Where(t => t.Namespace?.StartsWith("NetArcaWs.Contracts.", StringComparison.Ordinal) == true && t.GetCustomAttribute<XmlRootAttribute>() is not null).ToArray();
        types.Should().HaveCount(369);
        foreach (Type type in types)
        {
            var serializer = new XmlSerializer(type);
            object value = Activator.CreateInstance(type)!;
            using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            serializer.Serialize(writer, value);
            using var reader = new StringReader(writer.ToString());
            serializer.Deserialize(reader).Should().BeOfType(type, type.FullName);
        }
    }

    [Fact]
    public void Every_generated_date_and_datetime_contract_type_round_trips_values()
    {
        Type[] types = typeof(IWsfev1Service).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("NetArcaWs.Contracts.", StringComparison.Ordinal) == true && t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.PropertyType == typeof(DateTime) && p.CanWrite && p.GetCustomAttribute<XmlIgnoreAttribute>() is null)).ToArray();
        types.Where(t => t.Namespace != "NetArcaWs.Contracts.Wscpe").Should().HaveCount(30);
        types.Where(t => t.Namespace == "NetArcaWs.Contracts.Wscpe").Should().HaveCount(26,
            "the pinned WSCPE contract has 26 date-bearing generated types");
        DateTime value = new(2026, 10, 6, 15, 45, 12, DateTimeKind.Unspecified);
        foreach (Type type in types)
        {
            object instance = Activator.CreateInstance(type)!;
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(DateTime) && p.CanWrite && p.GetCustomAttribute<XmlIgnoreAttribute>() is null).ToArray();
            foreach (PropertyInfo property in properties)
            {
                DateTime expected = property.GetCustomAttribute<XmlElementAttribute>()?.DataType == "date" ? value.Date : value;
                property.SetValue(instance, expected);
                PropertyInfo? specified = type.GetProperty(property.Name + "Specified");
                if (specified?.CanWrite == true) specified.SetValue(instance, true);
            }
            var serializer = new XmlSerializer(type);
            using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            serializer.Serialize(writer, instance);
            using var reader = new StringReader(writer.ToString());
            object copy = serializer.Deserialize(reader)!;
            foreach (PropertyInfo property in properties)
            {
                DateTime expected = property.GetCustomAttribute<XmlElementAttribute>()?.DataType == "date" ? value.Date : value;
                property.GetValue(copy).Should().Be(expected, $"{type.Name}.{property.Name}");
            }
        }

        Type invoiceType = typeof(IWsfev1Service).Assembly.GetType("NetArcaWs.Contracts.Wsmtxca.ComprobanteType", throwOnError: true)!;
        object invoice = Activator.CreateInstance(invoiceType)!;
        invoiceType.GetProperty("FechaEmision")!.SetValue(invoice, new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Unspecified));
        var invoiceSerializer = new XmlSerializer(invoiceType);
        using var invoiceWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        invoiceSerializer.Serialize(invoiceWriter, invoice);
        XElement fechaEmision = XDocument.Parse(invoiceWriter.ToString()).Descendants().Single(element => element.Name.LocalName == "fechaEmision");
        System.Text.RegularExpressions.Regex.IsMatch(fechaEmision.Value, @"^\d{4}-\d{2}-\d{2}(?:Z|[+-]\d{2}:\d{2})?$")
            .Should().BeTrue("the pinned MTXCA WSDL declares fechaEmision as xsd:date, which must not serialize a time component");
    }

    [Fact]
    public void Wsfecred_decimal_contract_values_serialize_with_invariant_xml_format_under_comma_decimal_culture()
    {
        var request = new NetArcaWs.Contracts.WsfeCred.InformarCancelacionTotalFeCredRequestType
        {
            ImporteCancelacion = 1234.56m
        };
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-AR");
            var serializer = new XmlSerializer(request.GetType());
            using var writer = new StringWriter(CultureInfo.CurrentCulture);
            serializer.Serialize(writer, request);

            XDocument.Parse(writer.ToString()).Descendants()
                .Single(element => element.Name.LocalName == "importeCancelacion")
                .Value.Should().Be("1234.56");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static string MessageElement(XElement message, XNamespace wsdl)
    {
        XAttribute element = message.Elements(wsdl + "part").Single().Attribute("element")!;
        return LocalName((string)element);
    }

    private static string? MessageElementOrNull(XElement message, XNamespace wsdl)
        => message.Elements(wsdl + "part").SingleOrDefault() is { } part ? LocalName((string)part.Attribute("element")!) : null;

    private static XName ResolveQName(XElement context, string qname)
    {
        int colon = qname.IndexOf(':');
        string namespaceUri = colon < 0 ? context.GetDefaultNamespace().NamespaceName : context.GetNamespaceOfPrefix(qname[..colon])!.NamespaceName;
        return XName.Get(colon < 0 ? qname : qname[(colon + 1)..], namespaceUri);
    }

    private static string LocalName(string qname) => qname[(qname.IndexOf(':') + 1)..];
}
