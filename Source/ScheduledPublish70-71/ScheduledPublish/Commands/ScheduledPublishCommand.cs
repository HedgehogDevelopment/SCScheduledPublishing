using ScheduledPublish.Models;
using ScheduledPublish.Repos;
using ScheduledPublish.Smtp;
using ScheduledPublish.Utils;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Constants = ScheduledPublish.Utils.Constants;

namespace ScheduledPublish.Commands
{
    /// <summary>
    /// Publishes the item(s) passed
    /// </summary>
    public class ScheduledPublishCommand
    {
        private ScheduledPublishRepo scheduledPublishRepo;

        public void Run(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            Log.Info("Scheduled Publish: started", this);

            scheduledPublishRepo = new ScheduledPublishRepo();

            Stopwatch commandStopwatch = new Stopwatch();
            commandStopwatch.Start();

            DateTime publishToDate = DateTime.Now;
            DateTime publishFromDate = publishToDate.AddHours(-1);
            PublishSchedules(publishFromDate, publishToDate);
            
            DateTime alertToDate = publishFromDate.AddHours(-1).AddSeconds(-1);
            DateTime alertFromDate = publishFromDate.AddHours(-2);
            AlertForFailedSchedules(alertFromDate, alertToDate);

            scheduledPublishRepo.CleanBucket();

            Log.Info("Scheduled Publish: Total Run " + commandStopwatch.ElapsedMilliseconds, this);
        }

        private void PublishSchedules(DateTime fromDate, DateTime toDate)
        {
            IEnumerable<PublishSchedule> duePublishSchedules = scheduledPublishRepo.GetUnpublishedSchedules(fromDate, toDate);
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
                Log.Info("BEFORE REPORT CHECK " + report.IsSuccessful, this);
                if (report.IsSuccessful)
                {
                    Log.Info("MARK AS PUBLISHED HERE PLEASE", this);
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

        private void AlertForFailedSchedules(DateTime fromDate, DateTime toDate)
        {
            IEnumerable<PublishSchedule> failedSchedules = scheduledPublishRepo.GetUnpublishedSchedules(fromDate, toDate);
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

        private void MarkAsPublished(PublishSchedule publishSchedule)
        {
            if (publishSchedule == null)
            {
                return;
            }

            publishSchedule.IsPublished = true;

            scheduledPublishRepo.UpdatePublishSchedule(publishSchedule);
        }
    }
}