using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TicketGate.Core.Contracts;
using TicketGate.Identity.Configuration;
using TicketGate.Identity.Features.Auth.Endpoints;
using TicketGate.Identity.Infrastructure;
using TicketGate.Identity.Infrastructure.Persistence;

namespace TicketGate.Identity;

/// <summary>
/// Identity modulunun servis ve endpoint kayitlarini yapar.
/// JWT Bearer dogrulama, Swagger auth tanimi ve auth slicelarini moduler monolith sinirinda toplar.
/// </summary>
public sealed class IdentityModule : IModule
{
    /// <summary>
    /// Identity DbContext, JWT Bearer dogrulama, authorization ve validator servislerini kaydeder.
    /// Token validation issuer, audience, lifetime ve signing key kontrollerini zorunlu tutar.
    /// </summary>
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Identity");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", IdentitySchema.Name);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = config.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TicketGate API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document, null),
                    []
                }
            });
        });

        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly, includeInternalTypes: true);
    }

    /// <summary>
    /// Development ortaminda Swagger middleware'ini ve auth endpoint'lerini kaydeder.
    /// Authentication/authorization middleware sirasi API host tarafinda merkezi olarak yonetilir.
    /// </summary>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        if (app is WebApplication webApplication)
        {
            if (webApplication.Environment.IsDevelopment())
            {
                webApplication.UseSwagger();
                webApplication.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketGate API v1");
                    options.RoutePrefix = "swagger";
                });
            }
        }

        app.MapIdentityEndpoints();
    }
}
