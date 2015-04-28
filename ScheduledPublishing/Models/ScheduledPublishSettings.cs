using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublishing.Models
{
    public static class ScheduledPublishSettings
    {
        public static Item InnerItem
        {
            get { return Sitecore.Context.ContentDatabase.GetItem(ID.Parse("{C1813448-7B11-4813-B0B9-FAF8A7A8F48E}")); }
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