using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ScheduledPublish.Models;
using ScheduledPublish.Repos;
using ScheduledPublish.Smtp;
using ScheduledPublish.Utils;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Tasks;
using Constants = ScheduledPublish.Utils.Constants;

namespace ScheduledPublish.Commands
{
    /// <summary>
    /// Publishes the item(s) passed
    /// </summary>
    public class ScheduledPublishCommand
    {
        public void Run(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            Log.Info("Scheduled Publish: started", this);

            Stopwatch commandStopwatch = new Stopwatch();
            commandStopwatch.Start();
            Stopwatch publishStopwatch = new Stopwatch();
            Stopwatch alertStopwatch = new Stopwatch();
            Stopwatch bucketStopwatch = new Stopwatch();

            publishStopwatch.Start();
            DateTime publishToDate = DateTime.Now;
            DateTime publishFromDate = publishToDate.AddHours(-1);
            PublishSchedules(publishFromDate, publishToDate);
            publishStopwatch.Stop();

            Log.Info("Scheduled Publish: Publishing Stopwatch " + publishStopwatch.ElapsedMilliseconds, this);
            Log.Info("Scheduled Publish: Total after Published Schedules " + commandStopwatch.ElapsedMilliseconds, this);

            alertStopwatch.Start();
            DateTime alertToDate = publishFromDate.AddHours(-1).AddSeconds(-1);
            DateTime alertFromDate = publishFromDate.AddHours(-2);
            AlertForFailedSchedules(alertFromDate, alertToDate);
            alertStopwatch.Stop();

            Log.Info("Scheduled Publish: Alert Failed Schedules Stopwatch " + alertStopwatch.ElapsedMilliseconds, this);
            Log.Info("Scheduled Publish: Total after Alerted Failed Schedules " + commandStopwatch.ElapsedMilliseconds, this);

            bucketStopwatch.Start();
            ScheduledPublishRepo.CleanBucket();
            bucketStopwatch.Stop();

            Log.Info("Scheduled Publish: Cleaning Buckets Stopwatch " + bucketStopwatch.ElapsedMilliseconds, this);
            Log.Info("Scheduled Publish: Total after Cleaned Buckets " + commandStopwatch.ElapsedMilliseconds, this);
            Log.Info("Scheduled Publish: Total Run " + commandStopwatch.ElapsedMilliseconds, this);
        }

        private void PublishSchedules(DateTime fromDate, DateTime toDate)
        {
            IEnumerable<PublishSchedule> duePublishSchedules = ScheduledPublishRepo.GetUnpublishedSchedules(fromDate, toDate);
            if (duePublishSchedules == null)
            {
                Log.Info(string.Format("Scheduled Publish: No publish schedules from {0} to {1}",
                    fromDate.ToString(Context.Culture),
                    toDate.ToString(Context.Culture)), this);
                return;
            }

            foreach (var schedule in duePublishSchedules)
            {
                Handle handle = ScheduledPublishManager.Publish(schedule);
                ScheduledPublishReport report = ScheduledPublishManager.GetScheduledPublishReport(handle);

                if (report.IsSuccessful)
                {
                    MarkAsPublished(schedule);
                }

                if (ScheduledPublishSettings.IsSendEmailChecked)
                {
                    try
                    {
                        MailManager.SendEmail(report.Message, schedule.ItemToPublish, schedule.SchedulerEmail);
                    }
                    catch (Exception)
                    {
                        Log.Error("Scheduled Publish: Sending publish email confirmation failed, continuing... ", schedule);
                    }
                }
            }
        }

        private static void AlertForFailedSchedules(DateTime fromDate, DateTime toDate)
        {
            IEnumerable<PublishSchedule> failedSchedules = ScheduledPublishRepo.GetUnpublishedSchedules(fromDate, toDate);
            if (failedSchedules == null)
            {
                return;
            }

            List<PublishSchedule> failedPublishSchedules = failedSchedules.ToList();
            if (!failedPublishSchedules.Any())
            {
                return;
            }

            StringBuilder sbMessage = new StringBuilder();

            foreach (var schedule in failedPublishSchedules)
            {
                sbMessage.AppendLine("Following item failed for scheduled publish:");
                sbMessage.AppendFormat("{0} for {1}.",
                                        schedule.ItemToPublish != null ? schedule.ItemToPublish.Paths.FullPath : Constants.WEBSITE_PUBLISH_TEXT,
                                        schedule.PublishDate);
                sbMessage.AppendLine();
                sbMessage.Append("Please, review and publish it manually.");

                string message = sbMessage.ToString();

                Log.Error("Scheduled Publish: " + message, new object());

                if (ScheduledPublishSettings.IsSendEmailChecked)
                {
                    try
                    {
                        MailManager.SendEmail(message, schedule.ItemToPublish, schedule.SchedulerEmail);
                    }
                    catch (Exception)
                    {
                        Log.Error("Scheduled Publish: Sending failed publish email notification failed, continuing... ", schedule);
                    }
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

            ScheduledPublishRepo.UpdatePublishSchedule(publishSchedule);
        }
    }
}