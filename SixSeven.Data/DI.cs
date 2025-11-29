using Microsoft.Extensions.DependencyInjection;
using SixSeven.Application.Interfaces.ReadStore;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Data.ReadStores;
using SixSeven.Domain.Entities;

namespace SixSeven.Data;

public static class DI
{
    public static IServiceCollection AddData(this IServiceCollection services)
    {
        services.AddScoped<IUserReadStore, UserReadStore>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped< IVolunteerReportingRepository, VolunteerReportingRepository>();

        return services;
    }
}