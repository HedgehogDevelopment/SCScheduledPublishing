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

        //protected void StartPublisher()
        //{
        //    Language[] languages = GetLanguages().ToArray();
        //    Database[] publishingTargetDatabases = GetPublishingTargetDatabases().ToArray();
        //    bool @checked = this.PublishChildren.Checked;
        //    string id = this.InnerItem.ID.ToString();
        //    bool isIncremental = Context.ClientPage.ClientRequest.Form["PublishMode"] == "IncrementalPublish";
        //    bool isSmart = Context.ClientPage.ClientRequest.Form["PublishMode"] == "SmartPublish";

        //    this.JobHandle = (string.IsNullOrEmpty(id)
        //        ? (!isIncremental
        //            ? (!isSmart
        //                ? PublishManager.Republish(Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language)
        //                : PublishManager.PublishSmart(Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language))
        //            : PublishManager.PublishIncremental(Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language))
        //        : PublishManager.PublishItem(Client.GetItemNotNull(id), publishingTargetDatabases, languages, @checked, isSmart)).ToString();
        //}
    }
}