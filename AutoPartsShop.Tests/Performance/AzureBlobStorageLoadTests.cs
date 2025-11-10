using AutoPartShop.Core.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace AutoPartsShop.Tests.Performance
{
    public class AzureBlobStorageLoadTests
    {
        private readonly AzureBlobStorageService m_service;
        private readonly ITestOutputHelper m_output;

        public AzureBlobStorageLoadTests(ITestOutputHelper p_output)
        {
            m_output = p_output;

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"AzureBlobStorage:ConnectionString", "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"},
                    {"AzureBlobStorage:ContainerName", "load-tests"}
                })
                .Build();

            m_service = new AzureBlobStorageService(config);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldHandleConcurrentUploadsEfficiently()
        {
            const int concurrentUploads = 25;

            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Task> tasks = Enumerable.Range(1, concurrentUploads).Select(async i =>
            {
                MemoryStream data = new MemoryStream(Encoding.UTF8.GetBytes($"Concurrent test file #{i}"));
                string fileName = $"loadtest_{i}.txt";
                await m_service.UploadFileAsync(data, fileName);
            }).ToList();

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            double totalMs = stopwatch.Elapsed.TotalMilliseconds;
            double avgMs = totalMs / concurrentUploads;

            m_output.WriteLine($" Total concurrent uploads: {concurrentUploads}");
            m_output.WriteLine($" Total time: {totalMs:F2} ms");
            m_output.WriteLine($" Avg per upload: {avgMs:F2} ms");

            Assert.True(avgMs < 200, $"Average upload time too high: {avgMs:F2} ms");
        }
    }
}
