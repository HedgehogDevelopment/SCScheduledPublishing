using System;
using System.Collections.Generic;
using System.Linq;
using ScheduledPublish.Models;
using ScheduledPublish.Recurrence.Implementation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Constants = ScheduledPublish.Utils.Constants;

namespace ScheduledPublish.Repos.Abstraction
{
    public abstract class SchedulesRepo<T> : ISchedulesRepo<T>
        where T : CommandSchedule
    {
        public abstract void CreateSchedule(T schedule);

        public void DeleteSchedule(T schedule)
        {
            if (schedule == null || schedule.InnerItem == null)
            {
                return;
            }

            DeleteItem(schedule.InnerItem);
        }

        public abstract void UpdateSchedule(T schedule);

        public IEnumerable<T> GetSchedules()
        {
            return RootFolder == null
                ? Enumerable.Empty<T>()
                : RootFolder.Axes.GetDescendants()
                    .Where(x => x.TemplateID == Constants.PUBLISH_SCHEDULE_TEMPLATE_ID)
                    .Select(Map)
                    .OrderBy(x => x.ScheduledDate);
        }

        public IEnumerable<T> GetSchedules(ID itemId)
        {
            return GetSchedules().Where(
                x => x.Items.Any()
                    && x.Items.First().ID == itemId
                    && !x.IsExecuted);
        }

        public IEnumerable<T> GetUnexecutedSchedules()
        {
            return GetSchedules().Where(x => x.Items.Any() && !x.IsExecuted)
                .OrderBy(x => x.ScheduledDate);
        }

        public IEnumerable<T> GetUnexecutedSchedules(DateTime from, DateTime to)
        {
            if (from > to)
            {
                return Enumerable.Empty<T>();
            }

            return GetUnexecutedSchedules()
                .Where(x => x.Items.Any()
                       && !x.IsExecuted
                       && x.ScheduledDate >= from
                       && x.ScheduledDate <= to);
        }

        public IEnumerable<T> GetRecurringSchedules()
        {
            return GetSchedules()
                .Where(x => x.Items.Any()
                        && x.IsExecuted
                        && x.RecurrenceType != RecurrenceType.None);
        }

        public IEnumerable<T> GetRecurringSchedules(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
            {
                return Enumerable.Empty<T>();
            }

            return GetRecurringSchedules()
                .Where(x => x.ScheduledDate >= fromDate
                        && x.ScheduledDate <= toDate);
        }

        public void CleanRepo()
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

        protected abstract T Map(Item item);

        protected abstract string BuildScheduleName();

        protected Item GetOrCreateFolder(DateTime date)
        {
            string yearName = date.Year.ToString();
            string monthName = date.Month.ToString();
            string dayName = date.Day.ToString();
            string hourName = date.Hour.ToString();

            TemplateItem folderTemplate = Database.GetTemplate(Constants.FOLDER_TEMPLATE_ID);
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

        protected Database Database
        {
            get
            {
                return Database.GetDatabase(Constants.SCHEDULED_REPO_CONTEXT_DATABASE);
            }
        }

        private Item RootFolder
        {
            get
            {
                Item rootItem = Database.GetItem(Constants.PUBLISH_SCHEDULES_ROOT_ID);

                if (rootItem == null)
                {
                    Log.Error("Cannot find SchduledPublish root item!", this);
                }

                return rootItem;
            }
        }

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

            return Database.GetItem(itemPath);
        }

        private static void DeleteItem(Item item)
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
    }
}