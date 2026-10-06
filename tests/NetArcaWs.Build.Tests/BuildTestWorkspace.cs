namespace NetArcaWs.Build.Tests;

internal sealed class BuildTestWorkspace : IDisposable
{
    internal BuildTestWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "NetArcaWs.Build.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    internal string Root { get; }

    internal string Write(string relativePath, string contents)
    {
        string path = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    internal string Read(string relativePath) => File.ReadAllText(
        Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    internal bool Exists(string relativePath) => File.Exists(
        Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    public void Dispose()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
    }
}
