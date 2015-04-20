using System.Linq;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetContentEditorWarnings;

namespace ScheduledPublishing.Pipelines.ContentEditorWarnings
{
    public class HasScheduledPublishing
    {
        private const string SCHEDULES_FOLDER_PATH = "/sitecore/System/Tasks/Schedules/";

        public void Process(GetContentEditorWarningsArgs args)
        {
            Item item = args.Item;
            Assert.IsNotNull(item, "item");

            string id = item.ID.ToString().Replace("{", string.Empty).Replace("}", string.Empty);

            if (Context.ContentDatabase.GetItem(SCHEDULES_FOLDER_PATH).Children.Any(x => x.Name.StartsWith(id)))
            {
                GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
                warning.Icon = "Applications/32x32/information2.png";
                warning.Text = "This item has been scheduled for publishing.";
                warning.IsExclusive = false;
            }
        }
    }
}