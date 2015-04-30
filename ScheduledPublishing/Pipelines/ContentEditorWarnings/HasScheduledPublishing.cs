using ScheduledPublishing.Models;
using ScheduledPublishing.Utils;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetContentEditorWarnings;
using System.Collections.Generic;
using System.Linq;
using Constants = ScheduledPublishing.Utils.Constants;

namespace ScheduledPublishing.Pipelines.ContentEditorWarnings
{
    public class HasScheduledPublishing
    {
        private readonly Database _database = Utils.Utils.ScheduledTasksContextDatabase;

        public void Process(GetContentEditorWarningsArgs args)
        {
            Item publishingSchedulesFolder = _database.GetItem(Constants.PUBLISH_OPTIONS_FOLDER_ID);
            Item item = args.Item;
            Assert.IsNotNull(item, "item");
            
            IEnumerable<ScheduledPublishOptions> allScheudles =
                ScheduledPublishOptionsManager.GetScheduledOptions(publishingSchedulesFolder, item.ID);

            if (allScheudles.Any())
            {
                GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
                warning.Icon = "Applications/32x32/information2.png";
                warning.Text = "This item has been scheduled for publishing.";
                warning.IsExclusive = false;
            }
        }
    }
}