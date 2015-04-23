using ScheduledPublishing.Models;

namespace ScheduledPublishing.Utils
{
    public class ScheduledPublishManager
    {
        //TODO Source, Target databases if needed
        private ScheduledPublishOptions ScheduledPublishOptions { get; set; }

        public ScheduledPublishManager(ScheduledPublishOptions options)
        {
            this.ScheduledPublishOptions = options;
        }

        public bool Publish()
        {
            //TODO Pubhish implementation
            return false;
        }
    }
}