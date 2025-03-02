using System;
using System.Configuration;
using System.Net.Mail;

using MailKit.Security;
using MimeKit;

public class EmailService
{
    private const string SmtpServer = "smtp.gmail.com";
    private const int SmtpPort = 587;
    private const string EmailSender = "venky27585@gmail.com"; // Replace with your Gmail
    private const string EmailPassword = "meru uhrk qcmm xhru"; // Replace with your App Password

    public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("venky27585@gmail.com", EmailSender));
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

