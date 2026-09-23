using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace Internship.Infrastructure.Data
{
    public class RefundDisputeContextFactory : IDesignTimeDbContextFactory<RefundDisputeContext>
    {
        public RefundDisputeContext CreateDbContext(string[] args)
        {
            // Get the environment
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Build config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var builder = new DbContextOptionsBuilder<RefundDisputeContext>();
            var connectionString = configuration.GetConnectionString("DBConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Could not find connection string 'DefaultConnection'");
            }

            builder.UseSqlServer(connectionString);

            return new RefundDisputeContext(builder.Options);
        }
    }
}