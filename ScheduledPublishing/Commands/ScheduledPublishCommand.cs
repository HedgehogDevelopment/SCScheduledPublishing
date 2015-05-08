using ScheduledPublishing.Models;
using ScheduledPublishing.SMTP;
using ScheduledPublishing.Utils;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ScheduledPublishing.Commands
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
            ScheduledPublishRepository.CleanBucket();
            bucketStopwatch.Stop();

            Log.Info("Scheduled Publish: Cleaning Buckets Stopwatch " + bucketStopwatch.ElapsedMilliseconds, this);
            Log.Info("Scheduled Publish: Total after Cleaned Buckets " + commandStopwatch.ElapsedMilliseconds, this);
            Log.Info("Scheduled Publish: Total Run " + commandStopwatch.ElapsedMilliseconds, this);
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
                    try
                    {
                        MailManager.SendEmail(report.Message, publishOptions.SchedulerEmail);
                    }
                    catch (Exception)
                    {
                        Log.Error("Scheduled Publish: Sending publish email confirmation failed, continuing... ", publishOptions);
                    }
                    
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
                sbMessage.Append("Following item failed for scheduled publish: \r\n");
                sbMessage.AppendFormat("{0} for {1}.\r\n",
                    schedule.ItemToPublish != null ? schedule.ItemToPublish.Paths.FullPath : "website",
                    schedule.PublishDate);
                sbMessage.Append("Please, review and publish it manually.\r\n");

                string message = sbMessage.ToString();

                Log.Error("Scheduled Publish: " + message, new object());

                if (ScheduledPublishSettings.IsSendEmailChecked)
                {
                    try
                    {
                        MailManager.SendEmail(message, schedule.SchedulerEmail);
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

            ScheduledPublishRepository.UpdatePublishSchedule(publishSchedule);
        }
    }
}