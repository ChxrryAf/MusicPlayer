
var builder = WebApplication.CreateBuilder(args);

// CORS für dein Vite-Frontend (Ports ggf. anpassen)
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

// Für lokale Tests ist HTTPS-Redirect oft störend – lass es weg oder kommentier es aus
// app.UseHttpsRedirection();

// Statische Dateien aus wwwroot (images, audio, …)
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// bindet deine Controller (z. B. SongApiController) ein
app.MapControllers();

app.Run();
