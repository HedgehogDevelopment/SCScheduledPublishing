using ScheduledPublishing.Models;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheduledPublishing.Utils
{
    public static class ScheduledPublishRepository
    {
        private static readonly Database _database = Constants.SCHEDULED_TASK_CONTEXT_DATABASE;

        public static IEnumerable<PublishSchedule> AllSchedules
        {
            get
            {
                return RootFolder.Axes.GetDescendants()
                .Where(x => x.TemplateID == Constants.PUBLISH_SCHEDULE_TEMPLATE_ID)
                .Select(x => new PublishSchedule(x))
                .OrderBy(x => x.PublishDate);
            }
        }

        public static IEnumerable<PublishSchedule> AllUnpublishedSchedules
        {
            get 
            { 
                return AllSchedules.Where(x => !x.IsPublished)
                .OrderBy(x => x.PublishDate); 
            }
        }

        private static Item RootFolder
        {
            get
            {
                Item rootItem = _database.GetItem("{7D8B2A62-A35A-4DA1-B7B6-89C11758C2E6}");
                Error.AssertItemFound(rootItem);
                return rootItem;
            }
        }

        public static IEnumerable<PublishSchedule> GetSchedules(ID itemId)
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

        public static IEnumerable<PublishSchedule> GetUnpublishedSchedules(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
            {
                return Enumerable.Empty<PublishSchedule>();
            }

            return AllSchedules
                .Where(x => !x.IsPublished
                       && x.PublishDate >= fromDate
                       && x.PublishDate <= toDate);
        }

        public static void CreatePublishSchedule(PublishSchedule publishSchedule)
        {
            string action = publishSchedule.Unpublish ? "unpublish" : "publish";

            try
            {
                using (new SecurityDisabler())
                {
                    TemplateItem publishOptionsTemplate = _database.GetTemplate(Constants.PUBLISH_SCHEDULE_TEMPLATE_ID);
                    string publishOptionsName = BuildPublishScheduleName(publishSchedule.ItemToPublish);
                    Item optionsFolder = GetOrCreateFolder(publishSchedule.PublishDate);
                    Item publishOptionsItem = optionsFolder.Add(publishOptionsName, publishOptionsTemplate);

                    publishOptionsItem.Editing.BeginEdit();

                    publishOptionsItem[PublishSchedule.SchedulerEmailId] = publishSchedule.SchedulerEmail;
                    publishOptionsItem[PublishSchedule.UnpublishId] = publishSchedule.Unpublish ? "1" : string.Empty;
                    if (publishSchedule.ItemToPublish != null)
                    {
                        publishOptionsItem[PublishSchedule.ItemToPublishId] = publishSchedule.ItemToPublish.Paths.FullPath;
                    }
                    publishOptionsItem[PublishSchedule.PublishModeId] = publishSchedule.PublishMode.ToString();
                    publishOptionsItem[PublishSchedule.PublishChildrenId] = publishSchedule.PublishChildren ? "1" : string.Empty;
                    publishOptionsItem[PublishSchedule.TargetLanguagesId] = 
                        string.Join("|", publishSchedule.TargetLanguages.Select(x => x.Name));
                    publishOptionsItem[PublishSchedule.SourceDatabaseId] = publishSchedule.SourceDatabase.Name;
                    publishOptionsItem[PublishSchedule.TargetDatabasesId] = string.Join("|", publishSchedule.TargetDatabases.Select(x => x.Name));
                    publishOptionsItem[PublishSchedule.PublishDateId] = DateUtil.ToIsoDate(publishSchedule.PublishDate);

                    publishOptionsItem.Editing.AcceptChanges();
                    publishOptionsItem.Editing.EndEdit();

                    Log.Info(
                        string.Format("Scheduled Publish: Created Publish Schedule: {0}: {1} {2} {3}",
                            action,
                            publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.Name : "Website",
                            publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.ID.ToString() : "Website",
                            DateTime.Now), new object());
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: Failed creating Publish Schedule: {0}: {1} {2} {3}",
                        action,
                        publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.Name : "Website",
                        publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.ID.ToString() : "Website",
                        ex), new object());
            }
        }

        public static void UpdatePublishSchedule(PublishSchedule publishSchedule)
        {
            if (publishSchedule.InnerItem == null)
            {
                Log.Error("Scheduled Publish: Scheduled Update Failed. Item is null.", new object());
                return;
            }

            string action = publishSchedule.Unpublish ? "unpublish" : "publish";

            try
            {
                using (new SecurityDisabler())
                {
                    publishSchedule.InnerItem.Editing.BeginEdit();

                    publishSchedule.InnerItem[PublishSchedule.SchedulerEmailId] = publishSchedule.SchedulerEmail;
                    publishSchedule.InnerItem[PublishSchedule.UnpublishId] = publishSchedule.Unpublish ? "1" : string.Empty;
                    if (publishSchedule.ItemToPublish != null)
                    {
                        publishSchedule.InnerItem[PublishSchedule.ItemToPublishId] = publishSchedule.ItemToPublish.Paths.FullPath;
                    }
                    publishSchedule.InnerItem[PublishSchedule.PublishModeId] = publishSchedule.PublishMode.ToString();
                    publishSchedule.InnerItem[PublishSchedule.PublishChildrenId] = publishSchedule.PublishChildren ? "1" : string.Empty;
                    publishSchedule.InnerItem[PublishSchedule.TargetLanguagesId] = string.Join("|",
                        publishSchedule.TargetLanguages.Select(x => x.Name));
                    publishSchedule.InnerItem[PublishSchedule.SourceDatabaseId] = publishSchedule.SourceDatabase.Name;
                    publishSchedule.InnerItem[PublishSchedule.TargetDatabasesId] = string.Join("|", publishSchedule.TargetDatabases.Select(x => x.Name));
                    publishSchedule.InnerItem[PublishSchedule.IsPublishedId] = publishSchedule.IsPublished ? "1" : string.Empty;

                    DateTime oldPublishDate =
                        DateUtil.IsoDateToDateTime(publishSchedule.InnerItem[PublishSchedule.PublishDateId]);
                    publishSchedule.InnerItem[PublishSchedule.PublishDateId] = DateUtil.ToIsoDate(publishSchedule.PublishDate);

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
                            publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.Name : "Website",
                            publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.ID.ToString() : "Website",
                            DateTime.Now), new object());
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format("Scheduled Publish: Failed updating Publish Schedule: {0}: {1} {2} {3}",
                        action,
                        publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.Name : "Website",
                        publishSchedule.ItemToPublish != null ? publishSchedule.ItemToPublish.ID.ToString() : "Website",
                        ex), new object());
            }
        }

        public static void DeleteItem(Item item)
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

        public static void CleanBucket()
        {
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

        private static Item GetOrCreateFolder(DateTime date)
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

        private static Item GetDateFolder(DateTime date, BucketFolderType folderType)
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

        private static string BuildPublishScheduleName(Item item)
        {
            Guid guid = item != null
                ? item.ID.Guid
                : Guid.NewGuid();

            return ItemUtil.ProposeValidItemName(string.Format("{0}PublishSchedule", guid));
        }
    }
}