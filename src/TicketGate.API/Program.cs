using TicketGate.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddModules(builder.Configuration);
var app = builder.Build();
app.MapModules();
app.Run();
