using System;
using System.Net;
using System.Net.Mail;

using Google.Apis.Auth;


public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            using (var client = new SmtpClient(_configuration["EmailSettings:SmtpHost"],
                                               int.Parse(_configuration["EmailSettings:SmtpPort"])))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(
                    _configuration["EmailSettings:SmtpUsername"],
                    _configuration["EmailSettings:SmtpPassword"]
                );

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["EmailSettings:FromEmail"],
                                           _configuration["EmailSettings:FromName"]),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine(" Email sent successfully to " + email);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(" Error sending email: " + ex.Message);
        }
    }



}
