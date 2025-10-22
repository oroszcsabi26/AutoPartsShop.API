using AutoPartShop.Core.Helpers;
using AutoPartsShop.API;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsShop.Tests.Integration
{
    public class DockerWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("TestingDocker");

            builder.ConfigureServices(services =>
            {
                // Eltávolítjuk az eredeti DbContext-regisztrációt
                ServiceDescriptor descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                ServiceDescriptor blobDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(AzureBlobStorageService)
                );
                if (blobDescriptor != null)
                    services.Remove(blobDescriptor);

                // Új, Docker SQL-hez kapcsolódó DbContext
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(
                        "Server=localhost,1433;Database=AutoPartsShop_DockerTest;" +
                        "User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;");
                });

                // A partscontroller azureblobstorage-t vár, ezért egy fake implementációt regisztrálunk
                services.AddSingleton<IAzureBlobStorageService, FakeBlobService>();

                // Az új context azonnali inicializálása
                using ServiceProvider sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            });
        }
    }
}
