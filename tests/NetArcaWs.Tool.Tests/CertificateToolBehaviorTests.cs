using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Tool.Tests;

public sealed class CertificateToolBehaviorTests
{
    [Fact]
    public async Task Version_reports_tool_semver()
    {
        var (tool, output, error) = ToolTestSupport.CreateTool();

        var code = await tool.RunAsync(["--version"], TestContext.Current.CancellationToken);

        code.Should().Be(0);
        output.ToString().Should().Contain("0.5.0");
        error.ToString().Should().BeEmpty();
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("cert-dev", "--help")]
    [InlineData("cert-prod", "-h")]
    [InlineData("cert-info", "--help")]
    public async Task Help_returns_success_without_creating_files(params string[] args)
    {
        using var temp = new TemporaryDirectory();
        var (tool, output, error) = ToolTestSupport.CreateTool();

        var code = await tool.RunAsync(args, TestContext.Current.CancellationToken);

        code.Should().Be(0);
        output.ToString().Should().NotBeNullOrWhiteSpace();
        error.ToString().Should().BeEmpty();
        Directory.GetFileSystemEntries(temp.Path).Should().BeEmpty();
    }

    [Theory]
    [InlineData("cert-dev", "--cuit", "30123456789", "--organization", "Org", "--name", "Name", "--output", "unused", "--unencrypted", "--unknown", "x")]
    [InlineData("cert-dev", "--cuit", "30123456789", "--organization", "Org", "--name", "Name", "--output", "unused", "--unencrypted", "--unencrypted")]
    [InlineData("cert-prod", "--cuit", "30123456789", "--organization", "Org", "--name", "Name", "--output", "unused", "--unencrypted", "--password-env", ToolTestSupport.PasswordEnvironmentVariable)]
    [InlineData("cert-dev", "--help", "--cuit", "30123456789")]
    [InlineData("cert-info", "--certificate", "cert.crt", "--password-env", ToolTestSupport.PasswordEnvironmentVariable)]
    public async Task Invalid_or_conflicting_arguments_return_usage_error_without_writes(params string[] args)
    {
        using var temp = new TemporaryDirectory();
        var outputPath = System.IO.Path.Combine(temp.Path, "unused");
        var adjustedArgs = args.Select(arg => arg == "unused" ? outputPath : arg).ToArray();
        var (tool, _, _) = ToolTestSupport.CreateTool(password: ToolTestSupport.PasswordSecret);

        var code = await tool.RunAsync(adjustedArgs, TestContext.Current.CancellationToken);

        code.Should().Be(2);
        Directory.GetFileSystemEntries(temp.Path).Should().BeEmpty();
    }

    [Theory]
    [InlineData("abc12345678")]
    [InlineData("3012345678")]
    [InlineData("３０１２３４５６７８９")]
    [InlineData("30-12345678-9-0")]
    public async Task Invalid_cuit_returns_usage_error_and_does_not_create_output(string cuit)
    {
        using var temp = new TemporaryDirectory();
        var outputPath = System.IO.Path.Combine(temp.Path, "out");
        var args = ToolTestSupport.CertificateGenerationArguments("cert-dev", outputPath);
        args[args.ToList().IndexOf("30-12345678-9") + 1] = cuit;
        var (tool, _, _) = ToolTestSupport.CreateTool(password: ToolTestSupport.PasswordSecret);

        var code = await tool.RunAsync(args, TestContext.Current.CancellationToken);

        code.Should().Be(2);
        Directory.Exists(outputPath).Should().BeFalse();
    }

    [Fact]
    public async Task Missing_password_environment_value_returns_usage_error_without_output()
    {
        using var temp = new TemporaryDirectory();
        var outputPath = System.IO.Path.Combine(temp.Path, "out");
        var args = ToolTestSupport.CertificateGenerationArguments("cert-dev", outputPath);
        var (tool, _, _) = ToolTestSupport.CreateTool();

        var code = await tool.RunAsync(args, TestContext.Current.CancellationToken);

        code.Should().Be(2);
        Directory.Exists(outputPath).Should().BeFalse();
    }

    [Fact]
    public async Task Existing_output_directory_is_not_overwritten()
    {
        using var temp = new TemporaryDirectory();
        var outputPath = System.IO.Path.Combine(temp.Path, "existing");
        Directory.CreateDirectory(outputPath);
        var sentinel = System.IO.Path.Combine(outputPath, "keep.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged", TestContext.Current.CancellationToken);
        var args = ToolTestSupport.CertificateGenerationArguments("cert-dev", outputPath);
        var (tool, _, _) = ToolTestSupport.CreateTool(password: ToolTestSupport.PasswordSecret);

        var code = await tool.RunAsync(args, TestContext.Current.CancellationToken);

        code.Should().Be(1);
        (await File.ReadAllTextAsync(sentinel, TestContext.Current.CancellationToken)).Should().Be("unchanged");
        Directory.GetFiles(outputPath).Should().ContainSingle().Which.Should().Be(sentinel);
    }

    [Fact]
    public async Task Symlink_output_path_is_rejected_without_creating_target()
    {
        if (OperatingSystem.IsWindows()) return;
        using var temp = new TemporaryDirectory();
        var outputPath = System.IO.Path.Combine(temp.Path, "linked");
        var targetPath = System.IO.Path.Combine(temp.Path, "target");
        Directory.CreateSymbolicLink(outputPath, targetPath);
        var args = ToolTestSupport.CertificateGenerationArguments("cert-dev", outputPath);
        var (tool, _, _) = ToolTestSupport.CreateTool(password: ToolTestSupport.PasswordSecret);

        var code = await tool.RunAsync(args, TestContext.Current.CancellationToken);

        code.Should().Be(1);
        Directory.Exists(targetPath).Should().BeFalse();
        new DirectoryInfo(outputPath).LinkTarget.Should().Be(targetPath);
    }

    [Fact]
    public async Task Concurrent_generation_to_same_directory_has_one_winner_and_no_partial_loser_output()
    {
        using var temp = new TemporaryDirectory();
        var outputPath = System.IO.Path.Combine(temp.Path, "shared");
        var firstArgs = ToolTestSupport.CertificateGenerationArguments("cert-dev", outputPath);
        var secondArgs = ToolTestSupport.CertificateGenerationArguments("cert-prod", outputPath);
        var first = ToolTestSupport.CreateTool(password: ToolTestSupport.PasswordSecret).Tool;
        var second = ToolTestSupport.CreateTool(password: ToolTestSupport.PasswordSecret).Tool;

        var results = await Task.WhenAll(
            first.RunAsync(firstArgs, TestContext.Current.CancellationToken),
            second.RunAsync(secondArgs, TestContext.Current.CancellationToken));

        results.Count(code => code == 0).Should().Be(1);
        results.Count(code => code == 1).Should().Be(1);
        Directory.GetFiles(outputPath).Select(System.IO.Path.GetFileName).Should().BeEquivalentTo(
            "privada.key", "solicitud.csr", "instructions.txt", "manifest.json");
    }

    [Fact]
    public async Task Certificate_info_reports_local_validity_without_claiming_arca_trust()
    {
        using var temp = new TemporaryDirectory();
        using var rsa = RSA.Create(2048);
        using var cert = ToolTestSupport.CreateCertificate(rsa, "CN=Local test, O=Example", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var certificatePath = System.IO.Path.Combine(temp.Path, "local.crt");
        await File.WriteAllBytesAsync(certificatePath, cert.Export(X509ContentType.Cert), TestContext.Current.CancellationToken);
        var (tool, output, error) = ToolTestSupport.CreateTool();

        var code = await tool.RunAsync(["cert-info", "--certificate", certificatePath], TestContext.Current.CancellationToken);

        code.Should().Be(0);
        output.ToString().Should().Contain("Vigencia local: vigente");
        output.ToString().Should().Contain("No se verificó la confianza de ARCA ni la autorización a servicios.");
        error.ToString().Should().BeEmpty();
    }

    [Theory]
    [InlineData(-2, -1, "Vigencia local: vencido")]
    [InlineData(1, 2, "Vigencia local: aún no válido")]
    public async Task Certificate_info_reports_invalid_local_validity_and_returns_error(int startDays, int endDays, string expected)
    {
        using var temp = new TemporaryDirectory();
        using var rsa = RSA.Create(2048);
        using var cert = ToolTestSupport.CreateCertificate(rsa, "CN=Local test", DateTimeOffset.UtcNow.AddDays(startDays), DateTimeOffset.UtcNow.AddDays(endDays));
        var certificatePath = System.IO.Path.Combine(temp.Path, "local.crt");
        await File.WriteAllBytesAsync(certificatePath, cert.Export(X509ContentType.Cert), TestContext.Current.CancellationToken);
        var (tool, output, _) = ToolTestSupport.CreateTool();

        var code = await tool.RunAsync(["cert-info", "--certificate", certificatePath], TestContext.Current.CancellationToken);

        code.Should().Be(1);
        output.ToString().Should().Contain(expected);
        output.ToString().Should().Contain("No se verificó la confianza de ARCA ni la autorización a servicios.");
    }

    [Fact]
    public async Task Certificate_info_validates_private_key_match_and_password_environment()
    {
        using var temp = new TemporaryDirectory();
        using var certKey = RSA.Create(2048);
        using var wrongKey = RSA.Create(2048);
        using var cert = ToolTestSupport.CreateCertificate(certKey, "CN=Local test", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var certificatePath = System.IO.Path.Combine(temp.Path, "local.crt");
        var privateKeyPath = System.IO.Path.Combine(temp.Path, "private.pem");
        await File.WriteAllBytesAsync(certificatePath, cert.Export(X509ContentType.Cert), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(privateKeyPath, wrongKey.ExportPkcs8PrivateKeyPem(), TestContext.Current.CancellationToken);
        var (tool, _, error) = ToolTestSupport.CreateTool();

        var code = await tool.RunAsync(["cert-info", "--certificate", certificatePath, "--key", privateKeyPath], TestContext.Current.CancellationToken);

        code.Should().Be(1);
        error.ToString().Should().Contain("No se pudo procesar el material criptográfico.");
        error.ToString().Should().NotContain(ToolTestSupport.PasswordSecret);
    }

    [Fact]
    public async Task Certificate_info_accepts_a_matching_key_and_password_environment()
    {
        using var temp = new TemporaryDirectory();
        using var key = RSA.Create(2048);
        using var cert = ToolTestSupport.CreateCertificate(key, "CN=Local test", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var certificatePath = System.IO.Path.Combine(temp.Path, "local.crt");
        var privateKeyPath = System.IO.Path.Combine(temp.Path, "private.pem");
        await File.WriteAllBytesAsync(certificatePath, cert.Export(X509ContentType.Cert), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(privateKeyPath, key.ExportEncryptedPkcs8PrivateKeyPem(ToolTestSupport.PasswordSecret, new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000)), TestContext.Current.CancellationToken);
        var (tool, output, _) = ToolTestSupport.CreateTool(password: ToolTestSupport.PasswordSecret);

        var code = await tool.RunAsync(["cert-info", "--certificate", certificatePath, "--key", privateKeyPath, "--password-env", ToolTestSupport.PasswordEnvironmentVariable], TestContext.Current.CancellationToken);

        code.Should().Be(0);
        output.ToString().Should().Contain("Vigencia local: vigente");
    }

    [Fact]
    public async Task Certificate_info_rejects_non_rsa_certificate()
    {
        using var temp = new TemporaryDirectory();
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=EC local test", key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var certificatePath = System.IO.Path.Combine(temp.Path, "ec.crt");
        await File.WriteAllBytesAsync(certificatePath, certificate.Export(X509ContentType.Cert), TestContext.Current.CancellationToken);
        var (tool, _, error) = ToolTestSupport.CreateTool();

        var code = await tool.RunAsync(["cert-info", "--certificate", certificatePath], TestContext.Current.CancellationToken);

        code.Should().Be(1);
        error.ToString().Should().Contain("No se pudo procesar el material criptográfico.");
    }

    [Fact]
    public async Task Certificate_info_rejects_certificate_files_over_one_mib()
    {
        using var temp = new TemporaryDirectory();
        var certificatePath = System.IO.Path.Combine(temp.Path, "oversized.crt");
        await File.WriteAllBytesAsync(certificatePath, new byte[(1024 * 1024) + 1], TestContext.Current.CancellationToken);
        var (tool, _, error) = ToolTestSupport.CreateTool();

        var code = await tool.RunAsync(["cert-info", "--certificate", certificatePath], TestContext.Current.CancellationToken);

        code.Should().Be(1);
        error.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Precancelled_run_propagates_cancellation_without_creating_output()
    {
        using var temp = new TemporaryDirectory();
        var outputPath = System.IO.Path.Combine(temp.Path, "out");
        var args = ToolTestSupport.CertificateGenerationArguments("cert-dev", outputPath);
        var (tool, _, _) = ToolTestSupport.CreateTool(password: ToolTestSupport.PasswordSecret);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> act = async () => { await tool.RunAsync(args, cancellation.Token); };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
        Directory.Exists(outputPath).Should().BeFalse();
    }
}
