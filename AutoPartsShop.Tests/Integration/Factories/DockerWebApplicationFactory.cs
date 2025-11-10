using AutoPartShop.Core.Helpers;
using AutoPartsShop.API;
using AutoPartsShop.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsShop.Tests.Integration.Factories
{
    public class DockerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly IConfiguration m_testConfig;
        private readonly string m_containerName = "test-container";

        public DockerWebApplicationFactory()
        {
            // Tesztkonfiguráció az Azurite (Docker) blob storage-hoz
            var settings = new Dictionary<string, string?>
            {
                {
                    "AzureBlobStorage:ConnectionString",
                    "DefaultEndpointsProtocol=http;" +
                    "AccountName=devstoreaccount1;" +
                    "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
                    "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
                },
                { "AzureBlobStorage:ContainerName", m_containerName }
            };

            m_testConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("TestingDocker");

            builder.ConfigureServices(services =>
            {
                // --- SQL Server konfigurálása Docker MSSQL-hez ---
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                var dbName = $"AutoPartsShop_DockerTest_{Guid.NewGuid():N}";

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(
                        $"Server=localhost,1433;Database={dbName};User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;");
                });

                // --- Saját konfiguráció regisztrálása (Azurite-hoz) ---
                services.AddSingleton<IConfiguration>(m_testConfig);

                // --- Valódi AzureBlobStorageService regisztrálása ---
                var blobDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IAzureBlobStorageService));
                if (blobDescriptor != null)
                    services.Remove(blobDescriptor);

                services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();

                // --- Adatbázis inicializálása ---
                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            });
        }
    }
}
