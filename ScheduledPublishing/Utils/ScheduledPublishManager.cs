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
        public static Handle Publish(ScheduledPublishOptions publishOptions)
        {
            if (!ValidatePublishOptions(publishOptions))
            {
                Log.Info("Scheduled Publish Task didn't execute because of invalid Publish Options", new object());
                return null;
            }

            return publishOptions.ItemToPublish != null
                ? PublishItem(publishOptions)
                : PublishWebsite(publishOptions);
        }

        //TODO: Should check in some interval for status.State
        public static string GetPublishReport(Handle handle)
        {
            var sbResult = new StringBuilder();

            if (handle != null)
            {
                var status = PublishManager.GetStatus(handle);

                if (status == null)
                {
                    sbResult.Append("The scheduled publishing process was unexpectedly interrupted. <br/>");
                }
                else
                {
                    if (status.Failed)
                    {
                        sbResult.Append("Final Status: Fail. <br/>");
                    }
                    else if (status.IsDone)
                    {
                        sbResult.Append("Final Status: Success. <br/>");
                    }

                    sbResult.AppendFormat("Items processed: {0}. <br/><br/>", status.Processed);

                    if (status.Messages != null)
                    {
                        sbResult.Append("Detailed Information: <br/>");

                        foreach (var message in status.Messages)
                        {
                            sbResult.AppendFormat("{0} <br/>", message);
                        }
                    }
                }
            }
            else
            {
                sbResult.Append("Final Status: Fail. <br />");
                sbResult.Append("Please, check log files for more information </br>");
            }

            return sbResult.ToString();
        }

        private static bool ValidatePublishOptions(ScheduledPublishOptions publishOptions)
        {
            if (publishOptions == null) return false;
            if (publishOptions.Languages == null || !publishOptions.Languages.Any()) return false;
            if (publishOptions.TargetDatabases == null || !publishOptions.TargetDatabases.Any()) return false;
            if (publishOptions.SourceDatabase == null) return false;
            if (publishOptions.PublishMode == PublishMode.Unknown) return false;

            return true;
        }

        private static Handle PublishItem(ScheduledPublishOptions publishOptions)
        {
            if (publishOptions.ItemToPublish == null)
            {
                Log.Info("Scheduled Publish Task didn't execute because PublishOptions.ItemToPublish is null", new object());
                return null;
            }

            Handle handle = null;

            try
            {
                if (publishOptions.Unpublish)
                {
                    publishOptions.ItemToPublish.Editing.BeginEdit();
                    publishOptions.ItemToPublish["__Never publish"] = "1";
                    publishOptions.ItemToPublish.Editing.AcceptChanges();
                    publishOptions.ItemToPublish.Editing.EndEdit();
                }

                handle = PublishManager.PublishItem(
                    publishOptions.ItemToPublish,
                    publishOptions.TargetDatabases,
                    publishOptions.Languages,
                    publishOptions.PublishChildren,
                    publishOptions.PublishMode == PublishMode.Smart);

                if (publishOptions.Unpublish)
                {
                    publishOptions.ItemToPublish.Editing.BeginEdit();
                    publishOptions.ItemToPublish["__Never publish"] = string.Empty;
                    publishOptions.ItemToPublish.Editing.AcceptChanges();
                    publishOptions.ItemToPublish.Editing.EndEdit();
                }
            }
            catch (Exception ex)
            {
                Log.Info(
                    string.Format("Scheduled Publish Task failed for {0} {1} {2}",
                                   publishOptions.ItemToPublish.Name,
                                   publishOptions.ItemToPublish.ID,
                                   ex), new object());
            }

            return handle;
        }

        private static Handle PublishWebsite(ScheduledPublishOptions publishOptions)
        {
            Handle handle = null;

            try
            {
                switch (publishOptions.PublishMode)
                {
                    case PublishMode.Smart:
                        {
                            handle = PublishManager.PublishSmart(
                                publishOptions.SourceDatabase,
                                publishOptions.TargetDatabases,
                                publishOptions.Languages);
                            break;
                        }
                    case PublishMode.Full:
                        {
                            handle = PublishManager.Republish(
                                publishOptions.SourceDatabase,
                                publishOptions.TargetDatabases,
                                publishOptions.Languages);
                            break;
                        }
                    case PublishMode.Incremental:
                        {
                            handle = PublishManager.PublishIncremental(
                                publishOptions.SourceDatabase,
                                publishOptions.TargetDatabases,
                                publishOptions.Languages);
                            break;
                        }
                    default:
                        {
                            Log.Info("Scheduled Publish Task didn't execute because invalid PublishMode", new object());
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Info(
                    string.Format("Scheduled Publish Task failed for Website Publish in {0} Mode {1}",
                                   publishOptions.PublishMode,
                                   ex), new object());
            }

            return handle;
        }
    }
}