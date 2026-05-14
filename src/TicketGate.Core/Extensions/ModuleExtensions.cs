using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Behaviors;
using TicketGate.Core.Contracts;

namespace TicketGate.Core.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration config)
    {
        var modules = DiscoverModules();

        foreach (var module in modules)
        {
            services.AddSingleton<IModule>(module);
            module.RegisterServices(services, config);
        }

        services.AddMediatR(configuration =>
        {
            foreach (var moduleAssembly in modules
                .Select(module => module.GetType().Assembly)
                .Distinct())
            {
                configuration.RegisterServicesFromAssembly(moduleAssembly);
            }

            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }

    public static WebApplication MapModules(this WebApplication app)
    {
        foreach (var module in app.Services.GetServices<IModule>())
        {
            module.MapEndpoints(app);
        }

        return app;
    }

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
