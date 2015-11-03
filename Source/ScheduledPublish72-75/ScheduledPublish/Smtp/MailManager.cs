using System.Collections.Generic;
using System.Linq;
using ScheduledPublish.Extensions;
using ScheduledPublish.Models;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;
using System.Net;
using System.Net.Mail;
using Sitecore.Security.Accounts;

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
                Log.Info("Scheduled Publish: No receiver for publishing email. " + DateTime.Now, new object());
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
        /// <param name="username">Receiver.</param>
        /// <returns>An <see cref="T:System.Net.Mail.MailMessage"/> email message ready to send.</returns>
        private static MailMessage ComposeEmail(string report, Item item, string username)
        {
            NotificationEmail mail = new NotificationEmail();

            string to = string.Empty;
            string bcc = string.Empty;
            string publishedBy = string.Empty;

            if (!string.IsNullOrWhiteSpace(username))
            {
                var author = User.FromName(username, false);
                if (author.Profile != null && !string.IsNullOrWhiteSpace(author.Profile.Email))
                {
                    to = author.Profile.Email;
                    publishedBy = !string.IsNullOrWhiteSpace(author.Profile.FullName)
                        ? author.Profile.FullName
                        : author.Name;
                }
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

            if (SectionsEmailSettings.Enabled)
            {
                string emailList = GetEmailsForSection(item);
                if (!string.IsNullOrEmpty(emailList))
                {
                    if (!string.IsNullOrWhiteSpace(bcc))
                    {
                        bcc = string.Format("{0}, {1}", bcc, emailList);
                    }
                    else
                    {
                        bcc = emailList;
                    }
                }
                to = string.IsNullOrWhiteSpace(to) && bcc.Length > 0
                        ? bcc.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)[0]
                        : to;
            }

            if (bcc.Contains(","))
            {
                var uniqueBccAddresses = bcc
                    .Replace(to, "")
                    .Split(new[] {", "}, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct();

                bcc = string.Join(", ", uniqueBccAddresses);
            }

            if (string.IsNullOrWhiteSpace(to))
            {
                return null;
            }

            string subject = mail.Subject
                .Replace("[item]", item.DisplayName)
                .Replace("[path]", item.Paths.FullPath)
                .Replace("[date]", DateTime.Now.ToShortDateString())
                .Replace("[time]", DateTime.Now.ToShortTimeString())
                .Replace("[version]", item.Version.ToString())
                .Replace("[id]", item.ID.ToString())
                .Replace("[publisher]", publishedBy);

            string body = mail.Body
                .Replace("[item]", item.DisplayName)
                .Replace("[path]", item.Paths.FullPath)
                .Replace("[date]", DateTime.Now.ToShortDateString())
                .Replace("[time]", DateTime.Now.ToShortTimeString())
                .Replace("[version]", item.Version.ToString())
                .Replace("[id]", item.ID.ToString())
                .Replace("[publisher]", publishedBy);

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(mail.EmailFrom),
                To = { to },
                Subject = subject,
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

        private static string GetEmailsForSection(Item item)
        {
            const string sectionNotFoundMessage = "Item '{0}' is not in any section";
            const string roleNotFoundMessage = "Could not find roles to notify for item '{0}'!";
            const string usersNotFoundMessage = "Could not find users to notify for item '{0}'!";

            ScheduledPublishSection section = item.GetParentSection();

            if (section == null)
            {
                Log.Warn(string.Format(sectionNotFoundMessage, item.Name), typeof(MailManager));
                return string.Empty;
            }

            IEnumerable<string> sectionRoleNames = section.SectionRoleNames;
            IList<Role> sectionRoles = sectionRoleNames.Where(Role.Exists).Select(Role.FromName).ToList();

            if (sectionRoles.Count == 0)
            {
                Log.Error(string.Format(roleNotFoundMessage, item.Name), typeof(MailManager));
                return string.Empty;
            }

            IList<User> users = new List<User>();

            foreach (var sectionRole in sectionRoles)
            {
                users = users.Union(RolesInRolesManager.GetUsersInRole(sectionRole, true)).ToList();
            }

            if (users.Count == 0)
            {
                Log.Warn(string.Format(usersNotFoundMessage, item.Name), typeof(MailManager));
                return string.Empty;
            }

            IEnumerable<string> emailList = users
                .Where(x => !string.IsNullOrWhiteSpace(x.Profile.Email))
                .Select(x => x.Profile.Email);

            return string.Join(", ", emailList);
        }
    }
}