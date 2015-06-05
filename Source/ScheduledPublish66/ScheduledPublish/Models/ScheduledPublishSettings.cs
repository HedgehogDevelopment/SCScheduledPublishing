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
                return _database.GetItem(ID.Parse("{C1813448-7B11-4813-B0B9-FAF8A7A8F48E}")); 
            }
        }

        public static bool IsSendEmailChecked
        {
            get
            {
                return "1" == InnerItem[ID.Parse("{C3CDED2B-CD39-4AD9-B361-865773A41C74}")];
            }
        }
    }
}