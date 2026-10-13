using System.Diagnostics;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class HomologationScriptTests
{
    private const string CertificateFixture = "synthetic certificate";
    private const string PrivateKeyFixture = "synthetic private key";

    [Fact]
    public void Manual_workflow_exposes_only_the_three_mode_choices()
    {
        string workflow = File.ReadAllText(Path.Combine(FindRepositoryRoot(), ".github", "workflows", "homologacion.yml"));
        string dispatch = workflow.Split("permissions:", StringSplitOptions.None)[0];
        dispatch.Should().Contain("inputs:");
        dispatch.Should().Contain("mode:");
        dispatch.Should().Contain("- consultas");
        dispatch.Should().Contain("- emision");
        dispatch.Should().Contain("- completa");
        dispatch.Should().NotContain("services:");
        dispatch.Should().NotContain("point_of_sale:");
        dispatch.Should().NotContain("voucher_number");
        dispatch.Should().NotContain("receiver");
        dispatch.Should().NotContain("buyer");
    }

    [Theory]
    [InlineData("GITHUB_REF", "refs/heads/release")]
    [InlineData("GITHUB_REPOSITORY", "someone-else/NetArcaWs")]
    [InlineData("GITHUB_EVENT_NAME", "push")]
    public void Run_rejects_unprotected_context_before_dotnet(string name, string value)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment[name] = value;

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("Homologación requiere una ejecución manual desde main del repositorio oficial.");
        File.Exists(fixture.Capture("calls")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("HOMO_CERTIFICATE_PEM")]
    [InlineData("HOMO_PRIVATE_KEY_PEM")]
    [InlineData("HOMOLOGATION_STATE_TOKEN")]
    [InlineData("ARCA_CUIT")]
    [InlineData("RUNNER_TEMP")]
    [InlineData("GITHUB_RUN_ID")]
    public void Run_rejects_missing_protected_configuration_before_dotnet(string name)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment.Remove(name);

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain($"Falta configuración obligatoria: {name}.");
        File.Exists(fixture.Capture("calls")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_rejects_malformed_issuer_CUIT_before_dotnet()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_CUIT"] = "invalid-cuit";

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("ARCA_CUIT debe contener 11 dígitos.");
        File.Exists(fixture.Capture("calls")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_stops_after_failed_matrix_preflight_before_materializing_credentials()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = "emision";
        fixture.Environment["PREFLIGHT_EXIT_CODE"] = "27";

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(27);
        File.ReadAllText(fixture.Capture("calls")).Should().Be("1");
        File.ReadAllText(fixture.Capture("call-1")).Should().Contain("preflight=1");
        File.ReadAllText(fixture.Capture("call-1")).Should().Contain("cert=false");
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("consultas", 1, "wsfe,wsfex,wsmtxca,wscdc,wsfecred,padron-a4,padron-a5,padron-a10,padron-a13")]
    [InlineData("emision", 2, "")]
    [InlineData("completa", 2, "wsfe,wsfex,wsmtxca,wscdc,wsfecred,padron-a4,padron-a5,padron-a10,padron-a13")]
    public void Run_forwards_only_mode_and_preflights_writes_before_credentials(string mode, int expectedCalls, string expectedServices)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = mode;

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(0);
        File.ReadAllText(fixture.Capture("calls")).Should().Be(expectedCalls.ToString());
        string normal = File.ReadAllText(fixture.Capture($"call-{expectedCalls}"));
        normal.Should().Contain("mode=" + mode);
        normal.Should().Contain("services=" + expectedServices);
        normal.Should().Contain("runid=stable-run-id");
        normal.Should().Contain("cert=true");
        normal.Should().Contain("pem-present=false");
        normal.Should().Contain("state-token=true");
        normal.Should().Contain("query-cuit=");
        if (mode is "emision" or "completa")
        {
            string preflight = File.ReadAllText(fixture.Capture("call-1"));
            preflight.Should().Contain("preflight=1");
            preflight.Should().Contain("cert=false");
            preflight.Should().Contain("cert-path=");
            preflight.Should().Contain("issuer=");
            preflight.Should().Contain("state-token=false");
            preflight.Should().Contain("pem-present=false");
            preflight.Should().Contain("state=false");
            preflight.Should().Contain("github-token=false");
        }
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData(" ")]
    public void Run_rejects_unknown_mode_before_dotnet(string mode)
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = mode;

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("ARCA_HOMOLOGY_MODE");
        File.Exists(fixture.Capture("calls")).Should().BeFalse();
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    [Fact]
    public void Run_preserves_dotnet_exit_code_and_removes_private_credentials()
    {
        EnsureBashAvailable();
        using var workspace = new BuildTestWorkspace();
        var fixture = CreateFixture(workspace);
        fixture.Environment["ARCA_HOMOLOGY_MODE"] = "emision";
        fixture.Environment["DOTNET_EXIT_CODE"] = "23";

        ProcessResult result = RunScript(workspace, fixture.Environment);

        result.ExitCode.Should().Be(23);
        AssertCredentialDirectoryWasRemoved(fixture.RunnerTemp);
    }

    private static ScriptFixture CreateFixture(BuildTestWorkspace workspace)
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("These script integration tests require bash and Unix file permissions.");
        string runnerTemp = Path.Combine(workspace.Root, "runner-temp");
        string dotnetDirectory = Path.Combine(workspace.Root, "fake-bin");
        string captureDirectory = Path.Combine(workspace.Root, "capture");
        Directory.CreateDirectory(runnerTemp);
        Directory.CreateDirectory(dotnetDirectory);
        Directory.CreateDirectory(captureDirectory);
        string stubPath = Path.Combine(dotnetDirectory, "dotnet");
        File.WriteAllText(stubPath, DotnetStub);
        File.SetUnixFileMode(stubPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        string inheritedPath = Environment.GetEnvironmentVariable("PATH") ?? "/usr/bin:/bin";
        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["GITHUB_REPOSITORY"] = "ARSASWebDesign/NetArcaWs", ["GITHUB_REF"] = "refs/heads/main",
            ["GITHUB_EVENT_NAME"] = "workflow_dispatch", ["GITHUB_RUN_ID"] = "stable-run-id",
            ["HOMO_CERTIFICATE_PEM"] = CertificateFixture, ["HOMO_PRIVATE_KEY_PEM"] = PrivateKeyFixture,
            ["HOMOLOGATION_STATE_TOKEN"] = "synthetic-fine-grained-token", ["HOMOLOGATION_STATE"] = "private-state",
            ["ARCA_CUIT"] = "30123456789", ["RUNNER_TEMP"] = runnerTemp, ["WSAA_SERVICE"] = "wsfe",
            ["WSAA_CERT_PATH"] = "inherited-cert-path", ["WSAA_KEY_PATH"] = "inherited-key-path",
            ["ARCA_HOMOLOGY_PREFLIGHT_ONLY"] = "unexpected-inherited-mode", ["GITHUB_TOKEN"] = "synthetic-gh-token",
            ["ARCA_QUERY_CUIT"] = "20123456789", ["PATH"] = dotnetDirectory + Path.PathSeparator + inheritedPath,
            ["CAPTURE_DIR"] = captureDirectory
        };
        return new ScriptFixture(environment, runnerTemp, captureDirectory);
    }

    private static ProcessResult RunScript(BuildTestWorkspace workspace, IReadOnlyDictionary<string, string> environment)
    {
        string script = Path.Combine(FindRepositoryRoot(), "scripts", "run-homologacion.sh");
        var start = new ProcessStartInfo("/bin/bash") { WorkingDirectory = workspace.Root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(script);
        start.Environment.Clear();
        foreach ((string name, string value) in environment) start.Environment[name] = value;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start bash.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "run-homologacion.sh"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate homologation script.");
    }

    private static void AssertCredentialDirectoryWasRemoved(string runnerTemp)
        => Directory.GetDirectories(runnerTemp, "netarcaws-homologacion.*").Should().BeEmpty();
    private static void EnsureBashAvailable() { if (OperatingSystem.IsWindows()) Assert.Skip("These script integration tests require bash."); }
    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
    private sealed record ScriptFixture(Dictionary<string, string> Environment, string RunnerTemp, string CaptureDirectory)
    { internal string Capture(string name) => Path.Combine(CaptureDirectory, name); }

    private const string DotnetStub = """
        #!/usr/bin/env bash
        set -euo pipefail
        count=0
        [[ ! -f "$CAPTURE_DIR/calls" ]] || count=$(cat "$CAPTURE_DIR/calls")
        count=$((count + 1))
        printf '%s' "$count" > "$CAPTURE_DIR/calls"
        path="$CAPTURE_DIR/call-$count"
        printf 'preflight=%s\nmode=%s\nservices=%s\nrunid=%s\ncert=%s\ncert-path=%s\nissuer=%s\npem-present=%s\nstate-token=%s\nstate=%s\ngithub-token=%s\nquery-cuit=%s\n' \
          "${ARCA_HOMOLOGY_PREFLIGHT_ONLY:-}" "${ARCA_HOMOLOGY_MODE:-}" "${ARCA_HOMOLOGY_SERVICES:-}" "${GITHUB_RUN_ID:-}" \
          "$(if [[ -n "${WSAA_CERT_PATH:-}" && -f "$WSAA_CERT_PATH" ]]; then echo true; else echo false; fi)" "${WSAA_CERT_PATH:-}" "${ARCA_CUIT:-}" \
          "$(if [[ -n "${HOMO_CERTIFICATE_PEM:-}" || -n "${HOMO_PRIVATE_KEY_PEM:-}" ]]; then echo true; else echo false; fi)" \
          "$(if [[ -n "${HOMOLOGATION_STATE_TOKEN:-}" ]]; then echo true; else echo false; fi)" \
          "$(if [[ -n "${HOMOLOGATION_STATE:-}" ]]; then echo true; else echo false; fi)" \
          "$(if [[ -n "${GITHUB_TOKEN:-}" || -n "${GH_TOKEN:-}" ]]; then echo true; else echo false; fi)" "${ARCA_QUERY_CUIT:-}" > "$path"
        if [[ "${ARCA_HOMOLOGY_PREFLIGHT_ONLY:-}" == 1 && "${PREFLIGHT_EXIT_CODE:-0}" != 0 ]]; then exit "$PREFLIGHT_EXIT_CODE"; fi
        if [[ "${DOTNET_EXIT_CODE:-0}" != 0 && "${ARCA_HOMOLOGY_PREFLIGHT_ONLY:-}" != 1 ]]; then echo 'fixture dotnet failure' >&2; exit "$DOTNET_EXIT_CODE"; fi
        if [[ "${ARCA_HOMOLOGY_PREFLIGHT_ONLY:-}" != 1 && -n "${WSAA_CERT_PATH:-}" ]]; then [[ -f "$WSAA_CERT_PATH" && -f "$WSAA_KEY_PATH" ]]; fi
        """;
}
