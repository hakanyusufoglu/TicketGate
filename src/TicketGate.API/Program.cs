using System.Reflection;
using System.Text;
using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TicketGate.Core.Behaviors;
using TicketGate.Core.Contracts;

var builder = WebApplication.CreateBuilder(args);
var moduleAssemblies = LoadModuleAssemblies();

builder.Services.AddProblemDetails();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddMediatR(configuration =>
{
    foreach (var assembly in moduleAssemblies)
    {
        configuration.RegisterServicesFromAssembly(assembly);
    }

    configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

foreach (var assembly in moduleAssemblies)
{
    builder.Services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
}

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
    redisOptions.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(redisOptions);
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecretKey = jwtSection["SecretKey"] ?? string.Empty;
var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

foreach (var module in app.Services.GetServices<IModule>())
{
    module.MapEndpoints(app);
}

app.Run();

static Assembly[] LoadModuleAssemblies()
{
    var moduleNames = new[]
    {
        "TicketGate.Identity",
        "TicketGate.Event",
        "TicketGate.Booking",
        "TicketGate.Payment",
        "TicketGate.Notification"
    };

    return moduleNames
        .Select(Assembly.Load)
        .ToArray();
}
