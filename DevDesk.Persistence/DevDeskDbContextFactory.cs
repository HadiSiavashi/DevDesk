using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DevDesk.Persistence;

public class DevDeskDbContextFactory : IDesignTimeDbContextFactory<DevDeskDbContext>
{
    public DevDeskDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DEVDESK_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=DevDesk;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<DevDeskDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new DevDeskDbContext(optionsBuilder.Options);
    }
}
