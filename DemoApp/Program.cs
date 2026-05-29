using LiteBase;
using SqlLiteUI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ILiteBaseAccess, AllowAllAccess>();

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
app.MapLiteBase(new LiteBaseOptions
{
    DbPath = "db/blogging.db",
    Firebase = new FirebaseConfig
    {
        ApiKey = "AIzaSyCqB7VHM1-t_3gOrpXKUIB2gMs4DqirrpA",
        AuthDomain = "bgquest.app",
        ProjectId = "boardgame-tracker-2498e",
        StorageBucket = "boardgame-tracker-2498e.firebasestorage.app",
        MessagingSenderId = "483970449590",
        AppId = "1:483970449590:web:07fe9000eaf7b4bbf32617"
    }
});

app.Run();

public class AllowAllAccess : ILiteBaseAccess
{
    public Task<bool> IsAuthorizedAsync() => Task.FromResult(true);
}
