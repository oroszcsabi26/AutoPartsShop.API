using AutoPartsShop.Core.Helpers;

namespace AutoPartsShop.Tests.Unit.Helpers
{
    public class FakeEmailService : IEmailService
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendEmailAsync(string p_toEmail, string p_subject, string p_body)
        {
            Sent.Add((p_toEmail,p_subject,p_body));
            return Task.CompletedTask;
        }
    }
}
