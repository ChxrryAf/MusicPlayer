using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using SongApi.Data;

var builder = WebApplication.CreateBuilder(args);

// MySQL EF Core DbContexts
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    options.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 37)));
});
builder.Services.AddDbContext<DiscogsContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    options.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 37)));
});

// HttpClient fuer Discogs-API
builder.Services.AddHttpClient("discogs", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var userAgent = config["Discogs:UserAgent"];
    if (string.IsNullOrWhiteSpace(userAgent))
    {
        userAgent = "MusicPlayer/1.0 (+https://example.com/contact)";
    }

    client.BaseAddress = new Uri("https://api.discogs.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

    var token = config["Discogs:Token"];
    if (string.IsNullOrWhiteSpace(token))
    {
        token = Environment.GetEnvironmentVariable("DISCOGS_TOKEN");
    }
    if (!string.IsNullOrWhiteSpace(token))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Discogs", $"token={token}");
    }
});

// CORS fǬr dein Vite-Frontend (Ports ggf. anpassen)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("frontend", p => p
        .WithOrigins("http://localhost:5173", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// klassische Controller aktivieren + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("frontend");

// FǬr lokale Tests ist HTTPS-Redirect oft st��rend �?" lass es weg oder kommentier es aus
// app.UseHttpsRedirection();

// Statische Dateien aus wwwroot (images, audio, �?�)
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// bindet deine Controller (z. B. SongApiController) ein
app.MapControllers();

app.Run();
