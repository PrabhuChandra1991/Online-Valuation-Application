using System;
using System.Configuration;
using System.Net.Mail;

using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SKCE.Examination.Models.DbModels.Common;

public class EmailService
{
    private readonly string SmtpServer = "smtp.gmail.com";
    private readonly int SmtpPort = 587;
    private readonly string EmailSender = "swt@srikrishnaitech.com"; // Replace with your Gmail
    private readonly string EmailPassword = "hdwl cfbw unsd hfir"; // Replace with your App Password
    private readonly string FromAddress = "swt@srikrishnaitech.com"; // Replace with your Gmail

    public EmailService(IConfiguration configuration)
    {
        SmtpServer = configuration["Email:SmtpHost"];
        SmtpPort = int.Parse(configuration["Email:SmtpPort"]); // Convert string to int
        EmailSender = configuration["Email:Username"];
        EmailPassword = configuration["Email:Password"];
        FromAddress = configuration["Email:FromAddress"];
    }
    public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromAddress, EmailSender));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = subject;

            message.Body = new TextPart("plain") { Text = body };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(EmailSender, EmailPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine("Email sent successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            return false;
        }
    }
}

