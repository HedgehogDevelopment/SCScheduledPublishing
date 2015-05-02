using System.Collections.Generic;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Tasks;
using ScheduledPublishing.Models;
using ScheduledPublishing.SMTP;
using ScheduledPublishing.Utils;
using System;
using System.Linq;
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
            if (items == null)
            {
                Log.Info("Scheduled Publish Task didn't execute because of missing PublishOptions item", this);
                return;
            }

            IEnumerable<ScheduledPublishOptions> duePublishOptions = GetDueScheduledPublishOptions();

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

        private IEnumerable<ScheduledPublishOptions> GetDueScheduledPublishOptions()
        {
            try
            {
                var toDate = DateTime.Now;
                var fromDate = new DateTime(toDate.Year, toDate.Month, toDate.Day, toDate.Hour, 0, 0);

                Item currentHourFolder = GetHourFolder(fromDate);
                if (currentHourFolder == null || currentHourFolder.Children == null)
                {
                    return Enumerable.Empty<ScheduledPublishOptions>();
                }

                return ScheduledPublishOptionsManager.GetUnpublishedScheduledOptions(currentHourFolder, fromDate, toDate);
            }
            catch (Exception ex)
            {
                Log.Info(ex.ToString(), this);
                return Enumerable.Empty<ScheduledPublishOptions>();
            }
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