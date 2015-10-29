using ScheduledPublish.Models;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Constants = ScheduledPublish.Utils.Constants;

namespace ScheduledPublish.Repos
{
    /// <summary>
    /// Handles publish schedules operations.
    /// </summary>
    public class ScheduledPublishRepo
    {
        private static readonly Database _database = Constants.SCHEDULED_TASK_CONTEXT_DATABASE;

        /// <summary>
        /// List of all schedules.
        /// </summary>
        public IEnumerable<PublishSchedule> AllSchedules
        {
            get
            {
                return RootFolder == null
                    ? Enumerable.Empty<PublishSchedule>()
                    : RootFolder.Axes.GetDescendants()
                        .Where(x => x.TemplateID == Constants.PUBLISH_SCHEDULE_TEMPLATE_ID)
                        .Select(x => new PublishSchedule(x))
                        .OrderBy(x => x.PublishDate);
            }
        }

        /// <summary>
        /// List of valid unpublished schedules.
        /// </summary>
        public IEnumerable<PublishSchedule> AllUnpublishedSchedules
        {
            get 
            { 
                return AllSchedules.Where(x => x.ItemToPublish != null && !x.IsPublished)
                .OrderBy(x => x.PublishDate); 
            }
        }

        /// <summary>
        /// Root folder of the Scheudled Publish Module.
        /// </summary>
        private Item RootFolder
        {
            get
            {
                Item rootItem = _database.GetItem(Constants.PUBLISH_SCHEDULES_ROOT_ID);

                if (rootItem == null)
                {
                    Log.Error("Cannot find SchduledPublish root item!", this);
                }

                return rootItem;
            }
        }

        /// <summary>
        /// All valid schedules on an item.
        /// </summary>
        /// <param name="itemId">The id of the item to browse schedules for.</param>
        /// <returns>All valid publish schedules on the item.</returns>
        public IEnumerable<PublishSchedule> GetSchedules(ID itemId)
        {
            if (ID.IsNullOrEmpty(itemId))
            {
                return Enumerable.Empty<PublishSchedule>();
            }

            return AllSchedules.Where(
                x => x.ItemToPublish != null
                    && x.ItemToPublish.ID == itemId
                    && !x.IsPublished);
        }

        /// <summary>
        /// List of valid unpublished schedules in a period of time.
        /// </summary>
        /// <param name="fromDate">From <see cref="T:System.Datetime"/> date.</param>
        /// <param name="toDate">To <see cref="T:System.Datetime"/> date.</param>
        /// <returns>List of valid unpublished schedules in this period of time.</returns>
        public IEnumerable<PublishSchedule> GetUnpublishedSchedules(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
            {
                return Enumerable.Empty<PublishSchedule>();
            }

            return AllSchedules
                .Where(x => x.ItemToPublish != null
                       && !x.IsPublished
                       && x.PublishDate >= fromDate
                       && x.PublishDate <= toDate);
        }

        /// <summary>
        /// Creates a <see cref="T:ScheduledPublish.Models.PublishSchedule"/> publish schedule item in Sitecore.
        /// </summary>
        /// <param name="publishSchedule">A <see cref="T:ScheduledPublish.Models.PublishSchedule"/> publish schedule to create an item for.</param>
        public void CreatePublishSchedule(PublishSchedule publishSchedule)
        {
            string action = publishSchedule.Unpublish ? Constants.UNPUBLISH_TEXT : Constants.PUBLISH_TEXT;

            try
            {
                using (new SecurityDisabler())
                {
                    TemplateItem publishOptionsTemplate = _database.GetTemplate(Constants.PUBLISH_SCHEDULE_TEMPLATE_ID);
                    string publishOptionsName = BuildPublishScheduleName(publishSchedule.ItemToPublish);
                    Item optionsFolder = GetOrCreateFolder(publishSchedule.PublishDate);
                    Item publishOptionsItem = optionsFolder.Add(publishOptionsName, publishOptionsTemplate);

                    publishOptionsItem.Editing.BeginEdit();

                    publishOptionsItem[PublishSchedule.SchedulerUsernameId] = publishSchedule.SchedulerUsername;
                    publishOptionsItem[PublishSchedule.UnpublishId] = publishSchedule.Unpublish ? "1" : string.Empty;
                    if (publishSchedule.ItemToPublish != null)
                    {
                        publishOptionsItem[PublishSchedule.ItemToPublishId] = publishSchedule.ItemToPublish.Paths.FullPath;
                    }
                    publishOptionsItem[PublishSchedule.PublishModeId] = publishSchedule.PublishMode.ToString();
                    publishOptionsItem[PublishSchedule.PublishChildrenId] = publishSchedule.PublishChildren ? "1" : string.Empty;
                    publishOptionsItem[PublishSchedule.PublishRelatedItemsId] = publishSchedule.PublishRelatedItems ? "1" : string.Empty;
                    publishOptionsItem[PublishSchedule.TargetLanguagesId] = 
                        string.Join("|", publishSchedule.TargetLanguages.Select(x => x.Name));
                    publishOptionsItem[PublishSchedule.SourceDatabaseId] = publishSchedule.SourceDatabase.Name;
                    publishOptionsItem[PublishSchedule.TargetDatabasesId] = string.Join("|", publishSchedule.TargetDatabases.Select(x => x.Name));
                    publishOptionsItem[PublishSchedule.PublishDateId] = DateUtil.ToIsoDate(DateUtil.ToUniversalTime(publishSchedule.PublishDate));

                    publishOptionsItem.Editing.AcceptChanges();
                    publishOptionsItem.Editing.EndEdit();

                    Log.Info(
                        string.Format("Scheduled Publish: Created Publish Schedule: {0}: {1} {2} {3}",
                            action,
                            publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.Name : Constants.WEBSITE_PUBLISH_TEXT,
                            publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.ID.ToString() : Constants.WEBSITE_PUBLISH_TEXT,
                            DateTime.Now), new object());
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: Failed creating Publish Schedule: {0}: {1} {2} {3}",
                        action,
                        publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.Name : Constants.WEBSITE_PUBLISH_TEXT,
                        publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.ID.ToString() : Constants.WEBSITE_PUBLISH_TEXT,
                        ex), new object());
            }
        }

        /// <summary>
        /// Modifies an existing <see cref="T:ScheduledPublish.Models.PublishSchedule"/> publish schedule item in Sitecore.
        /// </summary>
        /// <param name="publishSchedule">A <see cref="T:ScheduledPublish.Models.PublishSchedule"/> publish schedule according to which to modify the Sitecore item.</param>
        public void UpdatePublishSchedule(PublishSchedule publishSchedule)
        {
            if (publishSchedule.InnerItem == null)
            {
                Log.Error("Scheduled Publish: Scheduled Update Failed. Item is null.", new object());
                return;
            }

            string action = publishSchedule.Unpublish ? Constants.UNPUBLISH_TEXT : Constants.PUBLISH_TEXT;

            try
            {
                using (new SecurityDisabler())
                {
                    publishSchedule.InnerItem.Editing.BeginEdit();

                    publishSchedule.InnerItem[PublishSchedule.SchedulerUsernameId] = publishSchedule.SchedulerUsername;
                    publishSchedule.InnerItem[PublishSchedule.UnpublishId] = publishSchedule.Unpublish ? "1" : string.Empty;
                    if (publishSchedule.ItemToPublish != null)
                    {
                        publishSchedule.InnerItem[PublishSchedule.ItemToPublishId] = publishSchedule.ItemToPublish.Paths.FullPath;
                    }
                    publishSchedule.InnerItem[PublishSchedule.PublishModeId] = publishSchedule.PublishMode.ToString();
                    publishSchedule.InnerItem[PublishSchedule.PublishChildrenId] = publishSchedule.PublishChildren ? "1" : string.Empty;
                    publishSchedule.InnerItem[PublishSchedule.PublishRelatedItemsId] = publishSchedule.PublishRelatedItems ? "1" : string.Empty;
                    publishSchedule.InnerItem[PublishSchedule.TargetLanguagesId] = string.Join("|",
                        publishSchedule.TargetLanguages.Select(x => x.Name));
                    publishSchedule.InnerItem[PublishSchedule.SourceDatabaseId] = publishSchedule.SourceDatabase.Name;
                    publishSchedule.InnerItem[PublishSchedule.TargetDatabasesId] = string.Join("|", publishSchedule.TargetDatabases.Select(x => x.Name));
                    publishSchedule.InnerItem[PublishSchedule.IsPublishedId] = publishSchedule.IsPublished ? "1" : string.Empty;

                    DateTime oldPublishDate = DateUtil.IsoDateToDateTime(publishSchedule.InnerItem[PublishSchedule.PublishDateId]);
                    publishSchedule.InnerItem[PublishSchedule.PublishDateId] = DateUtil.ToIsoDate(DateUtil.ToUniversalTime(publishSchedule.PublishDate));

                    publishSchedule.InnerItem.Editing.AcceptChanges();
                    publishSchedule.InnerItem.Editing.EndEdit();

                    if (oldPublishDate != publishSchedule.PublishDate)
                    {
                        Item newFolder = GetOrCreateFolder(publishSchedule.PublishDate);
                        publishSchedule.InnerItem.MoveTo(newFolder);
                    }

                    Log.Info(
                        string.Format("Scheduled Publish: Updated Publish Schedule: {0}: {1} {2} {3}",
                            action,
                            publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.Name : Constants.WEBSITE_PUBLISH_TEXT,
                            publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.ID.ToString() : Constants.WEBSITE_PUBLISH_TEXT,
                            DateTime.Now), new object());
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: Failed updating Publish Schedule: {0}: {1} {2} {3}",
                        action,
                        publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.Name : Constants.WEBSITE_PUBLISH_TEXT,
                        publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.ID.ToString() : Constants.WEBSITE_PUBLISH_TEXT,
                        ex), new object());
            }
        }

        /// <summary>
        /// Moves to recycle bin or deletes a Sitecore item.
        /// </summary>
        /// <param name="item">Sitecore item to delete.</param>
        public void DeleteItem(Item item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                using (new SecurityDisabler())
                {
                    if (Settings.RecycleBinActive)
                    {
                        item.Recycle();
                    }
                    else
                    {
                        item.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Scheduled Publish: Failed delete item {0} {1} {2}",
                    item.Paths.FullPath,
                    item.ID,
                    ex), new object());
            }
        }

        /// <summary>
        /// Deletes all schedules and bucket folders 
        /// </summary>
        public void CleanBucket()
        {
            if (RootFolder == null)
            {
                Log.Error("Running CleanBucket failed, because the module's root item is missing from Sitecore!", this);
                return;
            }

            DateTime currentTime = DateTime.Now;
            DateTime oneDayEarlier = currentTime.AddDays(-1);

            if (oneDayEarlier.Year != currentTime.Year)
            {
                Item yearFolder = GetDateFolder(oneDayEarlier, BucketFolderType.Year);
                if (yearFolder != null)
                {
                    DeleteItem(yearFolder);
                }
            }
            else if (oneDayEarlier.Month != currentTime.Month)
            {
                Item monthFolder = GetDateFolder(oneDayEarlier, BucketFolderType.Month);
                if (monthFolder != null)
                {
                    DeleteItem(monthFolder);
                }
            }

            Item dayFolder = GetDateFolder(oneDayEarlier, BucketFolderType.Day);
            if (dayFolder != null)
            {
                DeleteItem(dayFolder);
            }

            //clean older folders, if there are any left under special circumstances
            DateTime lastMonth = currentTime.AddMonths(-1);
            var lastMonthFolder = GetDateFolder(lastMonth, BucketFolderType.Month);
            if (lastMonthFolder != null)
            {
                DeleteItem(lastMonthFolder);
            }

            DateTime lastYear = currentTime.AddYears(-1);
            var lastYearFolder = GetDateFolder(lastYear, BucketFolderType.Year);
            if (lastYearFolder != null)
            {
                DeleteItem(lastYearFolder);
            }
        }

        /// <summary>
        /// Gets or create folder in schedules bucket
        /// </summary>
        /// <param name="date">Date which is used for parsing the folder path</param>
        /// <returns>Created folder</returns>
        private Item GetOrCreateFolder(DateTime date)
        {
            string yearName = date.Year.ToString();
            string monthName = date.Month.ToString();
            string dayName = date.Day.ToString();
            string hourName = date.Hour.ToString();

            TemplateItem folderTemplate = _database.GetTemplate(Constants.FOLDER_TEMPLATE_ID);
            Item yearFolder = RootFolder.Children.FirstOrDefault(x => x.Name == yearName) ??
                              RootFolder.Add(yearName, folderTemplate);

            Item monthFolder = yearFolder.Children.FirstOrDefault(x => x.Name == monthName) ??
                               yearFolder.Add(monthName, folderTemplate);

            Item dayFolder = monthFolder.Children.FirstOrDefault(x => x.Name == dayName) ??
                             monthFolder.Add(dayName, folderTemplate);

            Item hourFolder = dayFolder.Children.FirstOrDefault(x => x.Name == hourName) ??
                              dayFolder.Add(hourName, folderTemplate);

            return hourFolder;
        }

        /// <summary>
        /// Gets date folder corresponding to the path created from the passed date
        /// </summary>
        /// <param name="date"></param>
        /// <param name="folderType">Date which is used for parsing the folder path</param>
        /// <returns>Found folder</returns>
        private Item GetDateFolder(DateTime date, BucketFolderType folderType)
        {
            string rootPath = RootFolder.Paths.FullPath;
            string yearName = date.Year.ToString();
            string monthName = date.Month.ToString();
            string dayName = date.Day.ToString();
            string hourName = date.Hour.ToString();

            string itemPath = string.Empty;

            switch (folderType)
            {
                case BucketFolderType.Year:
                    {
                        itemPath = string.Format("{0}/{1}/", rootPath, yearName);
                        break;
                    }
                case BucketFolderType.Month:
                    {
                        itemPath = string.Format("{0}/{1}/{2}/", rootPath, yearName, monthName);
                        break;
                    }
                case BucketFolderType.Day:
                    {
                        itemPath = string.Format("{0}/{1}/{2}/{3}/", rootPath, yearName, monthName, dayName);
                        break;
                    }
                case BucketFolderType.Hour:
                    {
                        itemPath = string.Format("{0}/{1}/{2}/{3}/{4}/", rootPath, yearName, monthName, dayName, hourName);
                        break;
                    }
            }

            return _database.GetItem(itemPath);
        }

        /// <summary>
        /// Builds the name of the schedule in sitecore
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private string BuildPublishScheduleName(Item item)
        {
            Guid guid = item != null
                ? item.ID.Guid
                : Guid.NewGuid();

            return ItemUtil.ProposeValidItemName(string.Format("{0}PublishSchedule", guid));
        }
    }
}