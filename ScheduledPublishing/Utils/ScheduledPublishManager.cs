using System;
using System.Linq;
using System.Text;
using ScheduledPublishing.Models;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Publishing;

namespace ScheduledPublishing.Utils
{
    public static class ScheduledPublishManager
    {
        public static Handle Publish(PublishSchedule publishSchedule)
        {
            return publishSchedule.ItemToPublish != null
                ? PublishItem(publishSchedule)
                : PublishWebsite(publishSchedule);
        }

        public static ScheduledPublishReport GetScheduledPublishReport(Handle handle)
        {
            bool isSuccessful = false;
            StringBuilder sbMessage = new StringBuilder();

            if (handle == null)
            {
                sbMessage.Append("Final Status: Fail. <br/>");
                sbMessage.Append("Please, check log files for more information <br/>");
            }
            else if (PublishManager.WaitFor(handle))
            {
                PublishStatus status = PublishManager.GetStatus(handle);

                if (status == null)
                {
                    sbMessage.Append("The scheduled publishing process was unexpectedly interrupted. <br/>");
                    sbMessage.Append("Please, check log files for more information <br/>");
                }
                else
                {
                    if (status.Failed)
                    {
                        sbMessage.Append("Final Status: Fail. <br/>");
                        sbMessage.Append("Please, check log files for more information <br/>");
                    }
                    else if (status.IsDone)
                    {
                        sbMessage.Append("Final Status: Success. <br/>");
                        isSuccessful = true;
                    }

                    sbMessage.AppendFormat("Items processed: {0}. <br/><br/>", status.Processed);

                    if (status.Messages != null)
                    {
                        sbMessage.Append("Detailed Information: <br/>");

                        foreach (var message in status.Messages)
                        {
                            sbMessage.AppendFormat("{0} <br/>", message);
                        }
                    }
                }
            }

            ScheduledPublishReport report = new ScheduledPublishReport
            {
                IsSuccessful = isSuccessful, 
                Message = sbMessage.ToString()
            };

            return report;
        }

        private static Handle PublishItem(PublishSchedule publishSchedule)
        {
            if (publishSchedule.ItemToPublish == null)
            {
                Log.Error("Scheduled Publish: " + "Scheduled Publish Task didn't execute because PublishOptions.ItemToPublish is null", new object());
                return null;
            }

            Handle handle = null;

            try
            {
                if (publishSchedule.Unpublish)
                {
                    publishSchedule.ItemToPublish.Editing.BeginEdit();
                    publishSchedule.ItemToPublish.Publishing.NeverPublish= true;
                    publishSchedule.ItemToPublish.Editing.AcceptChanges();
                    publishSchedule.ItemToPublish.Editing.EndEdit();
                }

                handle = PublishManager.PublishItem(
                    publishSchedule.ItemToPublish,
                    publishSchedule.TargetDatabases.ToArray(),
                    publishSchedule.TargetLanguages.ToArray(),
                    publishSchedule.PublishChildren,
                    publishSchedule.PublishMode == PublishMode.Smart);

                if (publishSchedule.Unpublish)
                {
                    publishSchedule.ItemToPublish.Editing.BeginEdit();
                    publishSchedule.ItemToPublish.Publishing.NeverPublish = false;
                    publishSchedule.ItemToPublish.Editing.AcceptChanges();
                    publishSchedule.ItemToPublish.Editing.EndEdit();
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: " + "Scheduled Publish Task failed for {0} {1} {2}",
                                   publishSchedule.ItemToPublish.Name,
                                   publishSchedule.ItemToPublish.ID,
                                   ex), new object());
            }

            return handle;
        }

        private static Handle PublishWebsite(PublishSchedule publishSchedule)
        {
            Handle handle = null;

            try
            {
                switch (publishSchedule.PublishMode)
                {
                    case PublishMode.Smart:
                        {
                            handle = PublishManager.PublishSmart(
                                publishSchedule.SourceDatabase,
                                publishSchedule.TargetDatabases.ToArray(),
                                publishSchedule.TargetLanguages.ToArray());
                            break;
                        }
                    case PublishMode.Full:
                        {
                            handle = PublishManager.Republish(
                                publishSchedule.SourceDatabase,
                                publishSchedule.TargetDatabases.ToArray(),
                                publishSchedule.TargetLanguages.ToArray());
                            break;
                        }
                    case PublishMode.Incremental:
                        {
                            handle = PublishManager.PublishIncremental(
                                publishSchedule.SourceDatabase,
                                publishSchedule.TargetDatabases.ToArray(),
                                publishSchedule.TargetLanguages.ToArray());
                            break;
                        }
                    default:
                        {
                            Log.Error("Scheduled Publish: " + "Scheduled Publish Task didn't execute because invalid PublishMode", new object());
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: " + "Scheduled Publish Task failed for Website Publish in {0} Mode {1}",
                                   publishSchedule.PublishMode,
                                   ex), new object());
            }

            return handle;
        }
    }
}