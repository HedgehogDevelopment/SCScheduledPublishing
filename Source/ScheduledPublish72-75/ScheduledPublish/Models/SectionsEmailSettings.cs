using System.Collections.Generic;
using System.Linq;
using ScheduledPublish.Utils;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublish.Models
{
    /// <summary>
    /// Parses user-defined sections email settings from Sitecore into an object.
    /// </summary>
    public static class SectionsEmailSettings
    {
        private static readonly Database _database = Constants.SCHEDULED_TASK_CONTEXT_DATABASE;
        private static readonly IEnumerable<ScheduledPublishSection> _sectionItems = InnerItem.Children.Select(x => new ScheduledPublishSection(x));

        public static Item InnerItem
        {
            get { return _database.GetItem(ID.Parse("{5A61888A-D797-4582-AD3A-5FFA8AC4CF91}")); }
        }

        public static bool Enabled
        {
            get { return "1" == InnerItem[ID.Parse("{D9062D79-A51D-4F04-92ED-B7A3662FE35C}")]; }
        }

        public static IEnumerable<ScheduledPublishSection> SectionItems
        {
            get { return _sectionItems; }
        }
    }
}