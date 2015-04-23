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
        public static readonly ID PUBLISH_OPTIONS_UNPUBLISH = ID.Parse("{0A1E6524-43BA-4F3D-B7BF-1DD696FB2953}");
        public static readonly ID PUBLISH_OPTIONS_PUBLISH_CHILDREN = ID.Parse("{7E70DC6C-21E1-48C2-8AEC-ACA6A4B8BCB2}");
    }
}