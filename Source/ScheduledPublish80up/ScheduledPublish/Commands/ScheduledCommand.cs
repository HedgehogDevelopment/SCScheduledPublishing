using ScheduledPublish.Models;
using ScheduledPublish.Repos.Abstraction;
using Sitecore.Data.Items;
using Sitecore.Tasks;

namespace ScheduledPublish.Commands
{
    public class ScheduledCommand
    {
        private ISchedulesRepo<CommandSchedule> _scheduledPublishRepo;

        public void Run(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            //_scheduledPublishRepo = new ScheduledPublishRepo();

            //Stopwatch commandStopwatch = new Stopwatch();
            //commandStopwatch.Start();

            ////Publish all scheduled for the last hour
            //DateTime publishToDate = DateTime.Now;
            //DateTime publishFromDate = publishToDate.AddHours(-1);
            //PublishSchedules(publishFromDate, publishToDate);

            //ManageNextReccurentSchedules(publishFromDate, publishToDate);

            ////Alerts for failed schedules 2 hours agoда
            //DateTime alertToDate = publishFromDate.AddHours(-1).AddSeconds(-1);
            //DateTime alertFromDate = publishFromDate.AddHours(-2);
            //AlertForFailedSchedules(alertFromDate, alertToDate);

            //_scheduledPublishRepo.CleanBucket();
            //commandStopwatch.Stop();
            //Log.Info("Scheduled Publish: Total Run " + commandStopwatch.ElapsedMilliseconds, this);
        }
    }
}