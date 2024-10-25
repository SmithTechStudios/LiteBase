using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;

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
        if (context.Request.Path.StartsWithSegments("/litebase/ui"))
        {
            var assembly = typeof(LiteBaseUIMiddleware).Assembly;
            Stream? resourceStream;
            if (context.Request.Path.Value.ToLower().Contains("litebase/ui/index.js"))
            {
                context.Response.ContentType = "text/javascript";
                resourceStream = assembly.GetManifestResourceStream("LiteBase.ui.dist.litebase.js");
            }
            else if (context.Request.Path.Value.ToLower().Contains("litebase/ui/index.css"))
            {
                context.Response.ContentType = "text/css";
                resourceStream = assembly.GetManifestResourceStream("LiteBase.ui.dist.litebase.css");
            }
            else
            {
                context.Response.ContentType = "text/html";
                resourceStream = assembly.GetManifestResourceStream("LiteBase.index.html");
            }

            if (resourceStream != null)
            {
                await resourceStream.CopyToAsync(context.Response.Body);
                return;
            }
        }

        await _next(context);
    }
}

public static class LiteBaseUIMiddlewareExtensions
{
    public static IApplicationBuilder UseLiteBaseUi(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LiteBaseUIMiddleware>();
    }

    public static IApplicationBuilder UseLiteBaseEndpoint(this IApplicationBuilder builder, string dbPath)
    {
        return builder.UseMiddleware<LiteBaseEndpointMiddleware>(dbPath);
    }
}

public class LiteBaseEndpointMiddleware(RequestDelegate next, string dbPath)
{
    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.Equals("/litebase/tables", StringComparison.OrdinalIgnoreCase))
        {
            await using var connection = new SqliteConnection($"Data Source={dbPath}");
            // Create a query that retrieves all authors"    
            var sql = $"""
                       SELECT
                           m.name as TableName,
                           p.name as ColumnName,
                           p.type as ColumnType
                       FROM sqlite_master AS m
                       JOIN pragma_table_info(m.name) AS p
                       WHERE m.type = 'table'
                       ORDER BY m.name, p.cid
                       """;
            var rows = await connection.QueryAsync<Row>(sql);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(rows);

            return;
        }

        if (context.Request.Path.Value.Contains("/litebase/table/", StringComparison.OrdinalIgnoreCase))
        {
            await using var connection = new SqliteConnection($"Data Source={dbPath}");

            var tableName = context.Request.Path.Value?.Split("/").Last();

            var sql = $"SELECT * FROM {tableName.ToLower()}";

            var rows = await connection.QueryAsync<object>(sql);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(rows);
            return;
        }

        await next(context);
    }
}

record Row(string TableName, string ColumnName, string? ColumnType);