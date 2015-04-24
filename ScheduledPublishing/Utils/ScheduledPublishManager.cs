using System.Linq;
using ScheduledPublishing.Models;
using Sitecore;
using Sitecore.Data;
using Sitecore.Publishing;

namespace ScheduledPublishing.Utils
{
    public class ScheduledPublishManager
    {
        private ScheduledPublishOptions ScheduledPublishOptions { get; set; }
        private Database SourceDatabase { get; set; }

        public ScheduledPublishManager(ScheduledPublishOptions scheduledPublishOptions)
        {
            this.ScheduledPublishOptions = scheduledPublishOptions;
            this.SourceDatabase = Database.GetDatabase("master");
        }

        public ScheduledPublishManager(ScheduledPublishOptions scheduledPublishOptions, Database sourceDatabase)
        {
            this.ScheduledPublishOptions = scheduledPublishOptions;
            this.SourceDatabase = sourceDatabase;
        }

        public void Publish()
        {
            if (this.ScheduledPublishOptions.PublishItems != null
                && this.ScheduledPublishOptions.PublishItems.Any())
            {
                this.PublishSelectedItems();
            }
            else
            {
                this.PublishWebsite();
            }
        }

        private void PublishSelectedItems()
        {
            foreach (var item in this.ScheduledPublishOptions.PublishItems)
            {
                Handle result = PublishManager.PublishItem(
                    item,
                    this.ScheduledPublishOptions.TargetDatabases,
                    this.ScheduledPublishOptions.Languages,
                    this.ScheduledPublishOptions.PublishChildren,
                    this.ScheduledPublishOptions.PublishMode == PublishMode.Smart);

                var test = result.ToString();
            }
        }

        private void PublishWebsite()
        {
            switch (this.ScheduledPublishOptions.PublishMode)
            {
                case PublishMode.Smart:
                    {
                        PublishManager.PublishSmart(
                            this.SourceDatabase, 
                            this.ScheduledPublishOptions.TargetDatabases,
                            this.ScheduledPublishOptions.Languages,
                            Context.Language);
                        break;
                    }

                case PublishMode.Full:
                    {
                        PublishManager.Republish(
                            this.SourceDatabase,
                            this.ScheduledPublishOptions.TargetDatabases,
                            this.ScheduledPublishOptions.Languages,
                            Context.Language);
                        break;
                    }
                case PublishMode.Incremental:
                    {
                        PublishManager.PublishIncremental(
                            this.SourceDatabase,
                            this.ScheduledPublishOptions.TargetDatabases,
                            this.ScheduledPublishOptions.Languages,
                            Context.Language);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
    }
}