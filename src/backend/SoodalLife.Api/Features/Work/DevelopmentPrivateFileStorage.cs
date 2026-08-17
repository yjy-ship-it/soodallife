namespace SoodalLife.Api.Features.Work;

public interface IPrivateFileStorage
{
    Task SaveAsync(string storageKey, Stream source, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteIfExistsAsync(string storageKey, CancellationToken cancellationToken);
    Task PrepareBoundedUploadDirectoryAsync(string parentStorageKey, string uploadStorageKey, CancellationToken cancellationToken);
    Task DeleteDirectoryIfExistsAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed class DevelopmentPrivateFileStorage(IHostEnvironment environment, IConfiguration configuration) : IPrivateFileStorage
{
    private readonly string _rootPath = ResolveRoot(environment, configuration);

    public async Task SaveAsync(string storageKey, Stream source, CancellationToken cancellationToken)
    {
        var path = ResolveSafePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await source.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        Stream stream = new FileStream(ResolveSafePath(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(stream);
    }

    public Task DeleteIfExistsAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = ResolveSafePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task PrepareBoundedUploadDirectoryAsync(string parentStorageKey, string uploadStorageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parentPath = ResolveSafePath(parentStorageKey);
        var uploadPath = ResolveSafePath(uploadStorageKey);
        Directory.CreateDirectory(parentPath);
        var cutoff = DateTime.UtcNow.AddHours(-1);
        foreach (var directory in Directory.EnumerateDirectories(parentPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Directory.GetLastWriteTimeUtc(directory) < cutoff)
                Directory.Delete(directory, recursive: true);
        }
        if (!Directory.Exists(uploadPath) && Directory.EnumerateDirectories(parentPath).Take(3).Count() >= 3)
            throw new IOException("Too many active image uploads for this provider.");
        Directory.CreateDirectory(uploadPath);
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryIfExistsAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveSafePath(storageKey);
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
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
        var configured = configuration["FileStorage:PrivateRoot"] ?? configuration["FileStorage:DevelopmentRoot"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);
        return environment.IsEnvironment("Testing")
            ? Path.Combine(Path.GetTempPath(), "SoodalLife", "tests", Environment.ProcessId.ToString())
            : Path.Combine(environment.ContentRootPath, "App_Data", "private-files");
    }

}
