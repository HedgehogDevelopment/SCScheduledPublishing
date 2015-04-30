using System.Collections.Generic;
using System.Linq;
using ScheduledPublishing.Models;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublishing.Utils
{
    public static class ScheduledPublishOptionsManager
    {
        public static IEnumerable<ScheduledPublishOptions> GetAllScheduledOptions(Item rootFolder)
        {
            if (rootFolder == null)
            {
                return Enumerable.Empty<ScheduledPublishOptions>();
            }

            return rootFolder.Axes.GetDescendants()
                .Where(t => t.TemplateID == Constants.PUBLISH_OPTIONS_TEMPLATE_ID)
                .Select(t => new ScheduledPublishOptions(t))
                .OrderBy(t => t.PublishDateString);
        }

        public static IEnumerable<ScheduledPublishOptions> GetScheduledOptions(Item rootFolder, ID itemId)
        {
            if (rootFolder == null || ID.IsNullOrEmpty(itemId))
            {
                return Enumerable.Empty<ScheduledPublishOptions>();
            }

            var allScheduledPublishOptions = GetAllScheduledOptions(rootFolder);

            return allScheduledPublishOptions.Where(t => t.ItemToPublish.ID == itemId);
        } 
       
    }
}