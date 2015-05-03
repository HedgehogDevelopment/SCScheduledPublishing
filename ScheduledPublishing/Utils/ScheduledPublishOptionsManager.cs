using System.Collections.Generic;
using System.Linq;
using ScheduledPublishing.Models;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using System;

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
                .OrderBy(t => DateUtil.IsoDateToDateTime(t.PublishDateString));
        }

        public static IEnumerable<ScheduledPublishOptions> GetScheduledOptions(Item rootFolder, ID itemId)
        {
            if (rootFolder == null || ID.IsNullOrEmpty(itemId))
            {
                return Enumerable.Empty<ScheduledPublishOptions>();
            }

            var allScheduledPublishOptions = GetAllScheduledOptions(rootFolder);

            return allScheduledPublishOptions.Where(
                t => t.ItemToPublish != null && t.ItemToPublish.ID == itemId);
        }

        public static IEnumerable<ScheduledPublishOptions> GetUnpublishedScheduledOptions(Item rootFolder)
        {
            if (rootFolder == null)
            {
                return Enumerable.Empty<ScheduledPublishOptions>();
            }

            var allScheduledPublsihOptions = GetAllScheduledOptions(rootFolder);

            return allScheduledPublsihOptions.Where(t => !t.IsPublished);
        } 

        public static IEnumerable<ScheduledPublishOptions> GetUnpublishedScheduledOptions(Item rootFolder, DateTime fromDate, DateTime toDate)
        {
            if (rootFolder == null || fromDate > toDate)
            {
                return Enumerable.Empty<ScheduledPublishOptions>();
            }

            var allScheduledPublsihOptions = GetAllScheduledOptions(rootFolder);

            return allScheduledPublsihOptions
                .Where(t => !t.IsPublished
                       && DateUtil.IsoDateToDateTime(t.PublishDateString) >= fromDate
                       && DateUtil.IsoDateToDateTime(t.PublishDateString) <= toDate);
        } 
    }
}