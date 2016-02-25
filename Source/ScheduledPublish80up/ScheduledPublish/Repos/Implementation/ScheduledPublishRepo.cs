using System;
using System.Linq;
using ScheduledPublish.Models;
using ScheduledPublish.Repos.Abstraction;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Constants = ScheduledPublish.Utils.Constants;

namespace ScheduledPublish.Repos.Implementation
{
    public class ScheduledPublishRepo: SchedulesRepo<PublishSchedule>
    {
        public override void CreateSchedule(PublishSchedule schedule)
        {
            string action = schedule.Unpublish ? Constants.UNPUBLISH_TEXT : Constants.PUBLISH_TEXT;
            Item itemToPublish = schedule.Items.FirstOrDefault();

            try
            {
                using (new SecurityDisabler())
                {
                    TemplateItem publishOptionsTemplate = Database.GetTemplate(Constants.PUBLISH_SCHEDULE_TEMPLATE_ID);
                    string publishOptionsName = BuildScheduleName();
                    Item optionsFolder = GetOrCreateFolder(schedule.ScheduledDate);
                    Item publishOptionsItem = optionsFolder.Add(publishOptionsName, publishOptionsTemplate);

                    publishOptionsItem.Editing.BeginEdit();

                    publishOptionsItem[PublishSchedule.SchedulerUsernameId] = schedule.SchedulerUsername;
                    publishOptionsItem[PublishSchedule.UnpublishId] = schedule.Unpublish ? "1" : string.Empty;
                    if (itemToPublish != null)
                    {
                        publishOptionsItem[PublishSchedule.ItemsId] = itemToPublish.Paths.FullPath;
                    }
                    publishOptionsItem[PublishSchedule.PublishModeId] = schedule.PublishMode.ToString();
                    publishOptionsItem[PublishSchedule.PublishChildrenId] = schedule.PublishChildren ? "1" : string.Empty;
                    publishOptionsItem[PublishSchedule.PublishRelatedItemsId] = schedule.PublishRelatedItems ? "1" : string.Empty;
                    publishOptionsItem[PublishSchedule.TargetLanguagesId] =
                        string.Join("|", schedule.TargetLanguages.Select(x => x.Name));
                    publishOptionsItem[PublishSchedule.SourceDatabaseId] = schedule.SourceDatabase.Name;
                    publishOptionsItem[PublishSchedule.TargetDatabasesId] = string.Join("|", schedule.TargetDatabases.Select(x => x.Name));
                    publishOptionsItem[PublishSchedule.ScheduledDateId] = DateUtil.ToIsoDate(DateUtil.ToUniversalTime(schedule.ScheduledDate));
                    publishOptionsItem[PublishSchedule.RecurrenceTypeId] = schedule.RecurrenceType.ToString();
                    publishOptionsItem[PublishSchedule.HoursToNextPublishId] =
                        schedule.HoursToNextSchedule.ToString();

                    publishOptionsItem.Editing.AcceptChanges();
                    publishOptionsItem.Editing.EndEdit();

                    Log.Info(
                        string.Format("Scheduled Publish: Created Publish Schedule: {0}: {1} {2} {3}",
                            action,
                            itemToPublish != null ? itemToPublish.Name : Constants.WEBSITE_PUBLISH_TEXT,
                            itemToPublish != null ? itemToPublish.ID.ToString() : Constants.WEBSITE_PUBLISH_TEXT,
                            DateTime.Now), new object());
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: Failed creating Publish Schedule: {0}: {1} {2} {3}",
                        action,
                        itemToPublish != null ? itemToPublish.Name : Constants.WEBSITE_PUBLISH_TEXT,
                        itemToPublish != null ? itemToPublish.ID.ToString() : Constants.WEBSITE_PUBLISH_TEXT,
                        ex), new object());
            }
        }

        public override void UpdateSchedule(PublishSchedule schedule)
        {
            if (schedule.InnerItem == null)
            {
                Log.Error("Scheduled Publish: Scheduled Update Failed. Item is null.", new object());
                return;
            }

            string action = schedule.Unpublish ? Constants.UNPUBLISH_TEXT : Constants.PUBLISH_TEXT;
            Item itemToPublish = schedule.Items.FirstOrDefault();

            try
            {
                using (new SecurityDisabler())
                {
                    schedule.InnerItem.Editing.BeginEdit();

                    schedule.InnerItem[PublishSchedule.SchedulerUsernameId] = schedule.SchedulerUsername;
                    schedule.InnerItem[PublishSchedule.UnpublishId] = schedule.Unpublish ? "1" : string.Empty;
                    if (itemToPublish != null)
                    {
                        schedule.InnerItem[PublishSchedule.ItemsId] = itemToPublish.Paths.FullPath;
                    }
                    schedule.InnerItem[PublishSchedule.PublishModeId] = schedule.PublishMode.ToString();
                    schedule.InnerItem[PublishSchedule.PublishChildrenId] = schedule.PublishChildren ? "1" : string.Empty;
                    schedule.InnerItem[PublishSchedule.PublishRelatedItemsId] = schedule.PublishRelatedItems ? "1" : string.Empty;
                    schedule.InnerItem[PublishSchedule.TargetLanguagesId] = string.Join("|",
                        schedule.TargetLanguages.Select(x => x.Name));
                    schedule.InnerItem[PublishSchedule.SourceDatabaseId] = schedule.SourceDatabase.Name;
                    schedule.InnerItem[PublishSchedule.TargetDatabasesId] = string.Join("|", schedule.TargetDatabases.Select(x => x.Name));
                    schedule.InnerItem[PublishSchedule.IsExecutedId] = schedule.IsExecuted ? "1" : string.Empty;

                    DateTime oldPublishDate = DateUtil.IsoDateToDateTime(schedule.InnerItem[PublishSchedule.ScheduledDateId]);
                    schedule.InnerItem[PublishSchedule.ScheduledDateId] = DateUtil.ToIsoDate(DateUtil.ToUniversalTime(schedule.ScheduledDate));

                    schedule.InnerItem[PublishSchedule.RecurrenceTypeId] = schedule.RecurrenceType.ToString();
                    schedule.InnerItem[PublishSchedule.HoursToNextPublishId] = schedule.HoursToNextSchedule.ToString();

                    schedule.InnerItem.Editing.AcceptChanges();
                    schedule.InnerItem.Editing.EndEdit();

                    if (oldPublishDate != schedule.ScheduledDate)
                    {
                        Item newFolder = GetOrCreateFolder(schedule.ScheduledDate);
                        schedule.InnerItem.MoveTo(newFolder);
                    }

                    Log.Info(
                        string.Format("Scheduled Publish: Updated Publish Schedule: {0}: {1} {2} {3}",
                            action,
                            itemToPublish != null ? itemToPublish.Name : Constants.WEBSITE_PUBLISH_TEXT,
                            itemToPublish != null ? itemToPublish.ID.ToString() : Constants.WEBSITE_PUBLISH_TEXT,
                            DateTime.Now), new object());
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: Failed updating Publish Schedule: {0}: {1} {2} {3}",
                        action,
                        itemToPublish != null ? itemToPublish.Name : Constants.WEBSITE_PUBLISH_TEXT,
                        itemToPublish != null ? itemToPublish.ID.ToString() : Constants.WEBSITE_PUBLISH_TEXT,
                        ex), new object());
            }
        }

        protected override PublishSchedule Map(Item item)
        {
            return item == null ? null : new PublishSchedule(item);
        }

        protected override string BuildScheduleName()
        {
            return ItemUtil.ProposeValidItemName(string.Format("{0}ScheduledPublish", Guid.NewGuid()));
        }
    }
}