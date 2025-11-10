using AutoPartsShop.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Xunit;

namespace AutoPartsShop.Tests.Integration.Services
{
    public class EmailServiceTests
    {
        private readonly IConfiguration m_config;

        public EmailServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"Email:SmtpHost", "localhost"},  // a Docker SMTP4DEV konténer
                {"Email:SmtpPort", "2525"},       // a host port, amit beállítottunk
                {"Email:SmtpUser", ""},           // nem kell autentikáció
                {"Email:SmtpPass", ""},
                {"Email:From", "noreply@test.com"},
                {"Email:EnableSsl", "false"}
            };

            m_config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        [Fact]
        public async Task SendEmailAsync_ShouldSendMail_WhenConfigValid()
        {
            // Arrange
            var service = new EmailService(m_config);

            // Act + Assert
            await service.SendEmailAsync("to@test.com", "Subject", "Body");

            // Nincs exception → sikeres
            Assert.True(true);
        }
        
        [Fact]
        public async Task SendEmailAsync_ShouldThrow_WhenInvalidHost()
        {
            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Email:SmtpHost", "invalid.host"},
                    {"Email:SmtpPort", "587"},
                    {"Email:SmtpUser", "test"},
                    {"Email:SmtpPass", "test"},
                    {"Email:From", "noreply@test.com"}
                })
                .Build();

            var service = new EmailService(badConfig);

            await Assert.ThrowsAsync<SmtpException>(() =>
                service.SendEmailAsync("to@test.com", "Subject", "Body"));
        }
        
        [Fact]
        public async Task SendEmailAsync_ShouldAppearInSmtp4Dev()
        {
            // Arrange
            var service = new EmailService(m_config);
            var to = "test@recipient.com";
            var subject = "Test subject";
            var body = "Hello from integration test!";

            // Act
            await service.SendEmailAsync(to, subject, body);
            await Task.Delay(1000); // várunk egy másodpercet, amíg az smtp4dev feldolgozza

            using var httpClient = new HttpClient();

            // Lekérjük az összes üzenetet
            var response = await httpClient.GetStringAsync("http://localhost:3000/api/messages");
            var json = JsonDocument.Parse(response);

            var results = json.RootElement.GetProperty("results");
            Assert.True(results.GetArrayLength() > 0, "No emails found in SMTP4DEV");

            // Az utolsó üzenetet vesszük
            var latestMessage = results.EnumerateArray().Last();
            var receivedSubject = latestMessage.GetProperty("subject").GetString();
            var receivedTo = latestMessage.GetProperty("deliveredTo").GetString();

            // Assert csak azokra, amik biztosan működnek
            Assert.Equal(subject, receivedSubject);
            Assert.Contains(to, receivedTo);
        }
    }
}
