using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheduledPublishing.CustomScheduledTasks
{
    public class CreatePublishTasksCommand
    {
        protected Database _database;

        public void CreatePublishTasks(Item[] itemArray, CommandItem commandItem, ScheduleItem scheduledItem)
        {
            _database = Sitecore.Configuration.Factory.GetDatabase("master");
            IEnumerable<Item> duePublishings = GetDuePublishings();
            foreach (var publishOption in duePublishings)
            {
                CreateTask(publishOption);
            }
        }

        private void CreateTask(Item item)
        {
            try
            {
                using (new SecurityDisabler())
                {
                    Item creatingFor = _database.GetItem(item["Item"]);
                    TemplateItem scheduleTaskTemplate = _database.GetTemplate(Utils.Constants.SCHEDULE_TEMPLATE_ID);
                    var publishingTaskName = BuildPublishingTaskName(item.ID);
                    Item schedulesFolder = _database.GetItem(Utils.Constants.PUBLISH_OPTIONS_FOLDER_ID);
                    Item newTask = schedulesFolder.Add(publishingTaskName, scheduleTaskTemplate);

                    newTask.Editing.BeginEdit();

                    newTask["Command"] = Utils.Constants.SCHEDULE_PUBLISHING_COMMAND_ID.ToString();
                    newTask["Items"] = creatingFor.Paths.FullPath;
                    newTask["Schedule"] = FormatTaskScheduledTime();
                    newTask["Last run"] = DateUtil.IsoDateToDateTime(DateTime.Now.ToString(), DateTime.MinValue).ToString();
                    newTask["Auto remove"] = 1.ToString();

                    newTask.Editing.AcceptChanges();
                    newTask.Editing.EndEdit();

                    string action = string.IsNullOrEmpty(item["Unpiblishing"]) ? "publishing" : "unpublishing";
                    Log.Info(
                        "Task scheduling " + action + ": " + creatingFor.Name + " " + creatingFor.ID +
                        DateTime.Now, this);
                }
            }
            catch (Exception e)
            {
                string action = string.IsNullOrEmpty(item["Unpiblishing"]) ? "publishing" : "unpublishing";
                Log.Info(
                    "Failed scheduling " + action + ": " + item.Name + " " + item.ID +
                    DateTime.Now + " " + e.ToString(), this);
            }
        }
        
        private IEnumerable<Item> GetDuePublishings()
        {
            try
            {
                string currentTimeFolderPath = BuildTimeFolderPath(DateTime.Now);
                IEnumerable<Item> currentForScheduling = _database.GetItem(currentTimeFolderPath).Children;
                return currentForScheduling;
            }
            catch (Exception e)
            {
                Log.Info(e.ToString(), this);
            }

            return Enumerable.Empty<Item>();
        }

        private string FormatTaskScheduledTime()
        {
            const string format = "yyyyMMddTHHmmss";

            return string.Format("{0}|{1}|127|00:60:00",
                                  DateTime.Now.AddHours(1).ToString(format),
                                  DateTime.Now.AddHours(2).AddMinutes(1).ToString(format));
        }

        private string BuildTimeFolderPath(DateTime dateTime)
        {
            Item publishOptionsFolder = _database.GetItem(Utils.Constants.PUBLISH_OPTIONS_FOLDER_ID);
            return string.Format("{0}/{1}/{2}/{3}/{4}/", publishOptionsFolder.Paths.FullPath, dateTime.Year,
                dateTime.Month, dateTime.Day, dateTime.Hour);
        }

        private static string BuildPublishingTaskName(ID id)
        {
            return ItemUtil.ProposeValidItemName(string.Format("{0}_Task", id));
        }
    }
}