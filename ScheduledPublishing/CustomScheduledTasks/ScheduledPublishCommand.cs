using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Tasks;
using ScheduledPublishing.Models;
using ScheduledPublishing.SMTP;
using ScheduledPublishing.Utils;
using System;
using System.Linq;
using System.Text;
using Sitecore.Data;
using Sitecore.SecurityModel;

namespace ScheduledPublishing.CustomScheduledTasks
{
    /// <summary>
    /// Publishes the item(s) passed
    /// </summary>
    public class ScheduledPublishCommand
    {
        private readonly Database _database = Utils.Utils.ScheduledTasksContextDatabase;

        public void Run(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            var toDate = DateTime.Now;
            var fromDate = new DateTime(toDate.Year, toDate.Month, toDate.Day, toDate.Hour, 0, 0);

            PublishSchedules(fromDate, toDate);
            AlertForFailedSchedules(fromDate.AddHours(-1));
        }

        private void PublishSchedules(DateTime fromDate, DateTime toDate)
        {
            Item hourFolder = GetHourFolder(fromDate);
            if (hourFolder == null)
            {
                return;
            }

            var duePublishOptions = ScheduledPublishOptionsManager.GetUnpublishedScheduledOptions(hourFolder, fromDate, toDate);
            if (duePublishOptions == null)
            {
                return;
            }

            foreach (var publishOptions in duePublishOptions)
            {
                var handle = ScheduledPublishManager.Publish(publishOptions);
                var report = ScheduledPublishManager.GetScheduledPublishReport(handle);

                if (report.IsSuccessful)
                {
                    MarkAsPublished(publishOptions);
                }

                if (ScheduledPublishSettings.IsSendEmailChecked)
                {
                    MailManager.SendEmail(report.Message, publishOptions.SchedulerEmail);
                }
            }
        }

        private void AlertForFailedSchedules(DateTime fromDate)
        {
            Item hourFolder = GetHourFolder(fromDate);
            if (hourFolder == null)
            {
                return;
            }

            var failedSchedules = ScheduledPublishOptionsManager.GetUnpublishedScheduledOptions(hourFolder);
            if (failedSchedules == null)
            {
                return;
            }

            var failedSchedulesList = failedSchedules.ToList();
            if (!failedSchedulesList.Any())
            {
                return;
            }

            var sbMessage = new StringBuilder();

            foreach (var schedule in failedSchedulesList)
            {
                sbMessage.Append("Following item failed for scheduled publish: <br/>");
                sbMessage.AppendFormat("{0} for {1}.<br/>",
                    schedule.ItemToPublish != null ? schedule.ItemToPublish.Paths.FullPath : "website",
                    schedule.PublishDateString);
                sbMessage.Append("Please, review and publish it manually.<br/>");

                var message = sbMessage.ToString();

                Log.Error(message, new object());

                if (ScheduledPublishSettings.IsSendEmailChecked)
                {
                    MailManager.SendEmail(message, schedule.SchedulerEmail);
                }

                sbMessage.Clear();
            }
        }

        private Item GetHourFolder(DateTime dateTime)
        {
            Item publishOptionsFolder = _database.GetItem(Constants.PUBLISH_OPTIONS_FOLDER_ID);

            if (publishOptionsFolder == null)
            {
                return null;
            }

            var path = string.Format("{0}/{1}/{2}/{3}/{4}/", publishOptionsFolder.Paths.FullPath, dateTime.Year,
                dateTime.Month, dateTime.Day, dateTime.Hour);

            return _database.GetItem(path);
        }

        private void MarkAsPublished(ScheduledPublishOptions scheduledPublishOptions)
        {
            try
            {
                using (new SecurityDisabler())
                {
                    scheduledPublishOptions.InnerItem.Editing.BeginEdit();

                    scheduledPublishOptions.IsPublished = true;

                    scheduledPublishOptions.InnerItem.Editing.AcceptChanges();
                    scheduledPublishOptions.InnerItem.Editing.EndEdit();
                }
            }
            catch (Exception ex)
            {
                Log.Info(
                    string.Format("Failed marking {0} - Publish Options as 'Published'. {1}",
                        scheduledPublishOptions.InnerItem.ID,
                        ex), this);
            }
        }
    }
}