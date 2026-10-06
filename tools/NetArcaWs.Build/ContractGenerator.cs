using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace NetArcaWs.Build;

public static class ContractGenerator
{
    private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";
    private static readonly XNamespace Xsd = XsdNamespace;
    private static readonly XNamespace Wsdl = "http://schemas.xmlsoap.org/wsdl/";
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/wsdl/soap/";

    private static readonly (string Wsdl, string Contract, string Client, string ServiceId, string Endpoint)[] Services =
    [
        ("wsfev1-production.wsdl", "WsfeV1", "Wsfev1Service", "wsfe", "Wsfe"),
        ("wsfexv1-production.wsdl", "WsfexV1", "Wsfexv1Service", "wsfex", "Wsfex"),
        ("wsmtxca-production.wsdl", "Wsmtxca", "Wsmtxcav1Service", "wsmtxca", "Wsmtxca"),
        ("padron-a4-production.wsdl", "PadronA4", "PadronA4Service", "ws_sr_padron_a4", "PadronA4"),
        ("padron-a5-production.wsdl", "PadronA5", "PadronA5Service", "ws_sr_constancia_inscripcion", "PadronA5"),
        ("padron-a10-production.wsdl", "PadronA10", "PadronA10Service", "ws_sr_padron_a10", "PadronA10"),
        ("padron-a13-production.wsdl", "PadronA13", "PadronA13Service", "ws_sr_padron_a13", "PadronA13"),
    ];

    public static void Run(string root, string xscgen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(xscgen);
        root = Path.GetFullPath(root);
        xscgen = Path.GetFullPath(xscgen);
        if (!File.Exists(xscgen)) throw new InvalidOperationException($"xscgen executable not found: {xscgen}");

        var sources = Path.Combine(root, "docs", "reference", "contracts");
        var output = Path.Combine(root, "src", "NetArcaWs", "Contracts", "Generated");
        var servicesOutput = Path.Combine(root, "src", "NetArcaWs", "Services", "Generated", "ArcaServices.g.cs");
        Directory.CreateDirectory(output);
        var temp = Path.Combine(Path.GetTempPath(), "netarca-contracts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var generatedFiles = new List<(string Name, string Path)>();
            foreach (var (_, service, _, _, _) in Services)
            {
                var wsdl = Path.Combine(sources, Services.First(x => x.Contract == service).Wsdl);
                if (!File.Exists(wsdl)) throw new InvalidOperationException($"Missing pinned WSDL: {wsdl}");
                var schemaFile = Path.Combine(temp, service + ".xsd");
                var targetNamespace = InlineSchema(wsdl, schemaFile);
                var serviceOutput = Path.Combine(temp, service);
                Directory.CreateDirectory(serviceOutput);
                var start = new ProcessStartInfo(xscgen) { UseShellExecute = false };
                start.ArgumentList.Add($"--namespace={targetNamespace}=NetArcaWs.Contracts.{service}");
                start.ArgumentList.Add("--nullable");
                start.ArgumentList.Add("--order");
                start.ArgumentList.Add("--netCore");
                start.ArgumentList.Add("--commandArgs-");
                start.ArgumentList.Add($"--output={serviceOutput}");
                start.ArgumentList.Add(schemaFile);
                using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start xscgen.");
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException($"xscgen failed for {service} with exit code {process.ExitCode}.");
                var files = Directory.GetFiles(serviceOutput, "*.cs").Order(StringComparer.Ordinal).ToArray();
                if (files.Length != 1) throw new InvalidOperationException($"{service}: expected one generated file, got {files.Length}");
                generatedFiles.Add((service + ".g.cs", files[0]));
            }

            foreach (var (name, generated) in generatedFiles)
            {
                var content = string.Join("\n", File.ReadAllLines(generated, Encoding.UTF8).Select(line => line.TrimEnd())) + "\n";
                AtomicWrite(Path.Combine(output, name), "#nullable disable\n" + content);
            }

            var serviceContent = RenderServices(sources, output);
            AtomicWrite(servicesOutput, serviceContent);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static string InlineSchema(string path, string destination)
    {
        var sourceText = File.ReadAllText(path, new UTF8Encoding(false, true));
        XDocument document;
        try { document = XDocument.Parse(sourceText, LoadOptions.PreserveWhitespace); }
        catch (XmlException ex) { throw new InvalidOperationException($"{Path.GetFileName(path)}: invalid XML: {ex.Message}", ex); }
        var schema = document.Descendants(Xsd + "schema").FirstOrDefault()
            ?? throw new InvalidOperationException($"{Path.GetFileName(path)}: no inline XML Schema was found");
        if (schema.Element(Xsd + "import") is not null || schema.Element(Xsd + "include") is not null)
            throw new InvalidOperationException($"{Path.GetFileName(path)}: external schema imports/includes need to be pinned first");

        var definitions = Regex.Match(sourceText, "<(?:[A-Za-z_][\\w.-]*:)?definitions\\b[^>]*>", RegexOptions.CultureInvariant);
        if (!definitions.Success) throw new InvalidOperationException($"{Path.GetFileName(path)}: no WSDL definitions element was found");
        var declaration = definitions.Value;
        foreach (Match match in Regex.Matches(declaration, "xmlns:([A-Za-z_][\\w.-]*)=[\"']([^\"']+)[\"']", RegexOptions.CultureInvariant))
            schema.SetAttributeValue(XNamespace.Xmlns + match.Groups[1].Value, match.Groups[2].Value);
        var defaultNamespace = Regex.Match(declaration, "xmlns=[\"']([^\"']+)[\"']", RegexOptions.CultureInvariant);
        if (defaultNamespace.Success) schema.SetAttributeValue("xmlns", defaultNamespace.Groups[1].Value);

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false, OmitXmlDeclaration = false, NewLineHandling = NewLineHandling.None };
        using (var writer = XmlWriter.Create(destination, settings))
        {
            writer.WriteStartDocument();
            schema.WriteTo(writer);
            writer.WriteEndDocument();
        }
        var targetNamespace = (string?)schema.Attribute("targetNamespace");
        return !string.IsNullOrEmpty(targetNamespace) ? targetNamespace : throw new InvalidOperationException($"{Path.GetFileName(path)}: inline XML Schema has no targetNamespace");
    }

    private static List<(string Name, string? RequestType, string ResponseType, string Action)> WsdlOperationTypes(XElement root, string generatedSource)
    {
        var roots = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(generatedSource, "XmlRootAttribute\\(\"([^\"]+)\"[^\\]]*\\)\\]\\s+public partial class (\\w+)", RegexOptions.Singleline | RegexOptions.CultureInvariant))
            roots[match.Groups[1].Value] = match.Groups[2].Value;
        var messages = root.Elements(Wsdl + "message").Where(e => e.Attribute("name") is not null).ToDictionary(e => (string)e.Attribute("name")!, StringComparer.Ordinal);
        var portType = root.Element(Wsdl + "portType") ?? throw new InvalidOperationException("WSDL has no portType");
        var actions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var binding in root.Elements(Wsdl + "binding"))
        {
            if (binding.Element(Soap + "binding") is null) continue;
            foreach (var operation in binding.Elements(Wsdl + "operation"))
            {
                var soapOperation = operation.Element(Soap + "operation");
                if (soapOperation is not null) actions[(string?)operation.Attribute("name") ?? ""] = (string?)soapOperation.Attribute("soapAction") ?? "";
            }
        }
        var result = new List<(string, string?, string, string)>();
        foreach (var operation in portType.Elements(Wsdl + "operation"))
        {
            var name = (string?)operation.Attribute("name") ?? "";
            var input = operation.Element(Wsdl + "input"); var output = operation.Element(Wsdl + "output");
            if (input is null || output is null) throw new InvalidOperationException($"{name}: one-way operations are not supported");
            var requestMessage = LocalName((string?)input.Attribute("message"));
            var responseMessage = LocalName((string?)output.Attribute("message"));
            if (!messages.TryGetValue(requestMessage, out var request) || !messages.TryGetValue(responseMessage, out var response))
                throw new InvalidOperationException($"{name}: input/output WSDL messages are missing");
            var requestPart = request.Element(Wsdl + "part"); var responsePart = response.Element(Wsdl + "part");
            if (responsePart is null) throw new InvalidOperationException($"{name}: output WSDL message part is missing");
            var requestRoot = LocalName((string?)requestPart?.Attribute("element"));
            var responseRoot = LocalName((string?)responsePart.Attribute("element"));
            var requestType = requestPart is null ? null : roots.GetValueOrDefault(requestRoot);
            var responseType = roots.GetValueOrDefault(responseRoot);
            if ((requestPart is not null && requestType is null) || responseType is null)
                throw new InvalidOperationException($"{name}: generated root type is missing ({requestRoot}, {responseRoot})");
            result.Add((name, requestType, responseType, actions.GetValueOrDefault(name, "")));
        }
        return result;
    }

    private static string RenderServices(string sources, string output)
    {
        var lines = new List<string> { "// <auto-generated>", "// Generated from pinned ARCA production WSDLs by scripts/generate-contracts.sh.", "// Do not edit manually; regenerate after reviewing source WSDL changes.", "// </auto-generated>", "#nullable disable", "using NetArcaWs.HealthChecks;", "using NetArcaWs.Multitenancy;", "using NetArcaWs.Transport;", "", "namespace NetArcaWs.Services;", "" };
        foreach (var (wsdlName, contract, client, serviceId, endpoint) in Services)
        {
            var wsdl = XDocument.Load(Path.Combine(sources, wsdlName)).Root!;
            var source = File.ReadAllText(Path.Combine(output, contract + ".g.cs"));
            var methods = new List<string>(); var implementations = new List<string>();
            foreach (var (operationName, rawRequest, rawResponse, action) in WsdlOperationTypes(wsdl, source))
            {
                var requestType = rawRequest is null ? null : $"global::NetArcaWs.Contracts.{contract}.{rawRequest}";
                var responseType = $"global::NetArcaWs.Contracts.{contract}.{rawResponse}";
                string signature;
                if (operationName.ToLowerInvariant().EndsWith("dummy", StringComparison.Ordinal))
                {
                    requestType ??= "object";
                    var expression = requestType == "object" ? "null!" : $"new {requestType}()";
                    signature = $"Task<{responseType}> {operationName}Async(ArcaEnvironment environment = ArcaEnvironment.Homologation, CancellationToken cancellationToken = default);";
                    implementations.AddRange([$"    public {signature[..^1]}", $"        => SendUnauthenticatedAsync<{requestType}, {responseType}>(", $"            ArcaServiceEndpoints.Select(environment, ArcaServiceEndpoints.{endpoint}Homologation, ArcaServiceEndpoints.{endpoint}Production), \"{action}\", {expression}, cancellationToken);", ""]);
                }
                else
                {
                    signature = $"Task<{responseType}> {operationName}Async(ArcaTenantContext tenant, {requestType} request, CancellationToken cancellationToken = default);";
                    implementations.AddRange([$"    public {signature[..^1]}", $"        => SendAuthenticatedAsync<{requestType}, {responseType}>(TicketService,", $"            ArcaServiceEndpoints.Select(tenant.Environment, ArcaServiceEndpoints.{endpoint}Homologation, ArcaServiceEndpoints.{endpoint}Production),", $"            \"{action}\", tenant, request, cancellationToken);", ""]);
                }
                methods.Add("    " + signature);
            }
            lines.AddRange([$"public interface I{client}", "{", .. methods, "}", "", $"public sealed class {client} : ArcaSoapServiceBase, I{client}", "{", $"    public const string TicketService = \"{serviceId}\";", "", $"    public {client}(ISoapTransport transport, IArcaTicketProvider tickets) : base(transport, tickets) {{ }}", "", .. implementations, "}", ""]);
        }
        return string.Join("\n", lines);
    }

    private static string LocalName(string? qname) => qname is null ? "" : qname[(qname.LastIndexOf(':') + 1)..];

    private static void AtomicWrite(string destination, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + ".tmp";
        try
        {
            File.WriteAllText(temporary, content, new UTF8Encoding(false));
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
