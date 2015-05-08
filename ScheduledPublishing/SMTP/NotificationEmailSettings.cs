using System;
using ScheduledPublishing.Utils;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublishing.SMTP
{
    public static class NotificationEmailSettings
    {
        private static readonly Database _database = Constants.SCHEDULED_TASK_CONTEXT_DATABASE;

        public static Item InnerItem
        {
            get { return _database.GetItem(ID.Parse("{F6E2C93B-635C-4657-92E8-3B1F02C51D34}")); }
        }

        public static bool UseWebConfig
        {
            get { return InnerItem[ID.Parse("{5729E93E-6E14-4AD8-BB76-7803302C95C3}")] == "1"; }
        }
        
        public static string MailServer
        {
            get { return InnerItem[ID.Parse("{214F19D5-72A8-4332-9375-85E599A2A451}")]; }
        }

        public static Int32 Port
        {
            get { return Convert.ToInt32(InnerItem[ID.Parse("{A08B5C87-8DD1-48BC-93BE-6001210791BD}")]); }
        }

        public static string Username
        {
            get { return InnerItem[ID.Parse("{510D71A5-358A-4336-8F2E-A55A33B53F29}")]; }
        }

        public static string Password
        {
            get { return InnerItem[ID.Parse("{494BDDC5-A953-4BE8-A735-E5BC29BDCA8C}")]; }
        }
    }
}