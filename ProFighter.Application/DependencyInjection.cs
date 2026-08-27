using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ProFighter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<ProFighter.Application.Customers.Services.Query.ICustomerQueryService, ProFighter.Application.Customers.Services.Query.CustomerQueryService>();
        services.AddScoped<ProFighter.Application.Common.Interfaces.IRekazCustomerSyncService, ProFighter.Application.Customers.Services.RekazCustomerSyncService>();
        services.AddScoped<ProFighter.Application.Common.Interfaces.IRekazSubscriptionEventHandler, ProFighter.Application.Subscriptions.Services.RekazSubscriptionEventHandler>();

        return services;
    }
}
