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
        if (context.Request.Path.StartsWithSegments("/LiteBase/ui"))
        {
            var assembly = typeof(LiteBaseUIMiddleware).Assembly;
            Stream? resourceStream;
            if (context.Request.Path.Value.Contains("LiteBase/ui/index.js"))
            {
                context.Response.ContentType = "text/javascript";
                resourceStream = assembly.GetManifestResourceStream("LiteBase.ui.dist.index.js");
            }
            else if (context.Request.Path.Value.Contains("LiteBase/ui/index.css"))
            {
                context.Response.ContentType = "text/css";
                resourceStream = assembly.GetManifestResourceStream("LiteBase.ui.dist.index.css");
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
        if (context.Request.Path.Equals("/LiteBase/tables", StringComparison.OrdinalIgnoreCase))
        {
            await using var db = new SqliteConnection($"Data Source={dbPath}");
            db.Open();

            var cmd = db.CreateCommand();
            cmd.CommandText = $"""
                               SELECT
                                   m.name as TableName,
                                   p.name as ColumnName,
                                   p.type as ColumnType
                               FROM
                                   sqlite_master AS m
                                       JOIN
                                   pragma_table_info(m.name) AS p
                               WHERE
                                   m.type = 'table'
                               ORDER BY
                                   m.name,
                                   p.cid
                               """;

            List<Row> items = new List<Row>();
            var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                items.Add(new Row(reader[0].ToString(), reader[1].ToString(), reader[2].ToString()));


                /*    for (int i = 0; i < reader.FieldCount; i++)
                       item.Add(reader[i]); */
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(items);
            return;
        }

        if (context.Request.Path.Value.Contains("/LiteBase/table/", StringComparison.OrdinalIgnoreCase))
        {
            await using var db = new SqliteConnection($"Data Source={dbPath}");
            db.Open();

            var tableName = context.Request.Path.Value?.Split("/").Last();

            var cmd = db.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {tableName.ToLower()}";

            List<List<object>> tableItems = new List<List<object>>();
            var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                var item = new List<Object>();
                tableItems.Add(item);

                for (int i = 0; i < reader.FieldCount; i++)
                    item.Add(reader[i]);
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(tableItems);
            return;
        }

        await next(context);
    }
}

record Row(string TableName, string ColumnName, string? ColumnType);