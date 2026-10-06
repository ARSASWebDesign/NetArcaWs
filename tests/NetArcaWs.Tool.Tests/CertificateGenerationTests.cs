using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Tool.Tests;

public sealed class CertificateGenerationTests
{
    private static readonly string[] ExpectedFiles =
    [
        "instructions.txt",
        "manifest.json",
        "privada.key",
        "solicitud.csr"
    ];

    [Theory]
    [InlineData("cert-dev", "Homologation")]
    [InlineData("cert-prod", "Production")]
    public async Task Certificate_command_creates_encrypted_private_key_valid_CSR_and_public_manifest(
        string command, string expectedEnvironment)
    {
        using var temp = new TemporaryDirectory();
        string outputDirectory = System.IO.Path.Combine(temp.Path, "output with spaces");

        var result = await ToolTestSupport.RunAsync(
            ToolTestSupport.CertificateGenerationArguments(command, outputDirectory),
            ToolTestSupport.PasswordSecret,
            TestContext.Current.CancellationToken);

        result.ExitCode.Should().Be(0, result.Error);
        Directory.GetFiles(outputDirectory).Select(System.IO.Path.GetFileName).Should().BeEquivalentTo(ExpectedFiles);
        result.Output.Should().NotContain(ToolTestSupport.PasswordSecret);
        result.Error.Should().NotContain(ToolTestSupport.PasswordSecret);

        string keyPem = await File.ReadAllTextAsync(System.IO.Path.Combine(outputDirectory, "privada.key"), TestContext.Current.CancellationToken);
        string csrPem = await File.ReadAllTextAsync(System.IO.Path.Combine(outputDirectory, "solicitud.csr"), TestContext.Current.CancellationToken);
        string instructions = await File.ReadAllTextAsync(System.IO.Path.Combine(outputDirectory, "instructions.txt"), TestContext.Current.CancellationToken);
        string manifestJson = await File.ReadAllTextAsync(System.IO.Path.Combine(outputDirectory, "manifest.json"), TestContext.Current.CancellationToken);
        keyPem.Should().Contain("-----BEGIN ENCRYPTED PRIVATE KEY-----");
        keyPem.Should().NotContain(ToolTestSupport.PasswordSecret);
        instructions.Should().NotContain(ToolTestSupport.PasswordSecret);
        manifestJson.Should().NotContain(ToolTestSupport.PasswordSecret);
        manifestJson.Should().NotContain("PRIVATE KEY");

        using RSA privateKey = RSA.Create();
        privateKey.ImportFromEncryptedPem(keyPem, ToolTestSupport.PasswordSecret);
        CertificateRequest csr = CertificateRequest.LoadSigningRequestPem(
            csrPem,
            HashAlgorithmName.SHA256,
            CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);
        using RSA csrPublicKey = csr.PublicKey.GetRSAPublicKey()!;
        RSAParameters privatePublic = privateKey.ExportParameters(includePrivateParameters: false);
        RSAParameters csrPublic = csrPublicKey.ExportParameters(includePrivateParameters: false);
        csrPublic.Modulus.Should().Equal(privatePublic.Modulus);
        csrPublic.Exponent.Should().Equal(privatePublic.Exponent);
        ReadSubjectAttributes(csr.SubjectName.RawData).Should().Contain(new KeyValuePair<string, (string, UniversalTagNumber)>(
            "2.5.4.5", ("CUIT 30123456789", UniversalTagNumber.PrintableString)));
        csr.SubjectName.Name.Should().Contain("CN=Sistemas Ejemplo");
        csr.SubjectName.Name.Should().Contain("O=Empresa de Prueba");

        using JsonDocument manifest = JsonDocument.Parse(manifestJson);
        JsonElement root = manifest.RootElement;
        root.GetProperty("environment").GetString().Should().Be(expectedEnvironment);
        root.GetProperty("cuit").GetString().Should().Be("30123456789");
        root.GetProperty("organization").GetString().Should().Be("Empresa de Prueba");
        root.GetProperty("commonName").GetString().Should().Be("Sistemas Ejemplo");
        root.GetProperty("keyFile").GetString().Should().Be("privada.key");
        root.GetProperty("csrFile").GetString().Should().Be("solicitud.csr");
        root.GetProperty("createdUtc").GetDateTimeOffset().Offset.Should().Be(TimeSpan.Zero);
        root.GetProperty("keySize").GetInt32().Should().Be(2048);
        root.GetProperty("encrypted").GetBoolean().Should().BeTrue();
        string publicKeyHash = Convert.ToHexString(SHA256.HashData(privateKey.ExportSubjectPublicKeyInfo()));
        string.Equals(root.GetProperty("publicKeySha256").GetString(), publicKeyHash, StringComparison.OrdinalIgnoreCase).Should().BeTrue();

        AssertPrivatePermissions(outputDirectory);
    }

    [Fact]
    public async Task Certificate_command_can_explicitly_write_an_unencrypted_private_key()
    {
        using var temp = new TemporaryDirectory();
        string outputDirectory = System.IO.Path.Combine(temp.Path, "unencrypted");
        string[] args = RemovePasswordEnvironmentFlag(
            ToolTestSupport.CertificateGenerationArguments("cert-dev", outputDirectory));
        args = [.. args, "--unencrypted"];

        var result = await ToolTestSupport.RunAsync(args, cancellationToken: TestContext.Current.CancellationToken);

        result.ExitCode.Should().Be(0, result.Error);
        string keyPem = await File.ReadAllTextAsync(System.IO.Path.Combine(outputDirectory, "privada.key"), TestContext.Current.CancellationToken);
        keyPem.Should().Contain("-----BEGIN PRIVATE KEY-----");
        keyPem.Should().NotContain("-----BEGIN ENCRYPTED PRIVATE KEY-----");
        using RSA key = RSA.Create();
        key.ImportFromPem(keyPem);
        using JsonDocument manifest = JsonDocument.Parse(await File.ReadAllTextAsync(System.IO.Path.Combine(outputDirectory, "manifest.json"), TestContext.Current.CancellationToken));
        manifest.RootElement.GetProperty("encrypted").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Certificate_command_applies_secure_unix_permissions_to_output_and_private_key()
    {
        if (OperatingSystem.IsWindows()) return;
        using var temp = new TemporaryDirectory();
        string outputDirectory = System.IO.Path.Combine(temp.Path, "restricted");

        var result = await ToolTestSupport.RunAsync(
            ToolTestSupport.CertificateGenerationArguments("cert-dev", outputDirectory),
            ToolTestSupport.PasswordSecret,
            TestContext.Current.CancellationToken);

        result.ExitCode.Should().Be(0, result.Error);
        File.GetUnixFileMode(outputDirectory).Should().Be(
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.GetUnixFileMode(System.IO.Path.Combine(outputDirectory, "privada.key")).Should().Be(
            UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private static string[] RemovePasswordEnvironmentFlag(string[] args)
    {
        var filtered = new List<string>();
        for (int index = 0; index < args.Length; index++)
        {
            if (args[index] == "--password-env")
            {
                index++;
                continue;
            }
            filtered.Add(args[index]);
        }
        return [.. filtered];
    }

    private static Dictionary<string, (string Value, UniversalTagNumber Tag)> ReadSubjectAttributes(byte[] encodedName)
    {
        var values = new Dictionary<string, (string Value, UniversalTagNumber Tag)>();
        var name = new AsnReader(encodedName, AsnEncodingRules.DER);
        AsnReader rdns = name.ReadSequence();
        while (rdns.HasData)
        {
            AsnReader set = rdns.ReadSetOf();
            while (set.HasData)
            {
                AsnReader attribute = set.ReadSequence();
                string oid = attribute.ReadObjectIdentifier();
                UniversalTagNumber tag = (UniversalTagNumber)attribute.PeekTag().TagValue;
                values[oid] = (attribute.ReadCharacterString(tag), tag);
            }
        }
        return values;
    }

    private static void AssertPrivatePermissions(string outputDirectory)
    {
        if (OperatingSystem.IsWindows()) return;
        File.GetUnixFileMode(outputDirectory).Should().Be(
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.GetUnixFileMode(System.IO.Path.Combine(outputDirectory, "privada.key")).Should().Be(
            UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
