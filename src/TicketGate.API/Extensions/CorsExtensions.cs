namespace TicketGate.API.Extensions;

/// <summary>
/// CORS politikası yapılandırması.
/// Development'ta tüm origin'lere izin verilir.
/// Production'da yalnızca izin verilen domain'ler geçer.
/// </summary>
public static class CorsExtensions
{
    private const string DevelopmentPolicy = "Development";
    private const string ProductionPolicy = "Production";
    private const string AllowedOriginsSection = "Cors:AllowedOrigins";

    /// <summary>
    /// Ortama göre Development veya Production CORS policy'sini kaydeder.
    /// Production origin listesi appsettings Cors:AllowedOrigins değerinden okunur.
    /// </summary>
    public static IServiceCollection AddTicketGateCors(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        services.AddCors(options =>
        {
            if (env.IsDevelopment())
            {
                options.AddPolicy(DevelopmentPolicy, policy =>
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            }
            else
            {
                var allowedOrigins = config
                    .GetSection(AllowedOriginsSection)
                    .Get<string[]>() ?? [];

                options.AddPolicy(ProductionPolicy, policy =>
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            }
        });

        return services;
    }

    /// <summary>
    /// Ortama uygun CORS policy'sini middleware pipeline'a ekler.
    /// Security headers ve auth katmanlariyla ayni host seviyesinde calisir.
    /// </summary>
    public static WebApplication UseTicketGateCors(this WebApplication app)
    {
        app.UseCors(app.Environment.IsDevelopment()
            ? DevelopmentPolicy
            : ProductionPolicy);

        return app;
    }
}
