namespace SoodalLife.Api.Features.Work;

public interface IPrivateFileStorage
{
    Task SaveAsync(string storageKey, Stream source, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteIfExistsAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed class DevelopmentPrivateFileStorage(IHostEnvironment environment, IConfiguration configuration) : IPrivateFileStorage
{
    private readonly string _rootPath = ResolveRoot(environment, configuration);

    public async Task SaveAsync(string storageKey, Stream source, CancellationToken cancellationToken)
    {
        EnsureAllowed(environment);
        var path = ResolveSafePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await source.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        EnsureAllowed(environment);
        Stream stream = new FileStream(ResolveSafePath(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(stream);
    }

    public Task DeleteIfExistsAsync(string storageKey, CancellationToken cancellationToken)
    {
        EnsureAllowed(environment);
        var path = ResolveSafePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolveSafePath(string storageKey)
    {
        var normalizedRoot = Path.GetFullPath(_rootPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(normalizedRoot, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage key escaped the private storage root.");
        return path;
    }

    private static string ResolveRoot(IHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["FileStorage:DevelopmentRoot"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);
        return environment.IsEnvironment("Testing")
            ? Path.Combine(Path.GetTempPath(), "SoodalLife", "tests", Environment.ProcessId.ToString())
            : Path.Combine(environment.ContentRootPath, "App_Data", "private-files");
    }

    private static void EnsureAllowed(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("Development private file storage is not available outside Development or Testing.");
    }
}
