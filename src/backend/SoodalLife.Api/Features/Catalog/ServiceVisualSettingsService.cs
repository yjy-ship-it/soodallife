using System.Text.Json;

namespace SoodalLife.Api.Features.Catalog;

public sealed record ServiceVisualSettingsResponse(bool ImagesEnabled, bool IconsEnabled, bool BannersEnabled);
public sealed record UpdateServiceVisualSettingsRequest(bool ImagesEnabled, bool IconsEnabled, bool BannersEnabled);

public sealed class ServiceVisualSettingsService(IWebHostEnvironment environment)
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private static readonly ServiceVisualSettingsResponse Default = new(true, true, true);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string SettingsPath => Path.Combine(environment.ContentRootPath, "App_Data", "service-visual-settings.json");

    public async Task<ServiceVisualSettingsResponse> GetAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(SettingsPath)) return Default;
        try
        {
            await using var stream = File.OpenRead(SettingsPath);
            return await JsonSerializer.DeserializeAsync<ServiceVisualSettingsResponse>(stream, JsonOptions, cancellationToken) ?? Default;
        }
        catch (IOException) { return Default; }
        catch (UnauthorizedAccessException) { return Default; }
        catch (JsonException) { return Default; }
    }

    public async Task<ServiceVisualSettingsResponse> UpdateAsync(UpdateServiceVisualSettingsRequest request, CancellationToken cancellationToken)
    {
        var value = new ServiceVisualSettingsResponse(request.ImagesEnabled, request.IconsEnabled, request.BannersEnabled);
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = SettingsPath + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(value, JsonOptions), cancellationToken);
            File.Move(temporaryPath, SettingsPath, true);
        }
        finally { WriteLock.Release(); }
        return value;
    }
}
