using System.Text.RegularExpressions;
using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class OperationDocumentationTests
{
    private static readonly XNamespace Wsdl = "http://schemas.xmlsoap.org/wsdl/";
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/wsdl/soap/";

    private static readonly (string File, string Contract, string Client)[] Services =
    [
        ("wsfev1-production.wsdl", "WsfeV1", "Wsfev1Service"),
        ("wsfexv1-production.wsdl", "WsfexV1", "Wsfexv1Service"),
        ("wsmtxca-production.wsdl", "Wsmtxca", "Wsmtxcav1Service"),
        ("wscdc-production.wsdl", "Wscdc", "WscdcService"),
        ("wsfecred-production.wsdl", "WsfeCred", "WsfecredService"),
        ("wscpe-production.wsdl", "Wscpe", "WscpeService"),
        ("padron-a4-production.wsdl", "PadronA4", "PadronA4Service"),
        ("padron-a5-production.wsdl", "PadronA5", "PadronA5Service"),
        ("padron-a10-production.wsdl", "PadronA10", "PadronA10Service"),
        ("padron-a13-production.wsdl", "PadronA13", "PadronA13Service")
    ];

    private static readonly HashSet<string> WsfecredWrites = new(StringComparer.Ordinal)
    {
        "rechazarNotaDC", "informarCancelacionTotalFECred", "aceptarFECred", "rechazarFECred",
        "informarFacturaAgtDptoCltv", "modificarOpcionTransferencia"
    };

    [Fact]
    public void Run_documents_every_pinned_operation_and_emits_reproducible_typed_examples()
    {
        string repositoryRoot = FindRepositoryRoot();
        using var workspace = new BuildTestWorkspace();
        string contractRoot = Path.Combine(repositoryRoot, "docs", "reference", "contracts");
        foreach (var service in Services)
            workspace.Write($"docs/reference/contracts/{service.File}",
                File.ReadAllText(Path.Combine(contractRoot, service.File)));

        string serviceSource = File.ReadAllText(Path.Combine(repositoryRoot, "src", "NetArcaWs", "Services", "Generated", "ArcaServices.g.cs"));
        workspace.Write("src/NetArcaWs/Services/Generated/ArcaServices.g.cs", serviceSource);
        foreach (var service in Services)
        {
            string contractPath = Path.Combine(repositoryRoot, "src", "NetArcaWs", "Contracts", "Generated", service.Contract + ".g.cs");
            workspace.Write($"src/NetArcaWs/Contracts/Generated/{service.Contract}.g.cs", File.ReadAllText(contractPath));
        }

        OperationDocumentation.Run(workspace.Root);

        int total = 0;
        foreach (var service in Services)
        {
            XDocument wsdl = XDocument.Load(Path.Combine(workspace.Root, "docs", "reference", "contracts", service.File));
            string markdown = workspace.Read($"docs/reference/operations/{service.Contract.ToLowerInvariant()}.md");
            string generatedExamples = workspace.Read("examples/NetArcaWs.Examples/OperationExamples.g.cs");
            var signatures = ReadInterfaceSignatures(serviceSource, service.Client);
            var messages = wsdl.Root!.Elements(Wsdl + "message")
                .ToDictionary(element => (string)element.Attribute("name")!, StringComparer.Ordinal);
            var actions = wsdl.Root.Elements(Wsdl + "binding")
                .Where(binding => binding.Element(Soap + "binding") is not null)
                .SelectMany(binding => binding.Elements(Wsdl + "operation"))
                .ToDictionary(element => (string)element.Attribute("name")!,
                    element => (string?)element.Element(Soap + "operation")?.Attribute("soapAction") ?? "",
                    StringComparer.Ordinal);
            var operations = wsdl.Root.Element(Wsdl + "portType")!.Elements(Wsdl + "operation").ToArray();
            total += operations.Length;

            foreach (XElement operation in operations)
            {
                string name = (string)operation.Attribute("name")!;
                var input = operation.Element(Wsdl + "input")!;
                var output = operation.Element(Wsdl + "output")!;
                string requestMessage = Local((string)input.Attribute("message")!);
                string responseMessage = Local((string)output.Attribute("message")!);
                string requestRoot = (string?)messages[requestMessage].Element(Wsdl + "part")?.Attribute("element") is { } requestQName
                    ? Local(requestQName)
                    : "(vacío)";
                string responseRoot = Local((string)messages[responseMessage].Element(Wsdl + "part")!.Attribute("element")!);
                var signature = signatures[name];
                string row = Regex.Matches(markdown, $"^\\| `{Regex.Escape(name)}` \\|.*$", RegexOptions.Multiline)
                    .Select(match => match.Value).Single();

                markdown.Should().Contain($"### `{name}`");
                row.Should().Contain(signature.RequestType is null
                    ? "sin DTO"
                    : $"`{service.Contract}.{signature.RequestType}`");
                row.Should().Contain($"`{service.Contract}.{signature.ResponseType}`");
                row.Should().Contain($"`{Escape(actions[name])}`");

                string section = Regex.Match(markdown,
                    $"### `{Regex.Escape(name)}`\\s*(?<body>.*?)(?=\\n### `|\\n## |\\z)", RegexOptions.Singleline).Groups["body"].Value;
                section.Should().Contain($"raíz XML `{requestRoot}`");
                section.Should().Contain($"raíz XML `{responseRoot}`");
                if (service.Contract is "Wscdc" or "WsfeCred" or "Wscpe")
                {
                    string expectedClass = name is "dummy" or "ComprobanteDummy"
                        ? "consulta técnica"
                        : (service.Contract == "WsfeCred" && WsfecredWrites.Contains(name)) ||
                          (service.Contract == "Wscpe" && !name.StartsWith("consulta", StringComparison.Ordinal))
                            ? "escritura" : "consulta";
                    section.Should().Contain($"Clase: **{expectedClass}**", $"{service.Contract}.{name}");
                }
                if (service.Contract == "Wscdc" && name == "ComprobanteDummy")
                {
                    section.Should().Contain("raíz XML `ComprobanteDummy`");
                    section.Should().NotContain("sin elemento/payload", "the generated client sends the WSDL request root despite having no public DTO parameter");
                }
                if (service.Contract == "WsfeCred" && name == "consultarTiposRetenciones")
                {
                    section.Should().Contain("| `authRequest` |", "the WSDL request element uses a named complex type");
                    section.Should().Contain("| `consultarTiposRetencionesReturn` |", "the response root uses a named complex type");
                    markdown.Should().Contain("### `ConsultarTiposRetencionesReturnType`");
                    markdown.Should().Contain("| `arrayTiposRetenciones` |", "the response type declares the WSDL catalog array");
                }

                string generatedName = Regex.Replace(service.Contract + "_" + name, "[^A-Za-z0-9_]", "_");
                generatedExamples.Should().Contain($" {generatedName}Async(");
                generatedExamples.Should().Contain($"service.{name}Async(");
                generatedExamples.Should().Contain($"{service.Contract}.{signature.ResponseType}");
                if (signature.RequestType is not null)
                    generatedExamples.Should().Contain($"{service.Contract}.{signature.RequestType} validatedRequest");
            }
        }

        total.Should().Be(183);
        Directory.GetFiles(Path.Combine(workspace.Root, "docs", "reference", "operations"), "*.md")
            .Should().HaveCount(Services.Length);

        var firstOutput = CaptureGeneratedFiles(workspace.Root);
        OperationDocumentation.Run(workspace.Root);
        CaptureGeneratedFiles(workspace.Root).Should().BeEquivalentTo(firstOutput);
    }

    private static Dictionary<string, (string? RequestType, string ResponseType)> ReadInterfaceSignatures(string source, string client)
    {
        var body = Regex.Match(source,
            $"public interface I{Regex.Escape(client)}\\s*\\{{(?<body>.*?)\\n\\}}", RegexOptions.Singleline)
            .Groups["body"].Value;
        body.Should().NotBeEmpty();
        return Regex.Matches(body,
                @"Task<global::NetArcaWs\.Contracts\.[^.]+\.(?<response>\w+)>\s+(?<method>\w+)\((?<parameters>[^;]*?)\);",
                RegexOptions.Singleline)
            .Cast<Match>()
            .ToDictionary(match => match.Groups["method"].Value.Replace("Async", "", StringComparison.Ordinal),
                match =>
                {
                    var request = Regex.Match(match.Groups["parameters"].Value,
                        @"global::NetArcaWs\.Contracts\.[^.]+\.(?<request>\w+) request");
                    return (RequestType: request.Success ? request.Groups["request"].Value : null,
                        ResponseType: match.Groups["response"].Value);
                }, StringComparer.Ordinal);
    }

    private static Dictionary<string, string> CaptureGeneratedFiles(string root)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(Path.Combine(root, "docs", "reference", "operations"), "*.md"))
            files[Path.GetRelativePath(root, path)] = File.ReadAllText(path);
        foreach (string relativePath in new[]
        {
            "examples/NetArcaWs.Examples/OperationExamples.g.cs",
            "examples/NetArcaWs.Examples/NetArcaWs.Examples.csproj"
        })
            files[relativePath] = File.ReadAllText(Path.Combine(root, relativePath));
        return files;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NetArcaWs.slnx")))
            current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Could not locate NetArcaWs.slnx.");
    }

    private static string Local(string qname) => qname[(qname.LastIndexOf(':') + 1)..];
    private static string Escape(string text) => text.Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
