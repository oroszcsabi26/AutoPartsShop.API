using AutoPartsShop.Core.Helpers;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AutoPartsShop.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient
            {
                Host = _configuration["Email:SmtpHost"],
                Port = int.Parse(_configuration["Email:SmtpPort"]),
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    _configuration["Email:SmtpUser"],
                    _configuration["Email:SmtpPass"]
                )
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:From"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
