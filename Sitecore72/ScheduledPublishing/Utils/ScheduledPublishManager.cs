using ScheduledPublishing.Models;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Publishing;
using System;
using System.Linq;
using System.Text;

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
                sbMessage.AppendLine("Final Status: Fail.");
                sbMessage.AppendLine("Please, check log files for more information.");
            }
            else if (PublishManager.WaitFor(handle))
            {
                PublishStatus status = PublishManager.GetStatus(handle);

                if (status == null)
                {
                    sbMessage.AppendLine("The scheduled publishing process was unexpectedly interrupted.");
                    sbMessage.AppendLine("Please, check log files for more information.");
                }
                else
                {
                    if (status.Failed)
                    {
                        sbMessage.AppendLine("Final Status: Fail.");
                        sbMessage.AppendLine("Please, check log files for more information.");
                    }
                    else if (status.IsDone)
                    {
                        sbMessage.AppendLine("Final Status: Success.");
                        isSuccessful = true;
                    }

                    sbMessage.AppendFormat("Items processed: {0}.", status.Processed);
                    sbMessage.AppendLine();
                    sbMessage.AppendLine();

                    if (status.Messages != null)
                    {
                        sbMessage.AppendLine("Detailed Information:");

                        foreach (var message in status.Messages)
                        {
                            sbMessage.AppendLine(message);
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
                Log.Error("Scheduled Publish: Scheduled Publish Task didn't execute because PublishSchedule.ItemToPublish is null", new object());
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
                    publishSchedule.PublishMode == PublishMode.Smart,
                    publishSchedule.PublishRelatedItems);

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
                    string.Format("Scheduled Publish: Scheduled Publish Task failed for {0} {1} {2}",
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
                            Log.Error("Scheduled Publish: Scheduled Publish Task didn't execute because invalid PublishMode", new object());
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: Scheduled Publish Task failed for Website Publish in {0} Mode {1}",
                                   publishSchedule.PublishMode,
                                   ex), new object());
            }

            return handle;
        }
    }
}