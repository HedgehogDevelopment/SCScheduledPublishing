using System.Linq;
using ScheduledPublishing.Utils;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;
using System.Net.Mail;

namespace ScheduledPublishing.SMTP
{
    public static class NotificationEmail
    {
        public static Item InnerItem
        {
            get { return Sitecore.Context.ContentDatabase.GetItem(Constants.SCHEUDLED_PUBLISH_EMAIL); }
        }

        public static string EmailTo
        {
            get { return InnerItem[ID.Parse("{A35E5B44-1CD0-49C4-B210-DB4106685CE4}")]; }
        }

        public static string EmailFrom
        {
            get { return InnerItem[ID.Parse("{4A6046AA-2B94-47B2-9A27-3B83F0601822}")]; }
        }

        public static string Subject
        {
            get { return InnerItem[ID.Parse("{20DEB7CE-6AD1-459F-B1A4-F6AE88B2C62A}")]; }
        }

        public static string Body
        {
            get { return InnerItem[ID.Parse("{39A13A96-8BF0-4334-B366-16C0ACCC2B60}")]; }
        }

        public static string MailServer
        {
            get { return InnerItem[ID.Parse("{CB095BAB-E9B7-4131-88DB-13F6EB917310}")]; }
        }

        public static void SendEmail(string report, string sendTo)
        {
            string emailTo = sendTo;

            var mailMessage = new MailMessage(EmailFrom, emailTo)
            {
                Subject = Subject,
                Body = report,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(EmailTo);

            SendMailMessage(mailMessage);
        }

        public static void SendEmail(Item item, string sendTo)
        {
            string emailTo = sendTo;
            string body = Body.Replace("[item]", item.DisplayName)
                .Replace("[path]", item.Paths.FullPath)
                .Replace("[date]", DateTime.Now.ToShortDateString())
                .Replace("[time]", DateTime.Now.ToShortTimeString())
                .Replace("[version]", item.Version.ToString())
                .Replace("[id]", item.ID.ToString());
            
            var mailMessage = new MailMessage(EmailFrom, emailTo)
            {
                Subject = Subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(EmailTo);

            SendMailMessage(mailMessage);
        }

        private static void SendMailMessage(MailMessage mailMessage)
        {
            SmtpClient client = new SmtpClient(MailServer, Convert.ToInt32(587));

            try
            {
                client.Send(mailMessage);
            }
            catch (Exception e)
            {
                Log.Error("Sending email failed: " + e.ToString(), mailMessage);
            }
        }
    }
}