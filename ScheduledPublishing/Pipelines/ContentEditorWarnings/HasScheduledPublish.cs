using ScheduledPublishing.Models;
using ScheduledPublishing.Utils;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetContentEditorWarnings;
using System.Collections.Generic;
using System.Linq;

namespace ScheduledPublishing.Pipelines.ContentEditorWarnings
{
    public class HasScheduledPublish
    {
        public void Process(GetContentEditorWarningsArgs args)
        {
            Item item = args.Item;
            Assert.IsNotNull(item, "item");
            
            IEnumerable<PublishSchedule> schedulesForCurrentItem =
                ScheduledPublishRepository.GetSchedules(item.ID);

            if (schedulesForCurrentItem.Any())
            {
                GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
                warning.Icon = "Applications/32x32/information2.png";
                warning.Text = "This item has been scheduled for publishing.";
                warning.IsExclusive = false;
            }
        }
    }
}