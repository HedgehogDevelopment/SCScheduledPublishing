using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Tasks;
using System.Linq;
using ScheduledPublishing.Models;
using ScheduledPublishing.SMTP;
using ScheduledPublishing.Utils;

namespace ScheduledPublishing.CustomScheduledTasks
{
    /// <summary>
    /// Publishes the item(s) passed
    /// </summary>
    public class ScheduledPublishingCommand
    {
        public void ExecuteScheduledPublish(Item[] items, CommandItem command, ScheduleItem schedule)
        {
            if (items == null || items.Count() != 1)
            {
                Log.Info("Scheduled Publish Task didn't execute because of missing or invalid count of PublishOptions item", this);
                return;
            }

            var scheduledPublishOptions = new ScheduledPublishOptions(items[0]);
            var handle = ScheduledPublishManager.Publish(scheduledPublishOptions);

            //TODO: if send email feature is activated. Not sure if this belongs here since publish is async
            if (true)
            {
                var report = ScheduledPublishManager.GetPublishReport(handle);
                var sendTo = scheduledPublishOptions.SchedulerEmail;
                NotificationEmail.SendEmail(report, sendTo);
            }
        }
    }
}