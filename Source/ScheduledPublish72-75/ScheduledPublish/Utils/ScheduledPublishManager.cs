using System.Threading;
using ScheduledPublish.Models;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Publishing;
using System;
using System.Linq;
using System.Text;
using Sitecore.SecurityModel;

namespace ScheduledPublish.Utils
{
    /// <summary>
    /// Handles publish-related actions.
    /// </summary>
    public static class ScheduledPublishManager
    {
        /// <summary>
        /// Handles the publish action.
        /// </summary>
        /// <param name="publishSchedule"></param>
        /// <returns>A <see cref="T:Sitecore.Handle"/> publish handle.</returns>
        public static Handle Publish(PublishSchedule publishSchedule)
        {
            return publishSchedule.ItemToPublish != null
                ? PublishItem(publishSchedule)
                : PublishWebsite(publishSchedule);
        }

        /// <summary>
        /// Gets a report on the handle.
        /// </summary>
        /// <param name="handle">Handle.</param>
        /// <returns>A <see cref="T:ScheduledPublish.Models.ScheduledPublishReport"/> report on the handle result.</returns>
        public static ScheduledPublishReport GetScheduledPublishReport(Handle handle)
        {
            bool isSuccessful = false;
            StringBuilder sbMessage = new StringBuilder();

            if (handle == null)
            {
                sbMessage.Append("Final Status: Fail.<br/>");
                sbMessage.Append("Please, check log files for more information.<br/>");
            }
            else
            {
                PublishStatus status = PublishManager.GetStatus(handle);
                
                if (status == null)
                {
                    sbMessage.Append("The scheduled publishing process was unexpectedly interrupted.<br/>");
                    sbMessage.Append("Please, check log files for more information.<br/>");
                }
                else
                {
                    if (status.Failed)
                    {
                        sbMessage.Append("Final Status: Fail.<br/>");
                        sbMessage.Append("Please, check log files for more information.<br/>");
                    }
                    else if (status.IsDone)
                    {
                        sbMessage.Append("Final Status: Success.<br/>");
                        isSuccessful = true;
                    }

                    sbMessage.AppendFormat("Items processed: {0}.<br/><br/>", status.Processed);

                    if (status.Messages != null)
                    {
                        sbMessage.Append("Detailed Information:<br/>");

                        foreach (var message in status.Messages)
                        {
                            sbMessage.Append(message);
                            sbMessage.Append("<br/>");
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

        /// <summary>
        /// Handles item publish.
        /// </summary>
        /// <param name="publishSchedule">A <see cref="ScheduledPublish.Models.PublishSchedule"/> schedule to publish.</param>
        /// <returns>A <see cref="T:Sitecore.Handle"/> publish handle.</returns>
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
                    using (new SecurityDisabler())
                    {
                        publishSchedule.ItemToPublish.Editing.BeginEdit();
                        publishSchedule.ItemToPublish.Publishing.NeverPublish = true;
                        publishSchedule.ItemToPublish.Editing.AcceptChanges();
                        publishSchedule.ItemToPublish.Editing.EndEdit();
                    }
                }

                handle = PublishManager.PublishItem(
                    publishSchedule.ItemToPublish,
                    publishSchedule.TargetDatabases.ToArray(),
                    publishSchedule.TargetLanguages.ToArray(),
                    publishSchedule.PublishChildren,
                    publishSchedule.PublishMode == PublishMode.Smart,
                    publishSchedule.PublishRelatedItems);

                WaitPublish(handle);

                if (publishSchedule.Unpublish)
                {
                    using (new SecurityDisabler())
                    {
                        publishSchedule.ItemToPublish.Editing.BeginEdit();
                        publishSchedule.ItemToPublish.Publishing.NeverPublish = false;
                        publishSchedule.ItemToPublish.Editing.AcceptChanges();
                        publishSchedule.ItemToPublish.Editing.EndEdit();
                    }
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

        /// <summary>
        /// Handles website publish.
        /// </summary>
        /// <param name="publishSchedule">A <see cref="ScheduledPublish.Models.PublishSchedule"/> schedule to publish.</param>
        /// <returns>A <see cref="T:Sitecore.Handle"/> publish handle.</returns>
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

                WaitPublish(handle);
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

        /// <summary>
        /// Waits publish to finish to we know the final status
        /// </summary>
        /// <param name="handle"></param>
        private static void WaitPublish(Handle handle)
        {
            if (handle == null)
            {
                return;
            }

            while (!PublishManager.GetStatus(handle).IsDone)
            {
                Thread.Sleep(200);
            }
        }
    }
}