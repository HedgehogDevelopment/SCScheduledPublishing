using Sitecore.Data;

namespace ScheduledPublishing.Utils
{
    public static class Constants
    {
        public static readonly ID PUBLISH_OPTIONS_TEMPLATE_ID = ID.Parse("{9F110258-0139-4FC9-AED8-5610C13DADF3}");
        public static readonly ID FOLDER_TEMPLATE_ID = ID.Parse("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}");
        public static readonly Database SCHEDULED_TASK_CONTEXT_DATABASE = Database.GetDatabase("master");
    }
}