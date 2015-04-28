using Sitecore.Data;

namespace ScheduledPublishing.Utils
{
    public static class Constants
    {
        public static readonly ID SCHEDULE_TEMPLATE_ID = ID.Parse("{70244923-FA84-477C-8CBD-62F39642C42B}");
        public static readonly ID SCHEDULE_PUBLISHING_COMMAND_ID = ID.Parse("{EF235C25-AE83-4678-9E2C-C22175925893}");
        public static readonly ID PUBLISH_OPTIONS_FOLDER_ID = ID.Parse("{7D8B2A62-A35A-4DA1-B7B6-89C11758C2E6}");
        public static readonly ID SCHEDULES_FOLDER_ID = ID.Parse("{A705D262-5714-4880-9962-051E25F1416D}");

        //Templates
        public static readonly ID PUBLISH_OPTIONS_TEMPLATE_ID = ID.Parse("{9F110258-0139-4FC9-AED8-5610C13DADF3}");
        public static readonly ID FOLDER_TEMPLATE_ID = ID.Parse("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}");

        //FieldIDs
        public static readonly ID PUBLISH_OPTIONS_SCHEDULED_DATE = ID.Parse("{9691E387-E516-450E-83EA-845AF5BA7276}");
        public static readonly ID PUBLISH_OPTIONS_SCHEDULED_TASK = ID.Parse("{EEAC5DF6-19B2-425B-84F4-466D44213108}");



        public static readonly ID SETTINGS_IS_SEND_EMAIL_CHECKED = ID.Parse("{C3CDED2B-CD39-4AD9-B361-865773A41C74}");
    }
}