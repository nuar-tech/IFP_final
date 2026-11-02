// src/SprintBoard.Application/DependencyInjection.cs
using Microsoft.Extensions.DependencyInjection;

namespace SprintBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddScoped<WorkItemService>().AddScoped<BoardService>();
}