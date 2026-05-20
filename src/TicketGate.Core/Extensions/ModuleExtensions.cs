using System.Reflection;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Behaviors;
using TicketGate.Core.Contracts;

namespace TicketGate.Core.Extensions;

/// <summary>
/// Modül discovery, servis kaydı ve endpoint map işlemlerini merkezi olarak sağlar.
/// Mediator pipeline davranışları tek noktadan kaydedilerek duplicate validation engellenir.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// TicketGate modüllerini bulur, servislerini kaydeder ve validation pipeline'ını scoped lifetime ile ekler.
    /// Scoped lifetime, FluentValidation validator'larının scoped bağımlılıklarla güvenli çalışmasını sağlar.
    /// </summary>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration config)
    {
        var modules = DiscoverModules();

        foreach (var module in modules)
        {
            services.AddSingleton<IModule>(module);
            module.RegisterServices(services, config);
        }

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    /// <summary>
    /// Keşfedilen modüllerin endpoint'lerini Minimal API route tablosuna ekler.
    /// Modüller kendi endpoint sınırlarını korur, host yalnızca map akışını başlatır.
    /// </summary>
    public static WebApplication MapModules(this WebApplication app)
    {
        foreach (var module in app.Services.GetServices<IModule>())
        {
            module.MapEndpoints(app);
        }

        return app;
    }

    /// <summary>
    /// Çalışma dizinindeki TicketGate assembly'lerini yükleyip IModule implementasyonlarını sıralı döndürür.
    /// Sıralama deterministik kayıt davranışı sağlar.
    /// </summary>
    private static IReadOnlyList<IModule> DiscoverModules()
    {
        LoadTicketGateAssemblies();

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(GetLoadableTypes)
            .Where(type =>
                typeof(IModule).IsAssignableFrom(type) &&
                type is { IsClass: true, IsAbstract: false })
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => (IModule)Activator.CreateInstance(type, nonPublic: true)!)
            .ToArray();
    }

    /// <summary>
    /// Henüz yüklenmemiş TicketGate assembly'lerini AppDomain'e ekler.
    /// Module discovery'nin sadece referans verilen değil, çıktı klasöründeki modülleri de görmesini sağlar.
    /// </summary>
    private static void LoadTicketGateAssemblies()
    {
        var loadedAssemblyNames = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => assembly.GetName().Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var assemblyPath in Directory.EnumerateFiles(AppContext.BaseDirectory, "TicketGate.*.dll"))
        {
            var assemblyName = AssemblyName.GetAssemblyName(assemblyPath).Name;

            if (string.IsNullOrWhiteSpace(assemblyName) || loadedAssemblyNames.Contains(assemblyName))
            {
                continue;
            }

            Assembly.LoadFrom(assemblyPath);
            loadedAssemblyNames.Add(assemblyName);
        }
    }

    /// <summary>
    /// Assembly type bilgilerini güvenli okur; yüklenemeyen tipler varsa kalan geçerli tiplerle devam eder.
    /// ReflectionTypeLoadException modül keşfini tamamen durdurmamalıdır.
    /// </summary>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }
}
