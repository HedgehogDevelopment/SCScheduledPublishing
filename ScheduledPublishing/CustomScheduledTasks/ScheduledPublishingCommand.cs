using System.Net.Mail;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Publishing;
using Sitecore.Tasks;
using System;
using System.Linq;

namespace ScheduledPublishing.CustomScheduledTasks
{
    /// <summary>
    /// Publishes the item(s) passed
    /// </summary>
    public class ScheduledPublishingCommand
    {
        protected Database master;

        public void SchedulePublishingTask(Item[] itemArray, CommandItem commandItem, ScheduleItem scheduledItem)
        {
            master = Sitecore.Configuration.Factory.GetDatabase("master");
            string[] publishingTargets;

            foreach (var item in itemArray)
            {
                // if the item has Publishing targets defined, use them and publish to all of them
                if (!string.IsNullOrEmpty(item[FieldIDs.PublishingTargets]))
                {
                    publishingTargets = item[FieldIDs.PublishingTargets].Split('|');
                }
                // if the item has no Publishing targets specified, publish to all
                else
                {
                    publishingTargets = master.GetItem("/sitecore/system/Publishing targets").Children.Select(x => x.ID.ToString()).ToArray();
                }
                if (publishingTargets.Length == 0)
                {
                    Log.Info("No publishing targets found", this);
                }
                else
                {
                    bool published = PublishItemToTargets(item, publishingTargets);
                    if (published)
                    {
                        Notify("admin@png.com", scheduledItem["CreatedByEmail"]);
                    }
                }
            }
        }

        private bool PublishItemToTargets(Item item, string[] publishingTargets)
        {
            bool successful = false;
            foreach (var pbTargetId in publishingTargets)
            {
                try
                {
                    Item pbTarget = master.GetItem(new ID(pbTargetId));
                    PublishOptions publishOptions = new PublishOptions(
                        master,
                        Database.GetDatabase(pbTarget["Target database"]),
                        PublishMode.SingleItem,
                        item.Language,
                        DateTime.Now);
                    Sitecore.Publishing.Pipelines.PublishItem.PublishItemPipeline.Run(item.ID, publishOptions);

                    Log.Info(
                        "Custom publishing task complete for " + item.Name + " - " + item.ID
                        + " Database source: " + master.Name + " Database target: " +
                        Database.GetDatabase(pbTarget["Target database"]).Name, this);
                    successful = true;
                }
                catch (Exception e)
                {
                    Log.Info("Custom publishing task failed for " + item.Name + " - " + item.ID, this);
                    Log.Info(e.ToString(), this);
                    successful = false;
                }
            }
            return successful;
        }
        public void Notify(string emailFrom, string emailTo)
        {
            var smtpClient = new SmtpClient();
            var mailMessage = new MailMessage(emailFrom, emailTo)
            {
                Subject = "PNG Publishing",
                IsBodyHtml = true,
                Body = "Publishing successful"
            };

            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception e)
            {
                Log.Info("Sending email failed: " + e.ToString(), this);
            }
        }
    }
}