using Microsoft.AspNetCore.Http.Features;
using SoodalLife.Api.Features.FilePrivacy;

namespace SoodalLife.Api.Infrastructure.Security;

public sealed class SafeImageUploadMiddleware(RequestDelegate next, ILogger<SafeImageUploadMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType || context.Request.Method is not ("POST" or "PUT" or "PATCH"))
        {
            await next(context);
            return;
        }

        try
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            if (form.Files.Count == 0) { await next(context); return; }
            var files = new FormFileCollection();
            foreach (var file in form.Files)
            {
                // The global interceptor normalizes raster images. Other file types (for example,
                // an A/S PDF document) must remain available to the endpoint-specific validator.
                if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(file);
                    continue;
                }

                var safe = await SafeImageUploadPolicy.ProcessAsync(file, context.RequestAborted);
                var stream = new MemoryStream(safe.Bytes, writable: false);
                context.Response.RegisterForDispose(stream);
                files.Add(new FormFile(stream, 0, safe.Bytes.Length, file.Name, safe.FileName)
                {
                    Headers = new HeaderDictionary(), ContentType = safe.ContentType,
                    ContentDisposition = $"form-data; name=\"{file.Name}\"; filename=\"{safe.FileName}\""
                });
            }
            context.Features.Set<IFormFeature>(new FormFeature(new FormCollection(
                form.ToDictionary(pair => pair.Key, pair => pair.Value), files)));
            await next(context);
        }
        catch (SafeImageUploadException error)
        {
            logger.LogWarning("Rejected upload {Code} at {Path}", error.Code, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { code = error.Code, message = error.Message }, context.RequestAborted);
        }
        catch (InvalidDataException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { code = "IMAGE_FORM_INVALID", message = "업로드 요청을 읽을 수 없습니다." }, context.RequestAborted);
        }
    }
}
