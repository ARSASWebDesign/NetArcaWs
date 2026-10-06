using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using NetArcaWs.Tool;

namespace NetArcaWs.Tool.Tests;

internal static class ToolTestSupport
{
    internal const string PasswordEnvironmentVariable = "NETARCA_TEST_PRIVATE_KEY_PASSWORD";
    internal const string PasswordSecret = "test-only secret 7a8f";

    internal static CertificateTool CreateTool(out StringWriter output, out StringWriter error, string? password = null)
    {
        output = new StringWriter();
        error = new StringWriter();
        return new CertificateTool(output, error, name =>
            name == PasswordEnvironmentVariable ? password : null);
    }

    internal static (CertificateTool Tool, StringWriter Output, StringWriter Error) CreateTool(string? password = null)
    {
        CertificateTool tool = CreateTool(out StringWriter output, out StringWriter error, password);
        return (tool, output, error);
    }

    internal static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        string[] arguments, string? password = null, CancellationToken cancellationToken = default)
    {
        CertificateTool tool = CreateTool(out StringWriter output, out StringWriter error, password);
        int exitCode = await tool.RunAsync(arguments, cancellationToken);
        return (exitCode, output.ToString(), error.ToString());
    }

    internal static string[] CertificateGenerationArguments(
        string command, string outputDirectory, string cuit = "30-12345678-9", string name = "Sistemas Ejemplo") =>
    [
        command,
        "--cuit", cuit,
        "--organization", "Empresa de Prueba",
        "--name", name,
        "--output", outputDirectory,
        "--password-env", PasswordEnvironmentVariable,
        "--key-size", "2048"
    ];

    internal static X509Certificate2 CreateCertificate(
        RSA key,
        string subject = "CN=local-tool-test, O=NetArcaWs Tool Tests, C=AR",
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null)
    {
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            notBefore ?? DateTimeOffset.UtcNow.AddDays(-1),
            notAfter ?? DateTimeOffset.UtcNow.AddDays(30));
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    internal TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NetArcaWs.Tool.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    internal string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
