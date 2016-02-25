using ScheduledPublish.Models;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetContentEditorWarnings;
using System.Collections.Generic;
using System.Linq;
using ScheduledPublish.Repos.Abstraction;
using ScheduledPublish.Repos.Implementation;
using ScheduledPublish.Utils;

namespace ScheduledPublish.Pipelines.ContentEditorWarnings
{
    /// <summary>
    /// Checks if current item has created publish schedules 
    /// and shows yellow worning in content editor 
    /// </summary>
    public class HasScheduledPublish
    {
        private ISchedulesRepo<PublishSchedule> _schedulesRepo;

        public void Process(GetContentEditorWarningsArgs args)
        {
            Item item = args.Item;
            Assert.IsNotNull(item, "item");

            _schedulesRepo = new ScheduledPublishRepo();

            using (new LanguageSwitcher(LanguageManager.DefaultLanguage))
            {
                IEnumerable<PublishSchedule> schedulesForCurrentItem = _schedulesRepo.GetSchedules(item.ID);

                if (schedulesForCurrentItem.Any())
                {
                    GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
                    warning.Icon = Constants.SCHEDULED_PUBLISH_ICON;
                    warning.Text = Constants.SCHEDULED_PUBLISH_NOTIFICATION;
                    warning.IsExclusive = false;
                }
            }
        }
    }
}