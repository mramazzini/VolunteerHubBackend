using Microsoft.Extensions.DependencyInjection;

namespace SixSeven.Application;

public static class DI
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}