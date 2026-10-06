using NetArcaWs.Build;

try
{
    var root = Directory.GetCurrentDirectory();
    while (!File.Exists(Path.Combine(root, "NetArcaWs.slnx")))
        root = Directory.GetParent(root)?.FullName
            ?? throw new InvalidOperationException("Run from the NetArcaWs repository.");

    switch (args)
    {
        case ["operations"]:
            OperationDocumentation.Run(root);
            break;
        case ["wiki"]:
            WikiMirror.Run(root);
            break;
        case ["persistence-schema"]:
            PersistenceSchema.Run(root, check: false);
            break;
        case ["persistence-schema", "--check"]:
            PersistenceSchema.Run(root, check: true);
            break;
        case ["contracts", "--xscgen", var executable]:
            ContractGenerator.Run(root, Path.GetFullPath(executable));
            break;
        case ["release-version"]:
            var release = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME") == "release";
            var flag = Environment.GetEnvironmentVariable("RELEASE_PRERELEASE");
            if (release && flag is not ("true" or "false"))
                throw new InvalidOperationException("RELEASE_PRERELEASE must be true or false.");
            var version = ReleaseVersion.Validate(root,
                release ? Environment.GetEnvironmentVariable("RELEASE_TAG") ?? "" : null,
                release ? flag == "true" : null);
            Console.WriteLine(version);
            if (Environment.GetEnvironmentVariable("GITHUB_OUTPUT") is { Length: > 0 } output)
                await File.AppendAllTextAsync(output, $"version={version}\n");
            break;
        case ["release-assets", var repository, var tag, var directory]:
            await ReleaseAssets.RunAsync(repository, tag, Path.GetFullPath(directory));
            break;
        default:
            Console.Error.WriteLine("Commands: operations | wiki | persistence-schema [--check] | contracts --xscgen PATH | release-version | release-assets OWNER/REPO TAG DIRECTORY");
            return 2;
    }
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
