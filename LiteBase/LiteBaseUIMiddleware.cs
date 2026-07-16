using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace LiteBase;

public class LiteBaseUIMiddleware
{
    private readonly RequestDelegate _next;

    public LiteBaseUIMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/litebase/ui"))
        {
            await _next(context);
            return;
        }

        var assembly = typeof(LiteBaseUIMiddleware).Assembly;
        var path = context.Request.Path.Value ?? "";

        var (contentType, resourceName) = path.ToLower() switch
        {
            var p when p.Contains("litebase.js")  => ("text/javascript", "LiteBase.ui.dist.litebase.js"),
            var p when p.Contains("litebase.css") => ("text/css",        "LiteBase.ui.dist.litebase.css"),
            _                                     => ("text/html",       "LiteBase.ui.dist.index.html")
        };

        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        context.Response.ContentType = contentType;
        await stream.CopyToAsync(context.Response.Body);
    }
}
