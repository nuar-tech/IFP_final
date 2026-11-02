// src/SprintBoard.Infrastructure/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintBoard.Application;

namespace SprintBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IUserLookup, UserLookup>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}