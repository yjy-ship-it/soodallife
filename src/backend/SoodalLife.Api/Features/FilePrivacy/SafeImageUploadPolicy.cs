using System.Security.Cryptography;
using ImageMagick;

namespace SoodalLife.Api.Features.FilePrivacy;

public static class SafeImageUploadPolicy
{
    public const long MaximumBytes = 5 * 1024 * 1024;
    public const long MaximumPixels = 25_000_000;
    public const int MaximumDimension = 12_000;

    public static async Task<SafeImageUpload> ProcessAsync(IFormFile file, CancellationToken token)
    {
        if (file.Length is <= 0 or > MaximumBytes)
            throw new SafeImageUploadException("IMAGE_SIZE_INVALID", "JPG 또는 PNG 이미지는 파일당 5MB 이하만 업로드할 수 있습니다.");
        await using var source = file.OpenReadStream();
        using var memory = new MemoryStream((int)file.Length);
        await source.CopyToAsync(memory, token);
        return Process(file.FileName, file.ContentType, memory.ToArray());
    }

    public static SafeImageUpload Process(string fileName, string? contentType, byte[] bytes)
    {
        if (bytes.LongLength is <= 0 or > MaximumBytes)
            throw new SafeImageUploadException("IMAGE_SIZE_INVALID", "JPG 또는 PNG 이미지는 파일당 5MB 이하만 업로드할 수 있습니다.");

        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name) || name != fileName || name.Length > 255)
            throw new SafeImageUploadException("IMAGE_NAME_INVALID", "안전한 이미지 파일명을 사용해 주세요.");

        var extension = Path.GetExtension(name).ToLowerInvariant();
        var declared = contentType?.Split(';', 2)[0].Trim().ToLowerInvariant();
        var expected = extension switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", _ => null };
        if (expected is null || declared != expected)
            throw new SafeImageUploadException("IMAGE_TYPE_INVALID", "JPG, JPEG, PNG 이미지만 업로드할 수 있습니다.");

        try
        {
            var info = new MagickImageInfo(bytes);
            var actual = info.Format switch { MagickFormat.Jpeg => "image/jpeg", MagickFormat.Png => "image/png", _ => null };
            if (actual != expected)
                throw new SafeImageUploadException("IMAGE_SIGNATURE_INVALID", "확장자와 실제 이미지 형식이 일치하지 않습니다.");
            ValidateDimensions((long)info.Width, (long)info.Height);

            using var image = new MagickImage(bytes);
            if (image.Format is not (MagickFormat.Jpeg or MagickFormat.Png))
                throw new SafeImageUploadException("IMAGE_TYPE_INVALID", "JPG, JPEG, PNG 이미지만 업로드할 수 있습니다.");
            ValidateDimensions((long)image.Width, (long)image.Height);
            image.AutoOrient();
            image.Strip();
            image.Format = expected == "image/jpeg" ? MagickFormat.Jpeg : MagickFormat.Png;
            if (image.Format == MagickFormat.Jpeg) image.Quality = 88;
            using var output = new MemoryStream();
            image.Write(output);
            var sanitized = output.ToArray();
            if (sanitized.LongLength is <= 0 or > MaximumBytes)
                throw new SafeImageUploadException("IMAGE_OUTPUT_SIZE_INVALID", "보안 처리된 이미지가 5MB를 초과합니다. 크기를 줄여 다시 올려 주세요.");
            var normalizedExtension = expected == "image/jpeg" ? ".jpg" : ".png";
            var safeDisplayName = Path.GetFileNameWithoutExtension(name) + normalizedExtension;
            return new SafeImageUpload(sanitized, safeDisplayName, expected, normalizedExtension,
                Convert.ToHexString(SHA256.HashData(sanitized)).ToLowerInvariant(), (int)image.Width, (int)image.Height);
        }
        catch (SafeImageUploadException) { throw; }
        catch (MagickException)
        {
            throw new SafeImageUploadException("IMAGE_DECODE_INVALID", "손상되었거나 안전하게 처리할 수 없는 이미지입니다.");
        }
    }

    private static void ValidateDimensions(long width, long height)
    {
        if (width <= 0 || height <= 0 || width > MaximumDimension || height > MaximumDimension || width * height > MaximumPixels)
            throw new SafeImageUploadException("IMAGE_DIMENSIONS_INVALID", "이미지 해상도가 너무 큽니다. 가로·세로 12,000픽셀 및 전체 2,500만 픽셀 이하만 가능합니다.");
    }
}

public sealed record SafeImageUpload(byte[] Bytes, string FileName, string ContentType, string Extension, string Sha256Hex, int Width, int Height);
public sealed class SafeImageUploadException(string code, string message) : Exception(message) { public string Code { get; } = code; }
