using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Cairo_Cinema.Utilities
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("qwerty.asd.ana@gmail.com", "jnpi etuy vely zwgx")
            };

            return client.SendMailAsync(
                new MailMessage(from: "qwerty.asd.ana@gmail.com", to: email, subject, htmlMessage)
                {
                    IsBodyHtml = true
                });
        }
    }
}
