using AutoPartsShop.Core.Helpers;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AutoPartsShop.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration m_configuration;

        public EmailService(IConfiguration configuration)
        {
            m_configuration = configuration;
        }

        public async Task SendEmailAsync(string p_toEmail, string p_subject, string p_body)
        {
            var smtpClient = new SmtpClient
            {
                Host = m_configuration["Email:SmtpHost"],
                Port = int.Parse(m_configuration["Email:SmtpPort"]),
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    m_configuration["Email:SmtpUser"],
                    m_configuration["Email:SmtpPass"]
                )
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(m_configuration["Email:From"]),
                Subject = p_subject,
                Body = p_body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(p_toEmail);
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
