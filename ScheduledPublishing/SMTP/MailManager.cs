using ScheduledPublishing.Models;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace ScheduledPublishing.SMTP
{
    public static class MailManager
    {
        public static void SendEmail(string report, Item item, string sendTo)
        {
            MailMessage message = ComposeEmail(report, item, sendTo);

            if (message == null)
            {
                Log.Error("Scheduled Publish: No receiver for publishing email. " + DateTime.Now, new object());
                return;
            }

            if (NotificationEmailSettings.UseWebConfig)
            {
                try
                {
                    MainUtil.SendMail(message);
                }
                catch (Exception)
                {
                    Log.Error("Scheduled Publish: Sending publish email through web.config settings failed, continuing... ", message);
                }
            }
            else
            {
                SendMailMessage(message);
            }
        }

        private static MailMessage ComposeEmail(string report, Item item, string sendTo)
        {
            NotificationEmail mail = new NotificationEmail();
            string emailTo = sendTo;
            if (string.IsNullOrWhiteSpace(emailTo))
            {
                if (string.IsNullOrWhiteSpace(mail.EmailTo))
                {
                    return null;
                }
                emailTo = mail.EmailTo.Split(',').First();
            }

            string body = mail.Body.Replace("[item]", item.DisplayName)
                .Replace("[path]", item.Paths.FullPath)
                .Replace("[date]", DateTime.Now.ToShortDateString())
                .Replace("[time]", DateTime.Now.ToShortTimeString())
                .Replace("[version]", item.Version.ToString())
                .Replace("[id]", item.ID.ToString());

            MailMessage mailMessage = new MailMessage(mail.EmailFrom, emailTo)
            {
                Subject = mail.Subject.Replace("[item]", item.DisplayName),
                Body = body + "\r\n" + report,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(mail.EmailTo);

            return mailMessage;
        }

        private static void SendMailMessage(MailMessage mailMessage)
        {
            SmtpClient client = new SmtpClient(NotificationEmailSettings.MailServer, NotificationEmailSettings.Port);
            NetworkCredential credentials = new NetworkCredential(NotificationEmailSettings.Username, NotificationEmailSettings.Password);
            client.Credentials = credentials;

            try
            {
                client.Send(mailMessage);
            }
            catch (Exception e)
            {
                Log.Error("Scheduled Publish: Sending email failed: " + e, mailMessage);
            }
        }
    }
}