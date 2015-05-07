using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScheduledPublishing.Models;
using ScheduledPublishing.SMTP;
using ScheduledPublishing.Utils;
using Sitecore;
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
            DateTime publishToDate = DateTime.Now;
            DateTime publishFromDate = new DateTime(publishToDate.Year, publishToDate.Month, publishToDate.Day, publishToDate.Hour, 0, 0);
            PublishSchedules(publishFromDate, publishToDate);

            DateTime alertToDate = publishFromDate.AddSeconds(-1);
            DateTime alertFromDate = publishFromDate.AddHours(-1);
            AlertForFailedSchedules(alertFromDate, alertToDate);

            ScheduledPublishRepository.CleanBucket();
        }

        private static void PublishSchedules(DateTime fromDate, DateTime toDate)
        {
            IEnumerable<PublishSchedule> duePublishOptions = ScheduledPublishRepository.GetUnpublishedSchedules(fromDate, toDate);
            if (duePublishOptions == null)
            {
                return;
            }

            foreach (var publishOptions in duePublishOptions)
            {
                Handle handle = ScheduledPublishManager.Publish(publishOptions);
                ScheduledPublishReport report = ScheduledPublishManager.GetScheduledPublishReport(handle);

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
            IEnumerable<PublishSchedule> failedSchedules = ScheduledPublishRepository.GetUnpublishedSchedules(fromDate, toDate);
            if (failedSchedules == null)
            {
                return;
            }

            List<PublishSchedule> failedSchedulesList = failedSchedules.ToList();
            if (!failedSchedulesList.Any())
            {
                return;
            }

            StringBuilder sbMessage = new StringBuilder();

            foreach (var schedule in failedSchedulesList)
            {
                sbMessage.Append("Following item failed for scheduled publish: <br/>");
                sbMessage.AppendFormat("{0} for {1}.<br/>",
                    schedule.ItemToPublish != null ? schedule.ItemToPublish.Paths.FullPath : "website",
                    schedule.PublishDate);
                sbMessage.Append("Please, review and publish it manually.<br/>");

                string message = sbMessage.ToString();

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