using System;
using System.Linq;
using System.Text;
using ScheduledPublishing.Models;
using ScheduledPublishing.SMTP;
using ScheduledPublishing.Utils;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Tasks;

namespace ScheduledPublishing.Commands
{
    /// <summary>
    /// Publishes the item(s) passed
    /// </summary>
    public class ScheduledPublishCommand
    {
        public void Run(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            var publishToDate = DateTime.Now;
            var publishFromDate = new DateTime(publishToDate.Year, publishToDate.Month, publishToDate.Day, publishToDate.Hour, 0, 0);
            PublishSchedules(publishFromDate, publishToDate);

            var alertToDate = publishFromDate.AddSeconds(-1);
            var alertFromDate = publishFromDate.AddHours(-1);
            AlertForFailedSchedules(alertFromDate, alertToDate);

            ScheduledPublishRepository.CleanBucket();
        }

        private static void PublishSchedules(DateTime fromDate, DateTime toDate)
        {
            var duePublishOptions = ScheduledPublishRepository.GetUnpublishedSchedules(fromDate, toDate);
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

        private static void AlertForFailedSchedules(DateTime fromDate, DateTime toDate)
        {
            var failedSchedules = ScheduledPublishRepository.GetUnpublishedSchedules(fromDate, toDate);
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
                    schedule.PublishDate);
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

        private static void MarkAsPublished(PublishSchedule publishSchedule)
        {
            if (publishSchedule == null)
            {
                return;
            }

            publishSchedule.IsPublished = true;

            ScheduledPublishRepository.UpdateScheduledPublishOptions(publishSchedule);
        }
    }
}