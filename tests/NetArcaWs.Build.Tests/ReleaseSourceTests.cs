using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class ReleaseSourceTests
{
    [Fact]
    public void Generate_creates_exact_commit_archive_and_license_evidence()
    {
        using var repository = new GitRepositoryFixture();
        repository.Write("LICENSE", "LGPL source\n");
        repository.Write("licenses/dependencies.json", "{\"schemaVersion\":1}\n");
        repository.Write("tools/NetArcaWs.Build/Program.cs", "tracked build input\n");
        string commit = repository.Commit();
        string artifacts = Path.Combine(repository.Root, "artifacts");
        Directory.CreateDirectory(artifacts);
        repository.Write("local-secret.txt", "must not ship\n");
        repository.Write("ignored-cache/secret.txt", "must not ship\n");
        repository.Write(".gitignore", "ignored-cache/\nartifacts/\n");

        ReleaseSource.Generate(repository.Root, commit, artifacts);

        using var source = ZipFile.OpenRead(Path.Combine(artifacts, "SOURCE.zip"));
        source.Comment.Should().Be(commit);
        source.Entries.Select(entry => entry.FullName).Where(path => !path.EndsWith("/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).Should().Equal(repository.CommittedFiles(commit).Order(StringComparer.Ordinal));
        source.Entries.Select(entry => entry.FullName).Should().Contain("COPYING").And.Contain("THIRD-PARTY-NOTICES.md")
            .And.Contain("licenses/policy.json").And.Contain("licenses/third-party/example/LICENSE");
        source.Entries.Should().NotContain(entry => entry.FullName.Contains("secret", StringComparison.Ordinal));
        using JsonDocument evidence = JsonDocument.Parse(File.ReadAllText(Path.Combine(artifacts, "LICENSE_EVIDENCE.json")));
        evidence.RootElement.GetProperty("commit").GetString().Should().Be(commit);
        evidence.RootElement.GetProperty("inventorySha256").GetString().Should()
            .Be(Hash("{\"schemaVersion\":1}\n"));
        evidence.RootElement.GetProperty("sourceSha256").GetString().Should()
            .Be(Hash(File.ReadAllBytes(Path.Combine(artifacts, "SOURCE.zip"))));
    }

    [Fact]
    public void Generate_rejects_a_commit_other_than_HEAD()
    {
        using var repository = new GitRepositoryFixture();
        repository.Write("licenses/dependencies.json", "{}\n");
        string commit = repository.Commit();

        Action generate = () => ReleaseSource.Generate(repository.Root, new string('a', 40), Path.Combine(repository.Root, "artifacts"));

        generate.Should().Throw<InvalidOperationException>().WithMessage("*does not match HEAD*");
        commit.Should().HaveLength(40);
    }

    [Fact]
    public void Generate_rejects_tracked_source_changes_after_commit()
    {
        using var repository = new GitRepositoryFixture();
        repository.Write("licenses/dependencies.json", "{}\n");
        string commit = repository.Commit();
        repository.Write("licenses/dependencies.json", "changed\n");

        Action generate = () => ReleaseSource.Generate(repository.Root, commit, Path.Combine(repository.Root, "artifacts"));

        generate.Should().Throw<InvalidOperationException>().WithMessage("*tracked working tree is dirty*");
    }

    [Fact]
    public void Generate_rejects_tracked_files_silently_excluded_by_git_archive()
    {
        using var repository = new GitRepositoryFixture();
        repository.Write(".gitattributes", "secret.txt export-ignore\n");
        repository.Write("secret.txt", "tracked but excluded\n");
        repository.Write("licenses/dependencies.json", "{}\n");
        string commit = repository.Commit();

        Action generate = () => ReleaseSource.Generate(repository.Root, commit, Path.Combine(repository.Root, "artifacts"));

        generate.Should().Throw<InvalidOperationException>().WithMessage("*does not contain the complete tracked file tree*");
    }

    [Fact]
    public void Generate_rejects_tracked_symlinks()
    {
        using var repository = new GitRepositoryFixture();
        string outside = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        File.WriteAllText(outside, "outside source tree\n");
        File.CreateSymbolicLink(Path.Combine(repository.Root, "outside-link"), outside);
        string commit = repository.Commit();

        Action generate = () => ReleaseSource.Generate(repository.Root, commit, Path.Combine(repository.Root, "artifacts"));

        generate.Should().Throw<InvalidOperationException>().WithMessage("*symlink*");
        File.Delete(outside);
    }

    private static string Hash(string text) => Hash(System.Text.Encoding.UTF8.GetBytes(text));
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed class GitRepositoryFixture : IDisposable
    {
        internal GitRepositoryFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "NetArcaWs.ReleaseSource.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            Git("init", "--quiet", "--initial-branch=main");
            Git("config", "user.name", "Release Source Tests");
            Git("config", "user.email", "release-source-tests@example.invalid");
            Write("LICENSE", "LGPL\n");
            Write("COPYING", "LGPL terms\n");
            Write("THIRD-PARTY-NOTICES.md", "Third party notices\n");
            Write("licenses/policy.json", "{}\n");
            Write("NetArcaWs.slnx", "solution\n");
            Write("global.json", "{}\n");
            Write("licenses/dependencies.json", "{}\n");
            Write("licenses/third-party/example/LICENSE", "third party terms\n");
            Write("Directory.Build.props", "<Project />\n");
            foreach (string project in ReleaseVersion.PackageProjects)
                Write($"src/{project}/{project}.csproj", "<Project />\n");
            Write("tools/NetArcaWs.Build/NetArcaWs.Build.csproj", "<Project />\n");
        }

        internal string Root { get; }

        internal string Write(string path, string contents)
        {
            string fullPath = Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, contents);
            return fullPath;
        }

        internal string Commit()
        {
            Git("add", "--all");
            Git("commit", "--quiet", "-m", "fixture");
            return Git("rev-parse", "HEAD");
        }

        internal string[] CommittedFiles(string commit) => Git("ls-tree", "--full-tree", "-r", "--name-only", commit)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        private string Git(params string[] arguments)
        {
            var start = new ProcessStartInfo("git") { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            foreach (string argument in arguments) start.ArgumentList.Add(argument);
            using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start git for the test fixture.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException($"git fixture command failed: {error}");
            return output.Trim();
        }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }
}
