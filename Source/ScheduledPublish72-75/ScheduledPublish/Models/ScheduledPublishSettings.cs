using ScheduledPublish.Utils;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublish.Models
{
    /// <summary>
    /// Parses the main Scheduled Publish Settings item from Sitecore into an object.
    /// </summary>
    public static class ScheduledPublishSettings
    {
        private static readonly Database _database = Constants.SCHEDULED_TASK_CONTEXT_DATABASE;

        public static Item InnerItem
        {
            get
            {
                return _database.GetItem(Constants.SETTINGS_ITEM_ID); 
            }
        }

        public static bool IsSendEmailChecked
        {
            get
            {
                return "1" == InnerItem[ID.Parse(Constants.SETTINGS_SENDEMAILFIELD_ID)];
            }
        }
    }
}