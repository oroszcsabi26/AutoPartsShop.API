using AutoPartShop.Core.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Azure.Storage.Blobs;

namespace AutoPartsShop.Tests.Integration.Services
{
    public class AzureBlobStorageServiceTests
    {
        private readonly IConfiguration m_config;
        private readonly string m_containerName = "test-container";

        public AzureBlobStorageServiceTests()
        {
            var settings = new Dictionary<string, string?>
            {
                {"AzureBlobStorage:ConnectionString", "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"},
                {"AzureBlobStorage:ContainerName", m_containerName}
            };

            m_config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        [Fact]
        public async Task UploadAndDownload_ShouldReturnSameContent()
        {
            var service = new AzureBlobStorageService(m_config);
            var fileName = $"integration_{Guid.NewGuid()}.txt";
            var fileText = "Hello Azurite integration!";
            var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(fileText));

            var resultUrl = await service.UploadFileAsync(uploadStream, fileName);

            Assert.Contains(fileName, resultUrl);

            // Blob letöltése az Azurite-ból --> A blobcontainerclient inicializál egy új példányt egy connection stringel és a container névvel
            var blobClient = new BlobContainerClient(
                m_config["AzureBlobStorage:ConnectionString"],
                m_containerName
            ).GetBlobClient(fileName);

            var downloadStream = new MemoryStream();
            await blobClient.DownloadToAsync(downloadStream);

            var downloadedText = Encoding.UTF8.GetString(downloadStream.ToArray());
            Assert.Equal(fileText, downloadedText);
        }
    }
}
