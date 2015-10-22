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
    /// Publishes scheduled items
    /// </summary>
    public class ScheduledPublishCommand
    {
        private ScheduledPublishRepo _scheduledPublishRepo;

        /// <summary>
        /// Start point of the command
        /// </summary>
        /// <param name="items">Passed items</param>
        /// <param name="command">Passed command</param>
        /// <param name="schedule">Passed schedule item</param>
        public void Run(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            Log.Info("Scheduled Publish: started", this);

            _scheduledPublishRepo = new ScheduledPublishRepo();

            Stopwatch commandStopwatch = new Stopwatch();
            commandStopwatch.Start();

            //Publish all scheduled for the last hour
            DateTime publishToDate = DateTime.Now;
            DateTime publishFromDate = publishToDate.AddHours(-1);
            PublishSchedules(publishFromDate, publishToDate);

            //Alerts for failed schedules 2 hours ago
            DateTime alertToDate = publishFromDate.AddHours(-1).AddSeconds(-1);
            DateTime alertFromDate = publishFromDate.AddHours(-2);
            AlertForFailedSchedules(alertFromDate, alertToDate);

            _scheduledPublishRepo.CleanBucket();
            commandStopwatch.Stop();
            Log.Info("Scheduled Publish: Total Run " + commandStopwatch.ElapsedMilliseconds, this);
        }

        /// <summary>
        /// Publishes all scheduled items for time period
        /// </summary>
        /// <param name="fromDate">Start of the period</param>
        /// <param name="toDate">End of the period</param>
        private void PublishSchedules(DateTime fromDate, DateTime toDate)
        {
            IEnumerable<PublishSchedule> duePublishSchedules = _scheduledPublishRepo.GetUnpublishedSchedules(fromDate, toDate);
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
                        MailManager.SendEmail(report.Message, schedule.ItemToPublish, schedule.SchedulerUsername);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("{0} {1}","Scheduled Publish: Sending publish email confirmation failed.", ex), schedule);
                    }
                }
            }
        }

        /// <summary>
        /// Alerts for all failed schedules for time period
        /// </summary>
        /// <param name="fromDate">Start of the period</param>
        /// <param name="toDate">End of the period</param>
        private void AlertForFailedSchedules(DateTime fromDate, DateTime toDate)
        {
            IEnumerable<PublishSchedule> failedSchedules = _scheduledPublishRepo.GetUnpublishedSchedules(fromDate, toDate);
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
                        MailManager.SendEmail(message, schedule.ItemToPublish, schedule.SchedulerUsername);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("{0} {1}", "Scheduled Publish: Sending publish email confirmation failed.", ex), schedule);
                    }
                }

                sbMessage.Clear();
            }
        }

        /// <summary>
        /// Sets 'IsPublished' field of already published schedule to 'true'
        /// </summary>
        /// <param name="publishSchedule">Publish Schedule</param>
        private void MarkAsPublished(PublishSchedule publishSchedule)
        {
            if (publishSchedule == null)
            {
                return;
            }

            publishSchedule.IsPublished = true;

            _scheduledPublishRepo.UpdatePublishSchedule(publishSchedule);
        }
    }
}