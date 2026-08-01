using QuestPDF.Infrastructure;
using SAFWebApp.Server.Hubs;
using SAFWebApp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License =
    LicenseType.Community;

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<InformePdfService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<MqttService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddSignalR();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// En desarrollo Angular se comunica con la API por el proxy HTTP local.
// En producción se conserva la redirección obligatoria a HTTPS.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.MapHub<ValvulasHub>("/hubs/valvulas");

app.MapFallbackToFile("/index.html");

app.Run();
