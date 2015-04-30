using Sitecore;
using Sitecore.Diagnostics;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace ScheduledPublishing.SMTP
{
    public static class MailManager
    {
        public static void SendEmail(string report, string sendTo)
        {
            MailMessage message = ComposeEmail(report, sendTo);

            if (message == null)
            {
                Log.Error("No receiver for publishing email. " + DateTime.Now, new object());
                return;
            }

            if (NotificationEmailSettings.UseWebConfig)
            {
                MainUtil.SendMail(message);
            }
            else
            {
                SendMailMessage(message);
            }
        }

        private static MailMessage ComposeEmail(string report, string sendTo)
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

            var mailMessage = new MailMessage(mail.EmailFrom, emailTo)
            {
                Subject = mail.Subject,
                Body = mail.Body + "\r\n" + report,
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
                Log.Error("Sending email failed: " + e, mailMessage);
            }
        }
    }
}