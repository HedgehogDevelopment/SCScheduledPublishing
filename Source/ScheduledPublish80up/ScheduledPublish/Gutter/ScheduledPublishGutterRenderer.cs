using System.Collections.Generic;
using System.Linq;
using ScheduledPublish.Models;
using ScheduledPublish.Repos.Abstraction;
using ScheduledPublish.Repos.Implementation;
using ScheduledPublish.Utils;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor.Gutters;

namespace ScheduledPublish.Gutter
{
    public class ScheduledPublishGutterRenderer : GutterRenderer
    {
        private ISchedulesRepo<PublishSchedule> _schedulesRepo;
          
        protected override GutterIconDescriptor GetIconDescriptor(Item item)
        {
            if (item == null)
            {
                return null;
            }

            _schedulesRepo = new ScheduledPublishRepo();

            using (new LanguageSwitcher(LanguageManager.DefaultLanguage))
            {
                IEnumerable<PublishSchedule> schedulesForCurrentItem = _schedulesRepo.GetSchedules(item.ID);

                if (schedulesForCurrentItem.Any())
                {
                    return new GutterIconDescriptor
                    {
                        Icon = Constants.SCHEDULED_PUBLISH_ICON,
                        Tooltip = Constants.SCHEDULED_PUBLISH_NOTIFICATION
                    };

                }

            }

            return null;
        }
    }
}