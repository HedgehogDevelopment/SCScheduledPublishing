using ScheduledPublish.Models;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace ScheduledPublish.Smtp
{
    /// <summary>
    /// Handles email notifications
    /// </summary>
    public static class MailManager
    {
        /// <summary>
        /// Composes and sends an email according to user-defined preferences.
        /// </summary>
        /// <param name="report">Report to append to email body.</param>
        /// <param name="item">Item to send information on.</param>
        /// <param name="sendTo">Receiver's email address.</param>
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
                catch (Exception ex)
                {
                    Log.Error(string.Format("{0} {1}", "Scheduled Publish: Sending publish email through web.config settings failed.", ex), message);
                }
            }
            else
            {
                SendMailMessage(message);
            }
        }

        /// <summary>
        /// Composes email using content-managed text and placeholders.
        /// </summary>
        /// <param name="report">Report to append to email body.</param>
        /// <param name="item">Item to send information on.</param>
        /// <param name="sendTo">Receiver's email address.</param>
        /// <returns>An <see cref="T:System.Net.Mail.MailMessage"/> email message ready to send.</returns>
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
                Body = body + Environment.NewLine + report,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(mail.EmailTo);

            return mailMessage;
        }

        /// <summary>
        /// Creates an SMTP instance and sends email through it.
        /// </summary>
        /// <param name="mailMessage">An <see cref="T:System.Net.Mail.MailMessage"/> email message ready to send.</param>
        private static void SendMailMessage(MailMessage mailMessage)
        {
            SmtpClient client = new SmtpClient(NotificationEmailSettings.MailServer, NotificationEmailSettings.Port);
            NetworkCredential credentials = new NetworkCredential(NotificationEmailSettings.Username, NotificationEmailSettings.Password);
            client.Credentials = credentials;

            try
            {
                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Log.Error("Scheduled Publish: Sending email failed: " + ex, mailMessage);
            }
        }
    }
}