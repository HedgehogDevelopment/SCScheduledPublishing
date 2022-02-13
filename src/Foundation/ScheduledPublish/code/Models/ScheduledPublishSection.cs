using System;
using System.Collections.Generic;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace ScheduledPublish.Models
{
    /// <summary>
    /// Parses user-defined section email settings item from Sitecore into an object.
    /// </summary>
    public class ScheduledPublishSection
    {
        private static readonly ID SectionRootItemID = ID.Parse("{42113E0D-F76D-47C9-A3A1-31EBE11854C3}");
        private static readonly ID SectionRolesID = ID.Parse("{74F33BF8-EA32-489F-A9CC-B54622222FB6}");

        private readonly Item _innerItem;
        private readonly Item _sectionRoot;

        public Item InnerItem
        {
            get { return _innerItem; }
        }

        public Item SectionRoot
        {
            get { return _sectionRoot; }
        }

        public IEnumerable<string> SectionRoleNames
        {
            get
            {
                var rolesNames = InnerItem[SectionRolesID];
                return rolesNames.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public ScheduledPublishSection(Item item)
        {
            const string rootNotFoundMessage = "Cannot find root item for section '{0}'";

            Assert.IsNotNull(item, "item");

            var sectionRootField = (InternalLinkField)item.Fields[SectionRootItemID];
            _innerItem = item;

            if (sectionRootField != null && sectionRootField.TargetItem != null)
            {
                _sectionRoot = sectionRootField.TargetItem;
            }
            else
            {
                Log.Error(string.Format(rootNotFoundMessage, item.Name), this);
            }
        }
    }
}