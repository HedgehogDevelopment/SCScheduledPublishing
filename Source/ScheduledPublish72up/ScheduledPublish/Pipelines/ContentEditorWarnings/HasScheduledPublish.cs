using System.Collections.Generic;
using System.Linq;
using ScheduledPublish.Models;
using ScheduledPublish.Repos;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetContentEditorWarnings;

namespace ScheduledPublish.Pipelines.ContentEditorWarnings
{
    public class HasScheduledPublish
    {
        public void Process(GetContentEditorWarningsArgs args)
        {
            Item item = args.Item;
            Assert.IsNotNull(item, "item");

            ScheduledPublishRepo scheduledPublishRepo = new ScheduledPublishRepo();

            IEnumerable<PublishSchedule> schedulesForCurrentItem = scheduledPublishRepo.GetSchedules(item.ID);

            if (schedulesForCurrentItem.Any())
            {
                GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
                warning.Icon = "Applications/32x32/information2.png";
                warning.Text = "This item has been scheduled for publish.";
                warning.IsExclusive = false;
            }
        }
    }
}