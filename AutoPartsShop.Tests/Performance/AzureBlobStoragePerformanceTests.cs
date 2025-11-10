using AutoPartShop.Core.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace AutoPartsShop.Tests.Performance
{
    public class AzureBlobStoragePerformanceTests
    {
        private readonly AzureBlobStorageService m_service;
        private readonly ITestOutputHelper m_output;

        public AzureBlobStoragePerformanceTests(ITestOutputHelper p_output) 
        {
            m_output = p_output;

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"AzureBlobStorage:ConnectionString", "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"},
                    {"AzureBlobStorage:ContainerName", "performance-tests"}
                })
                .Build();

            m_service = new AzureBlobStorageService(config);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldBeUnder200ms_PerFile()
        {
            const int testFileCount = 20;
            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < testFileCount; i++)
            { 
                MemoryStream content = new MemoryStream(Encoding.UTF8.GetBytes($"Performance test file content : #{i}"));
                string fileName = $"performance_test_{i}.txt"; 

                await m_service.UploadFileAsync(content, fileName);
            }

            stopwatch.Stop();

            double averageTimePerFile = stopwatch.Elapsed.TotalMilliseconds / testFileCount;
            m_output.WriteLine($"Total time: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");
            m_output.WriteLine($"Average per file: {averageTimePerFile:F2} ms");

            Assert.True(averageTimePerFile < 200, $"Average upload time per file exceeded 100 ms: {averageTimePerFile:F2} ms");
        }
    }
}
