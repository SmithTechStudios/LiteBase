using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace LiteBase;

public static class LiteBaseExtensions
{
    public static IApplicationBuilder UseLiteBaseUi(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LiteBaseUIMiddleware>();
    }

    private static async Task<bool> IsAuthorizedAsync(HttpContext context)
    {
        var access = context.RequestServices.GetRequiredService<ILiteBaseAccess>();
        return await access.IsAuthorizedAsync();
    }

    public static IEndpointRouteBuilder MapLiteBase(this IEndpointRouteBuilder endpoints, LiteBaseOptions options)
    {
        endpoints.MapGet("/litebase/config", () => Results.Ok(options.Firebase));

        endpoints.MapGet("/litebase/tables", async (HttpContext context) =>
        {
            if (!await IsAuthorizedAsync(context)) return Results.Unauthorized();

            await using var connection = new SqliteConnection($"Data Source={options.DbPath}");
            var sql = """
                      SELECT
                          m.name as TableName,
                          p.name as ColumnName,
                          p.type as ColumnType,
                          p.pk > 0 as IsPrimaryKey
                      FROM sqlite_master AS m
                      JOIN pragma_table_info(m.name) AS p
                      WHERE m.type = 'table'
                      ORDER BY m.name, p.cid
                      """;
            var rows = await connection.QueryAsync<Row>(sql);
            return Results.Ok(rows);
        });

        endpoints.MapGet("/litebase/table/{tableName}", async (HttpContext context, string tableName) =>
        {
            if (!await IsAuthorizedAsync(context)) return Results.Unauthorized();

            await using var connection = new SqliteConnection($"Data Source={options.DbPath}");
            var rows = await connection.QueryAsync<object>($"SELECT rowid as rowid, * FROM {tableName.ToLower()}");
            return Results.Ok(rows);
        });

        endpoints.MapPut("/litebase/table/{tableName}/{rowid:long}", async (HttpContext context, string tableName, long rowid, Dictionary<string, JsonElement> body) =>
        {
            if (!await IsAuthorizedAsync(context)) return Results.Unauthorized();

            if (body == null || body.Count == 0)
                return Results.BadRequest("Request body is empty.");

            var table = tableName.ToLower();

            await using var connection = new SqliteConnection($"Data Source={options.DbPath}");

            await connection.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS _litebase_audit_log (
                    id         INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp  TEXT    NOT NULL,
                    table_name TEXT    NOT NULL,
                    table_rowId      INTEGER NOT NULL,
                    before     TEXT    NOT NULL,
                    after      TEXT    NOT NULL
                )
                """);

            var before = await connection.QuerySingleOrDefaultAsync<object>($"SELECT * FROM {table} WHERE rowid=@rowid", new { rowid });
            if (before is null)
                return Results.NotFound();

            var setClauses = body.Keys.Select(k => $"{k}=@{k}");
            var updateSql = $"UPDATE {table} SET {string.Join(", ", setClauses)} WHERE rowid=@rowid";

            var parameters = new DynamicParameters();
            foreach (var (key, element) in body)
            {
                object? value = element.ValueKind switch
                {
                    JsonValueKind.Number when element.TryGetInt64(out var l) => l,
                    JsonValueKind.Number => element.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => element.GetString()
                };
                parameters.Add(key, value);
            }
            parameters.Add("rowid", rowid);

            await connection.ExecuteAsync(updateSql, parameters);

            var beforeDict = (IDictionary<string, object?>)before;
            var changedKeys = body.Keys.Where(k =>
            {
                var oldVal = beforeDict.TryGetValue(k, out var v) ? v?.ToString() : null;
                var newVal = body[k].ValueKind == JsonValueKind.Null ? null : body[k].ToString();
                return oldVal != newVal;
            }).ToList();

            if (changedKeys.Count > 0)
            {
                var diffBefore = changedKeys.ToDictionary(k => k, k => beforeDict.TryGetValue(k, out var v) ? v : null);
                var diffAfter = changedKeys.ToDictionary(k => k, k => body[k].ValueKind == JsonValueKind.Null ? null : (object?)body[k].ToString());

                await connection.ExecuteAsync(
                    "INSERT INTO _litebase_audit_log (timestamp, table_name, table_rowId, before, after) VALUES (@timestamp, @tableName, @table_rowId, @before, @after)",
                    new
                    {
                        timestamp = DateTime.UtcNow.ToString("O"),
                        tableName = table,
                        table_rowId = rowid,
                        before = JsonSerializer.Serialize(diffBefore),
                        after = JsonSerializer.Serialize(diffAfter)
                    });
            }

            return Results.Ok();
        });

        return endpoints;
    }
}

record Row(string TableName, string ColumnName, string? ColumnType, long IsPrimaryKey);
