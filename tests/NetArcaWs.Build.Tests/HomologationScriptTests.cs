using System.Diagnostics;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class HomologationScriptTests
{
    private const string CertificateFixture = "-----BEGIN CERTIFICATE-----\nsynthetic certificate fixture\n-----END CERTIFICATE-----\n";
    private const string PrivateKeyFixture = "-----BEGIN PRIVATE KEY-----\nsynthetic private key fixture\n-----END PRIVATE KEY-----\n";

    [Theory]
    [InlineData("GITHUB_REF", "refs/heads/release")]
    [InlineData("GITHUB_REPOSITORY", "someone-else/NetArcaWs")]
    [InlineData("GITHUB_EVENT_NAME", "push")]
    public void Run_rejects_non_main_nonofficial_or_nonmanual_context(string name, string value)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment[name] = value;

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("Homologación requiere una ejecución manual desde main del repositorio oficial.");
        File.Exists(fixture.Capture("args")).Should().BeFalse("the dotnet stub must not run for a rejected context");
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_rejects_emision_outside_the_protected_manual_context()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = "emision";
        fixture.Environment["ARCA_HOMOLOGY_POINT_OF_SALE"] = "1";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_TYPE"] = "11";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_NUMBER"] = "1";
        fixture.Environment["GITHUB_REF"] = "refs/heads/release";

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("Homologación requiere una ejecución manual desde main del repositorio oficial.");
        File.Exists(fixture.Capture("args")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("HOMO_CERTIFICATE_PEM")]
    [InlineData("HOMO_PRIVATE_KEY_PEM")]
    [InlineData("ARCA_CUIT")]
    [InlineData("RUNNER_TEMP")]
    public void Run_rejects_missing_required_configuration(string name)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment.Remove(name);

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain($"Falta configuración obligatoria: {name}.");
        File.Exists(fixture.Capture("args")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("wsfex,unknown")]
    [InlineData("")]
    public void Run_rejects_unknown_or_empty_consulta_service_selection(string services)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_SERVICES"] = services;

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("ARCA_HOMOLOGY_SERVICES");
        File.Exists(fixture.Capture("args")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("wsfe")]
    [InlineData("wsfe,wsfex")]
    [InlineData("wsfe,wsfex,wsmtxca,wscdc,wsfecred,padron-a4,padron-a5,padron-a10,padron-a13")]
    public void Run_forwards_supported_consulta_services(string services)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_SERVICES"] = services;

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(0);
        File.ReadAllText(fixture.Capture("service")).Should().Be(services);
        File.ReadAllText(fixture.Capture("query-cuit")).Should().BeEmpty();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("padron-a4")]
    [InlineData("padron-a5")]
    [InlineData("padron-a10")]
    [InlineData("padron-a13")]
    public void Run_executes_each_padron_service_without_query_cuit(string service)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_SERVICES"] = service;
        fixture.Environment.Remove("ARCA_QUERY_CUIT");

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(0);
        File.ReadAllText(fixture.Capture("service")).Should().Be(service);
        File.ReadAllText(fixture.Capture("query-cuit")).Should().BeEmpty();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_defaults_to_consultas_and_forwards_the_default_voucher_type()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment.Remove("ARCA_HOMOLOGY_MODE");
        fixture.Environment.Remove("ARCA_HOMOLOGY_POINT_OF_SALE");
        fixture.Environment.Remove("ARCA_HOMOLOGY_VOUCHER_TYPE");
        fixture.Environment.Remove("ARCA_HOMOLOGY_VOUCHER_NUMBER");

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(0);
        File.ReadAllText(fixture.Capture("mode")).Should().Be("consultas");
        File.ReadAllText(fixture.Capture("point-of-sale")).Should().BeEmpty();
        File.ReadAllText(fixture.Capture("voucher-type")).Should().Be("11");
        File.ReadAllText(fixture.Capture("voucher-number")).Should().BeEmpty();
        File.ReadAllText(fixture.Capture("query-cuit")).Should().BeEmpty();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_forwards_valid_emision_selection_to_the_filtered_test()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = "emision";
        fixture.Environment["ARCA_HOMOLOGY_POINT_OF_SALE"] = "12";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_TYPE"] = "6";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_NUMBER"] = "98765";

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(0);
        File.ReadAllText(fixture.Capture("mode")).Should().Be("emision");
        File.ReadAllText(fixture.Capture("point-of-sale")).Should().Be("12");
        File.ReadAllText(fixture.Capture("voucher-type")).Should().Be("6");
        File.ReadAllText(fixture.Capture("voucher-number")).Should().Be("98765");
        File.ReadAllText(fixture.Capture("args")).Should().Contain("*Explicitly_enabled_authenticated_scenarios_run_against_homologation*");
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("ARCA_HOMOLOGY_MODE", "unknown")]
    [InlineData("ARCA_HOMOLOGY_SERVICES", "wsfe,wsfex")]
    [InlineData("ARCA_HOMOLOGY_POINT_OF_SALE", "0")]
    [InlineData("ARCA_HOMOLOGY_POINT_OF_SALE", "100000")]
    [InlineData("ARCA_HOMOLOGY_POINT_OF_SALE", "abc")]
    [InlineData("ARCA_HOMOLOGY_VOUCHER_TYPE", "1")]
    [InlineData("ARCA_HOMOLOGY_VOUCHER_NUMBER", "0")]
    [InlineData("ARCA_HOMOLOGY_VOUCHER_NUMBER", "100000000")]
    [InlineData("ARCA_HOMOLOGY_VOUCHER_NUMBER", "abc")]
    public void Run_rejects_invalid_emision_parameters_before_credentials_are_materialized(string name, string value)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = "emision";
        fixture.Environment["ARCA_HOMOLOGY_POINT_OF_SALE"] = "1";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_TYPE"] = "11";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_NUMBER"] = "1";
        fixture.Environment["ARCA_HOMOLOGY_SERVICES"] = "wsfe";
        fixture.Environment[name] = value;

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        File.Exists(fixture.Capture("args")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("ARCA_HOMOLOGY_POINT_OF_SALE")]
    [InlineData("ARCA_HOMOLOGY_VOUCHER_NUMBER")]
    public void Run_rejects_missing_required_emision_parameters(string name)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = "emision";
        fixture.Environment["ARCA_HOMOLOGY_POINT_OF_SALE"] = "1";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_TYPE"] = "11";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_NUMBER"] = "1";
        fixture.Environment.Remove(name);

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        File.Exists(fixture.Capture("args")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_rejects_emision_when_any_service_other_than_wsfe_is_selected()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = "emision";
        fixture.Environment["ARCA_HOMOLOGY_POINT_OF_SALE"] = "1";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_TYPE"] = "11";
        fixture.Environment["ARCA_HOMOLOGY_VOUCHER_NUMBER"] = "1";
        fixture.Environment["ARCA_HOMOLOGY_SERVICES"] = "wsfe,wsfex";

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("La emisión solo admite ARCA_HOMOLOGY_SERVICES=wsfe.");
        File.Exists(fixture.Capture("args")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_passes_only_the_filtered_homologation_test_and_temporary_restricted_credentials_to_dotnet()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(0);
        string arguments = File.ReadAllText(fixture.Capture("args"));
        arguments.Should().Contain("--project");
        arguments.Should().Contain("tests/NetArcaWs.IntegrationTests/NetArcaWs.IntegrationTests.csproj");
        arguments.Should().Contain("--filter-method");
        arguments.Should().Contain("*Explicitly_enabled_authenticated_scenarios_run_against_homologation*");
        arguments.Should().Contain("--no-build");
        arguments.Should().Contain("--no-restore");
        File.ReadAllText(fixture.Capture("require-homology")).Should().Be("1");
        File.ReadAllText(fixture.Capture("cert-content")).Should().Be(CertificateFixture);
        File.ReadAllText(fixture.Capture("key-content")).Should().Be(PrivateKeyFixture);
        File.ReadAllText(fixture.Capture("homologation-secret-unset")).Should().Be("true");
        File.ReadAllText(fixture.Capture("certificate-mode")).Trim().Should().Be("600");
        File.ReadAllText(fixture.Capture("key-mode")).Trim().Should().Be("600");
        File.ReadAllText(fixture.Capture("directory-mode")).Trim().Should().Be("700");
        File.ReadAllText(fixture.Capture("service")).Should().Be("wsfe");
        File.ReadAllText(fixture.Capture("wsaa-service")).Should().Be("wsfe");
        File.ReadAllText(fixture.Capture("mode")).Should().Be("consultas");
        File.ReadAllText(fixture.Capture("voucher-type")).Should().Be("11");
        File.ReadAllText(fixture.Capture("query-cuit")).Should().BeEmpty();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_preserves_dotnet_failure_exit_code_and_still_removes_temporary_credentials()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["DOTNET_EXIT_CODE"] = "23";

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(23);
        File.ReadAllText(fixture.Capture("dotnet-saw-certificate")).Should().Be("true");
        result.StandardError.Should().Contain("fixture dotnet failure");
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    private static ScriptFixture CreateFixture(BuildTestWorkspace workspace)
    {
        if (OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("These script integration tests require bash and Unix file permissions.");

        string runnerTemp = Path.Combine(workspace.Root, "runner-temp");
        string dotnetDirectory = Path.Combine(workspace.Root, "fake-bin");
        string captureDirectory = Path.Combine(workspace.Root, "capture");
        Directory.CreateDirectory(runnerTemp);
        Directory.CreateDirectory(dotnetDirectory);
        Directory.CreateDirectory(captureDirectory);
        string stubPath = Path.Combine(dotnetDirectory, "dotnet");
        File.WriteAllText(stubPath, DotnetStub);
        File.SetUnixFileMode(stubPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        string inheritedPath = Environment.GetEnvironmentVariable("PATH") ?? "/usr/bin:/bin";
        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["GITHUB_REPOSITORY"] = "ARSASWebDesign/NetArcaWs",
            ["GITHUB_REF"] = "refs/heads/main",
            ["GITHUB_EVENT_NAME"] = "workflow_dispatch",
            ["HOMO_CERTIFICATE_PEM"] = CertificateFixture,
            ["HOMO_PRIVATE_KEY_PEM"] = PrivateKeyFixture,
            ["ARCA_CUIT"] = "30123456789",
            ["RUNNER_TEMP"] = runnerTemp,
            ["WSAA_SERVICE"] = "wsfe",
            ["ARCA_HOMOLOGY_SERVICES"] = "wsfe",
            ["ARCA_QUERY_CUIT"] = "20123456789",
            ["PATH"] = dotnetDirectory + Path.PathSeparator + inheritedPath,
            ["CAPTURE_DIR"] = captureDirectory
        };
        return new ScriptFixture(environment, runnerTemp, captureDirectory);
    }

    private static ProcessResult RunScript(BuildTestWorkspace workspace, IReadOnlyDictionary<string, string> environment)
    {
        string script = Path.Combine(FindRepositoryRoot(), "scripts", "run-homologacion.sh");
        var start = new ProcessStartInfo("/bin/bash")
        {
            WorkingDirectory = workspace.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(script);
        start.Environment.Clear();
        foreach ((string name, string value) in environment)
            start.Environment[name] = value;

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start bash.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "run-homologacion.sh")))
                return directory.FullName;
        }
        throw new DirectoryNotFoundException("Could not locate scripts/run-homologacion.sh from the test output directory.");
    }

    private static void AssertCredentialDirectoryWasRemoved(string runnerTemp)
        => Directory.GetDirectories(runnerTemp, "netarcaws-homologacion.*").Should().BeEmpty();

    private static void EnsureBashAvailable()
    {
        if (OperatingSystem.IsWindows())
            Assert.Skip("These script integration tests require bash and are skipped on Windows.");
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    private sealed record ScriptFixture(
        Dictionary<string, string> Environment,
        string RunnerTemp,
        string CaptureDirectory)
    {
        internal string Capture(string name) => Path.Combine(CaptureDirectory, name);
    }

    private const string DotnetStub = """
        #!/usr/bin/env bash
        set -euo pipefail
        printf '%s\n' "$@" > "$CAPTURE_DIR/args"
        printf '%s' "$ARCA_REQUIRE_HOMOLOGY" > "$CAPTURE_DIR/require-homology"
        printf '%s' "$ARCA_HOMOLOGY_SERVICES" > "$CAPTURE_DIR/service"
        printf '%s' "$WSAA_SERVICE" > "$CAPTURE_DIR/wsaa-service"
        printf '%s' "${ARCA_HOMOLOGY_MODE:-}" > "$CAPTURE_DIR/mode"
        printf '%s' "${ARCA_HOMOLOGY_POINT_OF_SALE:-}" > "$CAPTURE_DIR/point-of-sale"
        printf '%s' "${ARCA_HOMOLOGY_VOUCHER_TYPE:-}" > "$CAPTURE_DIR/voucher-type"
        printf '%s' "${ARCA_HOMOLOGY_VOUCHER_NUMBER:-}" > "$CAPTURE_DIR/voucher-number"
        printf '%s' "${ARCA_QUERY_CUIT:-}" > "$CAPTURE_DIR/query-cuit"
        cat "$WSAA_CERT_PATH" > "$CAPTURE_DIR/cert-content"
        cat "$WSAA_KEY_PATH" > "$CAPTURE_DIR/key-content"
        printf '%s' "$(if [[ -z "${HOMO_CERTIFICATE_PEM+x}" && -z "${HOMO_PRIVATE_KEY_PEM+x}" ]]; then printf true; else printf false; fi)" > "$CAPTURE_DIR/homologation-secret-unset"
        printf '%s' "$(stat -c '%a' "$WSAA_CERT_PATH" 2>/dev/null || stat -f '%Lp' "$WSAA_CERT_PATH")" > "$CAPTURE_DIR/certificate-mode"
        printf '%s' "$(stat -c '%a' "$WSAA_KEY_PATH" 2>/dev/null || stat -f '%Lp' "$WSAA_KEY_PATH")" > "$CAPTURE_DIR/key-mode"
        printf '%s' "$(stat -c '%a' "$(dirname "$WSAA_CERT_PATH")" 2>/dev/null || stat -f '%Lp' "$(dirname "$WSAA_CERT_PATH")")" > "$CAPTURE_DIR/directory-mode"
        printf '%s' true > "$CAPTURE_DIR/dotnet-saw-certificate"
        if [[ "${DOTNET_EXIT_CODE:-0}" != 0 ]]; then
          printf '%s\n' 'fixture dotnet failure' >&2
        fi
        exit "${DOTNET_EXIT_CODE:-0}"
        """;
}
