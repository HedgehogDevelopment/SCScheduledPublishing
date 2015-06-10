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

            string to = string.Empty;
            string bcc = string.Empty;

            if (!string.IsNullOrEmpty(sendTo))
            {
                to = sendTo;
            }

            if (!string.IsNullOrWhiteSpace(mail.EmailTo))
            {
                if (string.IsNullOrEmpty(to))
                {
                    var index = mail.EmailTo.IndexOf(',');
                    if (index == -1)
                    {
                        to = mail.EmailTo.Trim();
                    }
                    else
                    {
                        to = mail.EmailTo.Substring(0, index);
                        bcc = mail.EmailTo.Substring(index + 1).Trim();
                    }
                }
                else
                {
                    bcc = mail.EmailTo;
                }
            }

            if (string.IsNullOrWhiteSpace(to))
            {
                return null;
            }

            string body = mail.Body.Replace("[item]", item.DisplayName)
                .Replace("[path]", item.Paths.FullPath)
                .Replace("[date]", DateUtil.ToServerTime(DateTime.UtcNow).ToShortDateString())
                .Replace("[time]", DateUtil.ToServerTime(DateTime.UtcNow).ToShortTimeString())
                .Replace("[version]", item.Version.ToString())
                .Replace("[id]", item.ID.ToString());

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(mail.EmailFrom),
                To = { to },
                Subject = mail.Subject.Replace("[item]", item.DisplayName),
                Body = body + Environment.NewLine + report,
                IsBodyHtml = true,
            };

            if (!string.IsNullOrEmpty(bcc))
            {
                mailMessage.Bcc.Add(bcc);
            }

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