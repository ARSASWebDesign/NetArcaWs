using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class WikiMirrorTests
{
    [Fact]
    public void Run_maps_pages_and_rewrites_relative_markdown_image_and_wiki_links()
    {
        using var workspace = new BuildTestWorkspace();
        workspace.Write("README.md", "# Project\n\n[guide](docs/wiki/Guide.md#overview) and [[Home]].\n");
        workspace.Write("docs/wiki/Home.md", "# Home\n\n## Navegación\n\n- [Guía](Guide.md)\n- [[Guide]]\n\n[project](../../README.md) [external](https://example.test/docs) ![diagram](../../assets/diagram.png)\n");
        workspace.Write("docs/wiki/Guide.md", "# Guide\n\nA local page.\n");
        workspace.Write("assets/diagram.png", "image fixture");

        WikiMirror.Run(workspace.Root);

        workspace.Read("docs/wiki-export/Proyecto.md").Should().Contain("[guide](Guide#overview)");
        workspace.Read("docs/wiki-export/Proyecto.md").Should().Contain("[Home](Home)");
        workspace.Read("docs/wiki-export/Home.md").Should().Contain("[project](Proyecto)");
        string home = workspace.Read("docs/wiki-export/Home.md");
        home.Should().Contain("## Navegación");
        home.Should().Contain("- [Guía](Guide)");
        home.Should().Contain("- [Guide](Guide)");
        home.IndexOf("- [Guía](Guide)", StringComparison.Ordinal)
            .Should().BeLessThan(home.IndexOf("- [Guide](Guide)", StringComparison.Ordinal));
        workspace.Exists("docs/wiki-export/_Sidebar.md").Should().BeFalse();
        workspace.Read("docs/wiki-export/Home.md").Should().Contain("https://example.test/docs");
        workspace.Read("docs/wiki-export/Home.md").Should().Contain(
            "![diagram](https://github.com/ARSASWebDesign/NetArcaWs/raw/main/assets/diagram.png)");
        workspace.Read("docs/wiki-export/Fuentes-documentales.md").Should().Contain("README.md` → `Proyecto");
    }

    [Fact]
    public void Run_uses_curated_home_navigation_and_ignores_legacy_sidebar_template()
    {
        using var workspace = new BuildTestWorkspace();
        workspace.Write("README.md", "# Project\n");
        workspace.Write("docs/wiki/Home.md", "# Inicio\n\n## Por tema\n\n- [Documentación](../../README.md)\n- [[Home]]\n");
        workspace.Write("docs/wiki-sidebar.txt", "# Legacy sidebar\n\n- [Home](Home)\n");

        WikiMirror.Run(workspace.Root);

        string home = workspace.Read("docs/wiki-export/Home.md");
        home.Should().Contain("## Por tema");
        home.Should().Contain("- [Documentación](Proyecto)");
        home.Should().Contain("- [Home](Home)");
        workspace.Exists("docs/wiki-export/_Sidebar.md").Should().BeFalse();
    }

    [Fact]
    public void Run_removes_legacy_adr_page_and_writes_its_curated_name()
    {
        using var workspace = new BuildTestWorkspace();
        workspace.Write("docs/adr/0001-safe-invoice-retries.md", "# Safe invoices\n");
        workspace.Write("docs/wiki-export/ADR-0001-safe-invoice-retries.md", "old page name");
        workspace.Write("docs/wiki-export/_Sidebar.md", "old generated sidebar");
        workspace.Write("docs/wiki-export/Fuentes-documentales.md", """
            # Fuentes anteriores

            - `docs/adr/0001-safe-invoice-retries.md` → `ADR-0001-safe-invoice-retries`
            """);

        WikiMirror.Run(workspace.Root);

        workspace.Exists("docs/wiki-export/ADR-0001-safe-invoice-retries.md").Should().BeFalse();
        workspace.Read("docs/wiki-export/Decisión-1-Emisión-y-reintentos-seguros.md")
            .Should().Contain("# Safe invoices");
        workspace.Exists("docs/wiki-export/_Sidebar.md").Should().BeFalse();
    }

    [Fact]
    public void Run_rejects_page_name_collisions_before_cleaning_existing_output()
    {
        using var workspace = new BuildTestWorkspace();
        workspace.Write("docs/wiki/alpha/Index.md", "# Alpha\n");
        workspace.Write("docs/wiki/beta/Index.md", "# Beta\n");
        workspace.Write("docs/wiki-export/Index.md", "keep this generated page until collision is resolved");

        Action build = () => WikiMirror.Run(workspace.Root);

        build.Should().Throw<Exception>();
        workspace.Read("docs/wiki-export/Index.md").Should().Be("keep this generated page until collision is resolved");
    }

    [Fact]
    public void Run_cleans_stale_pages_but_refuses_manifest_path_traversal()
    {
        using var workspace = new BuildTestWorkspace();
        workspace.Write("README.md", "# Project\n");
        workspace.Write("docs/wiki/Home.md", "# Home\n");
        workspace.Write("outside.md", "outside sentinel");
        workspace.Write("docs/wiki-export/Old.md", "stale generated content");
        workspace.Write("docs/wiki-export/nested/Managed.md", "nested output must not be deleted by the manifest");
        workspace.Write("docs/wiki-export/Fuentes-documentales.md", """
            # Fuentes del mirror local

            - `docs/wiki/Old.md` → `Old`
            - `docs/wiki/attack.md` → `../../outside`
            - `docs/wiki/nested.md` → `nested/Managed`
            """);

        WikiMirror.Run(workspace.Root);

        workspace.Exists("docs/wiki-export/Old.md").Should().BeFalse();
        workspace.Read("docs/wiki-export/nested/Managed.md").Should().Be("nested output must not be deleted by the manifest");
        workspace.Read("outside.md").Should().Be("outside sentinel");
        workspace.Read("docs/wiki-export/Proyecto.md").Should().Contain("Project");
    }

    [Fact]
    public void Run_excludes_git_artifacts_build_output_and_its_own_previous_mirror()
    {
        using var workspace = new BuildTestWorkspace();
        workspace.Write("docs/wiki/Home.md", "# Home\n");
        workspace.Write(".git/private.md", "must not be mirrored");
        workspace.Write("artifacts/private.md", "must not be mirrored");
        workspace.Write("bin/private.md", "must not be mirrored");
        workspace.Write("obj/private.md", "must not be mirrored");
        workspace.Write("docs/wiki-export/OldExport.md", "must not be mirrored as a source");

        WikiMirror.Run(workspace.Root);

        string sources = workspace.Read("docs/wiki-export/Fuentes-documentales.md");
        sources.Should().Contain("docs/wiki/Home.md");
        sources.Should().NotContain(".git/private.md");
        sources.Should().NotContain("artifacts/private.md");
        sources.Should().NotContain("bin/private.md");
        sources.Should().NotContain("obj/private.md");
        sources.Should().NotContain("OldExport.md");
    }

    [Fact]
    public void Run_does_not_mirror_markdown_reached_through_file_or_directory_symlinks()
    {
        using var workspace = new BuildTestWorkspace();
        string outsideRoot = CreateOutsideDirectory();
        try
        {
            string outsideFile = Path.Combine(outsideRoot, "Secret.md");
            string outsideDirectory = Path.Combine(outsideRoot, "linked-docs");
            Directory.CreateDirectory(outsideDirectory);
            File.WriteAllText(outsideFile, "outside source file");
            File.WriteAllText(Path.Combine(outsideDirectory, "Linked.md"), "outside linked directory");
            workspace.Write("docs/wiki/Home.md", "# Home\n");
            string fileLink = Path.Combine(workspace.Root, "docs", "wiki", "FileLink.md");
            string directoryLink = Path.Combine(workspace.Root, "docs", "wiki", "DirectoryLink");
            if (!TryCreateFileSymlink(fileLink, outsideFile) || !TryCreateDirectorySymlink(directoryLink, outsideDirectory))
                Assert.Skip("The current platform or account cannot create filesystem symlinks.");

            WikiMirror.Run(workspace.Root);

            string manifest = workspace.Read("docs/wiki-export/Fuentes-documentales.md");
            manifest.Should().Contain("docs/wiki/Home.md");
            manifest.Should().NotContain("FileLink.md");
            manifest.Should().NotContain("DirectoryLink");
            workspace.Exists("docs/wiki-export/FileLink.md").Should().BeFalse();
            workspace.Exists("docs/wiki-export/Linked.md").Should().BeFalse();
            File.ReadAllText(outsideFile).Should().Be("outside source file");
        }
        finally
        {
            if (Directory.Exists(outsideRoot)) Directory.Delete(outsideRoot, recursive: true);
        }
    }

    [Fact]
    public void Run_rejects_an_output_symlink_without_deleting_external_contents()
    {
        using var workspace = new BuildTestWorkspace();
        string outsideOutput = CreateOutsideDirectory();
        try
        {
            workspace.Write("README.md", "# Project\n");
            Directory.CreateDirectory(Path.Combine(workspace.Root, "docs"));
            string sentinel = Path.Combine(outsideOutput, "Sentinel.md");
            File.WriteAllText(sentinel, "keep external output");
            string outputLink = Path.Combine(workspace.Root, "docs", "wiki-export");
            if (!TryCreateDirectorySymlink(outputLink, outsideOutput))
                Assert.Skip("The current platform or account cannot create directory symlinks.");

            Action build = () => WikiMirror.Run(workspace.Root);

            build.Should().Throw<Exception>();
            File.ReadAllText(sentinel).Should().Be("keep external output");
        }
        finally
        {
            if (Directory.Exists(outsideOutput)) Directory.Delete(outsideOutput, recursive: true);
        }
    }

    [Fact]
    public void Run_rejects_a_docs_ancestor_symlink_without_deleting_external_contents()
    {
        using var workspace = new BuildTestWorkspace();
        string outsideDocs = CreateOutsideDirectory();
        try
        {
            workspace.Write("README.md", "# Project\n");
            string sentinel = Path.Combine(outsideDocs, "Sentinel.md");
            File.WriteAllText(sentinel, "keep external docs");
            string docsLink = Path.Combine(workspace.Root, "docs");
            if (!TryCreateDirectorySymlink(docsLink, outsideDocs))
                Assert.Skip("The current platform or account cannot create directory symlinks.");

            Action build = () => WikiMirror.Run(workspace.Root);

            build.Should().Throw<Exception>();
            File.ReadAllText(sentinel).Should().Be("keep external docs");
        }
        finally
        {
            if (Directory.Exists(outsideDocs)) Directory.Delete(outsideDocs, recursive: true);
        }
    }

    private static string CreateOutsideDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "NetArcaWs.WikiSymlink.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static bool TryCreateFileSymlink(string linkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static bool TryCreateDirectorySymlink(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}
