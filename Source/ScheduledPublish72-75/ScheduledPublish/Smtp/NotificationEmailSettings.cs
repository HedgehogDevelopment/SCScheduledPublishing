using System;
using ScheduledPublish.Utils;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublish.Smtp
{
    /// <summary>
    /// Parses user-defined email settings from Sitecore into an object.
    /// </summary>
    public static class NotificationEmailSettings
    {
        private static readonly Database _database = Constants.SCHEDULED_TASK_CONTEXT_DATABASE;

        public static Item InnerItem
        {
            get { return _database.GetItem(Constants.PUBLISH_EMAIL_SETTINGS); }
        }

        public static bool UseWebConfig
        {
            get { return InnerItem[Constants.PUBLISH_EMAIL_SETTINGS_USE_WEBCONFIG] == "1"; }
        }
        
        public static string MailServer
        {
            get { return InnerItem[Constants.PUBLISH_EMAIL_SETTINGS_MAILSERVER]; }
        }

        public static Int32 Port
        {
            get { return Convert.ToInt32(InnerItem[Constants.PUBLISH_EMAIL_SETTINGS_MAILSERVERPORT]); }
        }

        public static string Username
        {
            get { return InnerItem[Constants.PUBLISH_EMAIL_SETTINGS_MAILSERVERUSERNAME]; }
        }

        public static string Password
        {
            get { return InnerItem[Constants.PUBLISH_EMAIL_SETTINGS_MAILSERVERPASSWORD]; }
        }
    }
}