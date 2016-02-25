using ScheduledPublish.Models;
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
using ScheduledPublish.Recurrence.Abstraction;
using ScheduledPublish.Recurrence.Implementation;
using ScheduledPublish.Repos.Abstraction;
using ScheduledPublish.Repos.Implementation;
using Constants = ScheduledPublish.Utils.Constants;

namespace ScheduledPublish.Commands
{
    /// <summary>
    /// Publishes scheduled items
    /// </summary>
    public class ScheduledPublishCommand
    {
        private ISchedulesRepo<PublishSchedule> _schedulesRepo;

        /// <summary>
        /// Start point of the command
        /// </summary>
        /// <param name="items">Passed items</param>
        /// <param name="command">Passed command</param>
        /// <param name="schedule">Passed schedule item</param>
        public void Run(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            Log.Info("Scheduled Publish: started", this);

            _schedulesRepo = new ScheduledPublishRepo();

            Stopwatch commandStopwatch = new Stopwatch();
            commandStopwatch.Start();

            //Publish all scheduled for the last hour
            DateTime publishToDate = DateTime.Now;
            DateTime publishFromDate = publishToDate.AddHours(-1);
            PublishSchedules(publishFromDate, publishToDate);

            ManageNextReccurentSchedules(publishFromDate, publishToDate);

            //Alerts for failed schedules 2 hours ago
            DateTime alertToDate = publishFromDate.AddHours(-1).AddSeconds(-1);
            DateTime alertFromDate = publishFromDate.AddHours(-2);
            AlertForFailedSchedules(alertFromDate, alertToDate);

            _schedulesRepo.CleanRepo();
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
            IEnumerable<PublishSchedule> duePublishSchedules = _schedulesRepo.GetUnexecutedSchedules(fromDate, toDate);

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
                        MailManager.SendEmail(report.Message, schedule.Items.FirstOrDefault(), schedule.SchedulerUsername);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("{0} {1}", "Scheduled Publish: Sending publish email confirmation failed.", ex), schedule);
                    }
                }
            }
        }

        /// <summary>
        /// If recurrent publish has succeeded we schedule the next publish 
        /// moving the publish settings item in the repo.
        /// </summary>
        /// <param name="fromDate">Start of the period</param>
        /// <param name="toDate">End of the period</param>
        private void ManageNextReccurentSchedules(DateTime fromDate, DateTime toDate)
        {
            IRecurringScheduler recurrenceScheduler = new RecurringScheduler();

            IEnumerable<PublishSchedule> dueReccurentSchedules = _schedulesRepo.GetRecurringSchedules(fromDate, toDate);

            foreach (var schedule in dueReccurentSchedules)
            {
                recurrenceScheduler.ScheduleNextRecurrence(schedule);
                schedule.IsExecuted = false;
                _schedulesRepo.UpdateSchedule(schedule);
            }
        }

        /// <summary>
        /// Alerts for all failed schedules for time period
        /// </summary>
        /// <param name="fromDate">Start of the period</param>
        /// <param name="toDate">End of the period</param>
        private void AlertForFailedSchedules(DateTime fromDate, DateTime toDate)
        {
            IEnumerable<PublishSchedule> failedSchedules = _schedulesRepo.GetUnexecutedSchedules(fromDate, toDate);
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
                                        schedule.Items.Any() ? schedule.Items.First().Paths.FullPath : Constants.WEBSITE_PUBLISH_TEXT,
                                        schedule.ScheduledDate);
                sbMessage.AppendLine();
                sbMessage.Append("Please, review and publish it manually.");

                string message = sbMessage.ToString();

                Log.Error("Scheduled Publish: " + message, new object());

                if (ScheduledPublishSettings.IsSendEmailChecked)
                {
                    try
                    {
                        MailManager.SendEmail(message, schedule.Items.FirstOrDefault(), schedule.SchedulerUsername);
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

            publishSchedule.IsExecuted = true;

            _schedulesRepo.UpdateSchedule(publishSchedule);
        }
    }
}