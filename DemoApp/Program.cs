using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LiteBase;
using SqlLiteUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        using var db = new BloggingContext();

        // Note: This sample requires the database to be created before running.
        Console.WriteLine($"Database path: {db.DbPath}.");
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();


app.UseCors();

app.UseStaticFiles();

app.UseLiteBaseUi();
app.UseLiteBaseEndpoint("db/blogging.db");

app.MapGet("/databaseinfo", () =>
{
    using var db = new BloggingContext();

    var x = db.Database
        .SqlQuery<Row>($"""
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
                        """).ToList();

    return x;
});

app.MapGet("/tableinfo/{tableName}", (string tableName) =>
{

    using var db = new SqliteConnection("Data Source=db/blogging.db");
    db.Open(); ;

    var cmd = db.CreateCommand();
    cmd.CommandText = $"SELECT * FROM {tableName.ToLower()}";

    List<List<object>> items = new List<List<object>>();
    var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        var item = new List<Object>();
        items.Add(item);

        for (int i = 0; i < reader.FieldCount; i++)
            item.Add(reader[i]);
    }

    return items;
    // return Request.CreateResponse<List<object>>(HttpStatusCode.OK, items);

    /*var x = db.Database.SqlQueryRaw<dynamic>($"SELECT * FROM {tableName.ToLower()}").ToList();
    return x;*/
});


app.Run();

record Row(string TableName, string ColumnName, string? ColumnType);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
