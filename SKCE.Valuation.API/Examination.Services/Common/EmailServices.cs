using System;
using System.Configuration;
using System.Net.Mail;

using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SKCE.Examination.Models.DbModels.Common;
using SKCE.Examination.Services.Helpers;

public class EmailService
{
    private readonly string SmtpServer = "smtp.gmail.com";
    private readonly int SmtpPort = 587;
    private readonly string EmailSender = "swt@srikrishnaitech.com"; // Replace with your Gmail
    private readonly string EmailPassword = "hdwl cfbw unsd hfir"; // Replace with your App Password
    private readonly string FromAddress = "swt@srikrishnaitech.com"; // Replace with your Gmail
    private readonly AzureBlobStorageHelper _blobStorageHelper;
    public EmailService(IConfiguration configuration, AzureBlobStorageHelper blobStorageHelper)
    {
        SmtpServer = configuration["Email:SmtpHost"];
        SmtpPort = int.Parse(configuration["Email:SmtpPort"]); // Convert string to int
        EmailSender = configuration["Email:Username"];
        EmailPassword = configuration["Email:Password"];
        FromAddress = configuration["Email:FromAddress"];
        _blobStorageHelper = blobStorageHelper;
    }
    public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body, long documentId = 0)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromAddress, EmailSender));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = subject;

            message.Body = new TextPart("plain") { Text = body };
            var memoryStream = new MemoryStream();
            if (documentId > 0)
            {
                // Create an attachment
                var (blobStream, blobName) = await _blobStorageHelper.DownloadFileAsync(documentId);
                await blobStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset to beginning before attaching
                var attachment = new MimePart("application/octet-stream")
                {
                    Content = new MimeContent(memoryStream),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = blobName
                };
                var multipart = new Multipart("mixed") { message.Body, attachment };
                message.Body = multipart;
            }

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(EmailSender, EmailPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine("Email sent successfully!");
            // Now after email sent, we can dispose memoryStream manually
            memoryStream.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            return false;
        }
    }
}

