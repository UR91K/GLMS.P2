using GLMS.Api;
using GLMS.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GLMS.Tests.Integration;

public class ApiWebApplicationFactory : WebApplicationFactory<GlmsApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // remove all EF registrations that reference AppDbContext
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(IDbContextFactory<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext))))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContextFactory<AppDbContext>(opts =>
                opts.UseInMemoryDatabase("GlmsIntegrationTestDb_" + Guid.NewGuid()));
        });
    }
}
