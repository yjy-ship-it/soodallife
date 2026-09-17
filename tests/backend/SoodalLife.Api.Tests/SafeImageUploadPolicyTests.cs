using SoodalLife.Api.Features.FilePrivacy;

namespace SoodalLife.Api.Tests;

public sealed class SafeImageUploadPolicyTests
{
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public void Process_AcceptsAndReencodesPng()
    {
        var result = SafeImageUploadPolicy.Process("sample.png", "image/png", TinyPng);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(".png", result.Extension);
        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
        Assert.NotEmpty(result.Bytes);
    }

    [Fact]
    public void Process_RejectsPdfEvenWhenNamedAsPdf()
    {
        var error = Assert.Throws<SafeImageUploadException>(() =>
            SafeImageUploadPolicy.Process("document.pdf", "application/pdf", "%PDF-1.7"u8.ToArray()));
        Assert.Equal("IMAGE_TYPE_INVALID", error.Code);
    }

    [Fact]
    public void Process_RejectsMismatchedSignature()
    {
        var error = Assert.Throws<SafeImageUploadException>(() =>
            SafeImageUploadPolicy.Process("fake.jpg", "image/jpeg", TinyPng));
        Assert.Equal("IMAGE_SIGNATURE_INVALID", error.Code);
    }
}
