using ScheduledPublish.Models;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace ScheduledPublish.Extensions
{
    public static class ItemExtensions
    {
        public static ScheduledPublishSection GetParentSection(this Item item)
        {
            Assert.IsNotNull(item, "item");
            return item.GetSections().OrderByDescending(x => x.SectionRoot.Paths.FullPath.Length).FirstOrDefault();
        }

        public static IEnumerable<ScheduledPublishSection> GetSections(this Item item)
        {
            Assert.IsNotNull(item, "item");

            var sectionItems = SectionsEmailSettings.SectionItems;

            if (sectionItems == null)
            {
                return Enumerable.Empty<ScheduledPublishSection>();
            }

            return sectionItems.Where(x => x.SectionRoot != null && (x.SectionRoot.Paths.IsAncestorOf(item) || x.SectionRoot.ID == item.ID));
        }
    }
}