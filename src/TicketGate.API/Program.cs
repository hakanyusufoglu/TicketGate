using TicketGate.API.Seed;
using TicketGate.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddModules(builder.Configuration);
var app = builder.Build();
app.MapModules();

/// <summary>
/// Development ortamında test verilerini otomatik oluşturur.
/// Production'da çalışmaz.
/// </summary>
if (app.Environment.IsDevelopment())
{
    await SeedDataService.SeedAsync(app.Services);
}

app.Run();
