using DevDesk.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DevDesk")
            ?? throw new InvalidOperationException(
                "Connection string 'DevDesk' was not found. Add it under ConnectionStrings:DevDesk.");

        services.AddDbContext<DevDeskDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IDevDeskDbContext>(sp => sp.GetRequiredService<DevDeskDbContext>());
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
