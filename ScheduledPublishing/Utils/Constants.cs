using Sitecore.Data;

namespace ScheduledPublishing.Utils
{
    public static class Constants
    {
        public static readonly ID SCHEDULE_TEMPLATE_ID = ID.Parse("{70244923-FA84-477C-8CBD-62F39642C42B}");
        public static readonly ID SCHEDULE_PUBLISHING_COMMAND_ID = ID.Parse("{EF235C25-AE83-4678-9E2C-C22175925893}");
        public static readonly ID PUBLISH_OPTIONS_FOLDER_ID = ID.Parse("{7D8B2A62-A35A-4DA1-B7B6-89C11758C2E6}");

        //Templates
        public static readonly ID PUBLISH_OPTIONS_TEMPLATE_ID = ID.Parse("{9F110258-0139-4FC9-AED8-5610C13DADF3}");
        public static readonly ID FOLDER_TEMPLATE_ID = ID.Parse("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}");

        //FieldIDs
        public static readonly ID PUBLISH_OPTIONS_CREATED_BY_EMAIL = ID.Parse("{0BBED214-85E7-4773-AB6A-9608CAC921FE}");
        public static readonly ID PUBLISH_OPTIONS_UNPUBLISH = ID.Parse("{0A1E6524-43BA-4F3D-B7BF-1DD696FB2953}");
        public static readonly ID PUBLISH_OPTIONS_PUBLISH_ITEM = ID.Parse("{8B07571D-D616-4373-8DB0-D77672911D16}");
        public static readonly ID PUBLISH_OPTIONS_PUBLISH_MODE = ID.Parse("{F313EF5C-AC40-46DB-9AA1-52C70D590338}");
        public static readonly ID PUBLISH_OPTIONS_PUBLISH_CHILDREN = ID.Parse("{7E70DC6C-21E1-48C2-8AEC-ACA6A4B8BCB2}");
        public static readonly ID PUBLISH_OPTIONS_SOURCE_DATABASE = ID.Parse("{61632EB9-8A59-4AAB-B790-91AF3DA7B9F4}");
        public static readonly ID PUBLISH_OPTIONS_TARGET_DATABASES = ID.Parse("{193B7E69-8C83-422F-80B2-F7B48C42775E}");
        public static readonly ID PUBLISH_OPTIONS_TARGET_LANGUAGES = ID.Parse("{65C16118-BD34-4E45-9AAD-45C7AD0AE69A}");
        public static readonly ID PUBLISH_OPTIONS_SCHEDULED_DATE = ID.Parse("{9691E387-E516-450E-83EA-845AF5BA7276}");
    }
}