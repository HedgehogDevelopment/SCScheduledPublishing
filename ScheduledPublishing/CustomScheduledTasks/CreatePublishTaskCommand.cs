using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using ScheduledPublishing.Models;
using Sitecore.Configuration;
using Constants = ScheduledPublishing.Utils.Constants;

namespace ScheduledPublishing.CustomScheduledTasks
{
    public class CreatePublishTasksCommand
    {
        private const string DATE_TIME_FORMAT = "yyyyMMddTHHmmss";
        private readonly Database _database = Context.ContentDatabase;

        public void CreatePublishTasks(Item[] itemArray, CommandItem commandItem, ScheduleItem scheduledItem)
        {
            IEnumerable<Item> duePublishings = this.GetDuePublishings();
            foreach (var publishOption in duePublishings)
            {
                var scheduledTask = this.CreateTask(publishOption);
                if (scheduledTask != null)
                {
                    this.AssignTaskToScheduledPublishOptions(publishOption, scheduledTask.ID);
                }
            }
        }

        private Item CreateTask(Item item)
        {
            if (item == null)
            {
                return null;
            }

            var scheduledPublishOptions = new ScheduledPublishOptions(item);
            var action = scheduledPublishOptions.Unpublish ? "unpublish" : "publish";

            try
            {
                using (new SecurityDisabler())
                {
                    TemplateItem scheduleTaskTemplate = _database.GetTemplate(Constants.SCHEDULE_TEMPLATE_ID);
                    var publishingTaskName = BuildPublishingTaskName(item.ID);
                    Item schedulesFolder = _database.GetItem(Constants.SCHEDULES_FOLDER_ID);
                    Item newTask = schedulesFolder.Add(publishingTaskName, scheduleTaskTemplate);

                    newTask.Editing.BeginEdit();

                    newTask["Command"] = Constants.SCHEDULE_PUBLISHING_COMMAND_ID.ToString();
                    newTask["Items"] = item.Paths.FullPath;
                    newTask["Schedule"] = FormatTaskScheduledTime();
                    newTask["Last run"] = DateUtil.ToIsoDate(DateTime.MinValue);
                    newTask["Auto remove"] = "1";
                    newTask["Async"] = "1";

                    newTask.Editing.AcceptChanges();
                    newTask.Editing.EndEdit();

                    Log.Info(string.Format("Task scheduled for {0}: {1} {2} {3}",
                                            action,
                                            item.Name,
                                            item.ID,
                                            DateTime.Now), this);

                    return newTask;
                }
            }
            catch (Exception ex)
            {
                Log.Info(string.Format("Creation of scheduled task failed for {0}: {1} {2} {3}",
                                            action,
                                            item.Name,
                                            item.ID,
                                            ex), this);
                return null;
            }
        }

        private void AssignTaskToScheduledPublishOptions(Item scheduledPublishOptions, ID scheduledTaskId)
        {
            if (scheduledPublishOptions == null)
            {
                return;
            }

            try
            {
                using (new SecurityDisabler())
                {
                    scheduledPublishOptions.Editing.BeginEdit();

                    scheduledPublishOptions[Constants.PUBLISH_OPTIONS_SCHEDULED_TASK] = scheduledTaskId.ToString();

                    scheduledPublishOptions.Editing.AcceptChanges();
                    scheduledPublishOptions.Editing.EndEdit();

                    Log.Info(
                        string.Format("{0} Scheduled Task assigned to {1} Scheduled Publish Options.",
                            scheduledTaskId,
                            scheduledPublishOptions.ID), this);
                }
            }
            catch (Exception ex)
            {
                Log.Info(
                        string.Format("{0} Scheduled Task failed assigning to {1} Scheduled Publish Options. {2}",
                            scheduledTaskId,
                            scheduledPublishOptions.ID,
                            ex), this);
            }
        }

        private IEnumerable<Item> GetDuePublishings()
        {
            try
            {
                string currentTimeFolderPath = this.BuildTimeFolderPath(DateTime.Now);
                if (string.IsNullOrEmpty(currentTimeFolderPath))
                {
                    return Enumerable.Empty<Item>();
                }

                Item currentTimeFolderItem = _database.GetItem(currentTimeFolderPath);
                if (currentTimeFolderItem == null || currentTimeFolderItem.Children == null)
                {
                    return Enumerable.Empty<Item>();
                }

                return currentTimeFolderItem.Children
                                            .Where(x => string.IsNullOrEmpty(x[Constants.PUBLISH_OPTIONS_SCHEDULED_TASK]));
            }
            catch (Exception ex)
            {
                Log.Info(ex.ToString(), this);
                return Enumerable.Empty<Item>();
            }
        }

        private static string FormatTaskScheduledTime()
        {
            var scheduledStartDate = DateTime.Now;

            if (scheduledStartDate.Hour == 0 && scheduledStartDate.Minute == 0)
            {
                scheduledStartDate = scheduledStartDate.AddMinutes(1);
            }

            var scheduledEndDate = scheduledStartDate.AddDays(1);

            return string.Format("{0}|{1}|127|00:05:00",
                                  scheduledStartDate.ToString(DATE_TIME_FORMAT),
                                  scheduledEndDate.ToString(DATE_TIME_FORMAT));
        }

        private string BuildTimeFolderPath(DateTime dateTime)
        {
            Item publishOptionsFolder = _database.GetItem(Constants.PUBLISH_OPTIONS_FOLDER_ID);

            if (publishOptionsFolder == null)
            {
                return string.Empty;
            }

            return string.Format("{0}/{1}/{2}/{3}/{4}/", publishOptionsFolder.Paths.FullPath, dateTime.Year,
                dateTime.Month, dateTime.Day, dateTime.Hour);
        }

        private static string BuildPublishingTaskName(ID id)
        {
            return ItemUtil.ProposeValidItemName(string.Format("{0}ScheduledPublishTask", id));
        }
    }
}