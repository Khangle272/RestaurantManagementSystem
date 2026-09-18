using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantManagement.API.Data;

public class RestaurantDbContextFactory : IDesignTimeDbContextFactory<RestaurantDbContext>
{
    public RestaurantDbContext CreateDbContext(string[] args)
    {
        // Design-time fallback permits script generation without connecting to a server.
        // Set the environment variable to the verified instance before database update.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__RestaurantDb")
            ?? "Server=localhost;Database=RestaurantManagement;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        return new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlServer(connection).Options);
    }
}
