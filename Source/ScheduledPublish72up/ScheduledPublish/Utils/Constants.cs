using Sitecore.Data;

namespace ScheduledPublish.Utils
{
    public static class Constants
    {
        public static string PUBLISH_TEXT = "publish";
        public static string UNPUBLISH_TEXT = "unpublish";
        public static string WEBSITE_PUBLISH_TEXT = "website";
        public const string SCHEDULE_UNPUBLISH_SETTINGS_TITLE = "Scheduled Unpublish Settings";
        public const string SCHEDULE_UNPUBLISH_LANGUAGES_TITLE = "Scheduled Unpublish Languages";
        public const string SCHEDULE_UNPUBLISH_TARGETS_TITLE = "Scheduled Unpublish Targets";
        public const string SCHEDULE_DATETIMEPICKER_UNPUBLISH_TITLE = "Unpiblish Time:";
        public const string CURREN_TIME_ON_SERVER_TEXT = "Current time on server: ";
        public const string NO_VALID_VERSION_TEXT = "no valid version";
        public const string NO_EXISTING_SCHEDULES_TEXT = "This item has not been scheduled for publishing yet.";

        public static readonly ID PUBLISH_SCHEDULE_TEMPLATE_ID = ID.Parse("{9F110258-0139-4FC9-AED8-5610C13DADF3}");
        public static readonly ID FOLDER_TEMPLATE_ID = ID.Parse("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}");
        public static readonly Database SCHEDULED_TASK_CONTEXT_DATABASE = Database.GetDatabase("master");
    }
}