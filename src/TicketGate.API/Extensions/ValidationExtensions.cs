using Microsoft.AspNetCore.Mvc;

namespace TicketGate.API.Extensions;

/// <summary>
/// Global input validation yapılandırması.
/// Tüm modellerde otomatik validation çalışır.
/// FluentValidation pipeline ile entegre çalışır.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// MVC model state filtresini kapatarak validation hatalarinin Result pipeline'i ile donmesini saglar.
    /// Minimal API ve MediatR FluentValidation davranisi ayni hata formatini korur.
    /// </summary>
    public static IServiceCollection AddTicketGateValidation(
        this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        return services;
    }
}
