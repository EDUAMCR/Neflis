using Microsoft.Extensions.Options;
using Neflis.Models;
using System.Net;
using System.Net.Mail;

namespace Neflis.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task EnviarCorreoAsync(string destino, string asunto, string cuerpoHtml)
        {
            var msg = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };

            msg.To.Add(destino);

            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass),
                EnableSsl = true
            };

            await client.SendMailAsync(msg);
        }
    }
}
