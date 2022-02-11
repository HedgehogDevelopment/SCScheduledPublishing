using Sitecore.Data;

namespace ScheduledPublish.Utils
{
    /// <summary>
    /// Contains all global constants
    /// </summary>
    public static class Constants
    {
        #region Texts
        public const string PUBLISH_TEXT = "publish";
        public const string UNPUBLISH_TEXT = "unpublish";
        public const string WEBSITE_PUBLISH_TEXT = "website";
        public const string SCHEDULE_UNPUBLISH_SETTINGS_TITLE = "Scheduled Unpublish Settings";
        public const string SCHEDULE_UNPUBLISH_LANGUAGES_TITLE = "Scheduled Unpublish Languages";
        public const string SCHEDULE_UNPUBLISH_TARGETS_TITLE = "Scheduled Unpublish Targets";
        public const string SCHEDULE_DATETIMEPICKER_UNPUBLISH_TITLE = "Unpublish Time:";
        public const string CURREN_TIME_ON_SERVER_TEXT = "Current time on server: ";
        public const string NO_VALID_VERSION_TEXT = "no valid version";
        public const string NO_EXISTING_SCHEDULES_TEXT = "This item has not been scheduled for publishing yet.";
        public const string SCHEDULED_PUBLISH_NOTIFICATION = "This item has been scheduled for publish.";
        public const string SCHEDULED_PUBLISH_ICON = "People/16x16/clock_run.png";
        #endregion

        #region Template IDs
        public static readonly ID PUBLISH_SCHEDULE_TEMPLATE_ID = ID.Parse("{9F110258-0139-4FC9-AED8-5610C13DADF3}");
        public static readonly ID FOLDER_TEMPLATE_ID = ID.Parse("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}");
        #endregion

        #region Item IDs
        public static readonly ID PUBLISH_SCHEDULES_ROOT_ID = ID.Parse("{7D8B2A62-A35A-4DA1-B7B6-89C11758C2E6}");
        #endregion

        public static readonly Database SCHEDULED_TASK_CONTEXT_DATABASE = Database.GetDatabase("master");


        #region Settings
        public static readonly ID SETTINGS_ITEM_ID = ID.Parse("{C1813448-7B11-4813-B0B9-FAF8A7A8F48E}");
        public static readonly ID SETTINGS_SENDEMAILFIELD_ID = ID.Parse("{C3CDED2B-CD39-4AD9-B361-865773A41C74}");
        #endregion

        #region Smtp
        public static readonly ID PUBLISH_EMAIL_SETTINGS = ID.Parse("{F6E2C93B-635C-4657-92E8-3B1F02C51D34}");
        public static readonly ID PUBLISH_EMAIL_SETTINGS_USE_WEBCONFIG = ID.Parse("{5729E93E-6E14-4AD8-BB76-7803302C95C3}");
        public static readonly ID PUBLISH_EMAIL_SETTINGS_MAILSERVER = ID.Parse("{214F19D5-72A8-4332-9375-85E599A2A451}");
        public static readonly ID PUBLISH_EMAIL_SETTINGS_MAILSERVERPORT = ID.Parse("{A08B5C87-8DD1-48BC-93BE-6001210791BD}");
        public static readonly ID PUBLISH_EMAIL_SETTINGS_MAILSERVERUSERNAME = ID.Parse("{510D71A5-358A-4336-8F2E-A55A33B53F29}");
        public static readonly ID PUBLISH_EMAIL_SETTINGS_MAILSERVERPASSWORD = ID.Parse("{494BDDC5-A953-4BE8-A735-E5BC29BDCA8C}");
        #endregion
    }
}