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
    public class CreatePublishTaskCommand
    {
        protected Database _database;

        public void CreatePublishTask(Item[] itemArray, CommandItem commandItem, ScheduleItem scheduledItem)
        {
            _database = Sitecore.Configuration.Factory.GetDatabase("master");
            IEnumerable<Item> itemsToPublish = GetItemsToPublish();
            foreach (var item in itemsToPublish)
            {
                CreateTask(item);
            }
        }

        private void CreateTask(Item item)
        {
            try
            {
                using (new SecurityDisabler())
                {
                    Item creatingFor = _database.GetItem(item["Item"]);
                    TemplateItem scheduleTaskTemplate = _database.GetTemplate(new ID(Utils.Constants.SCHEDULE_TEMPLATE_ID));
                    var publishingTaskName = BuildPublishingTaskName(item.ID);
                    Item schedulesFolder = _database.GetItem(Utils.Constants.PUBLISHING_SCHEDULES_PATH);
                    Item newTask = schedulesFolder.Add(publishingTaskName, scheduleTaskTemplate);
                    newTask.Editing.BeginEdit();
                    newTask["Command"] = Utils.Constants.SCHEDULE_PUBLISHING_COMMAND_ID;
                    newTask["Items"] = creatingFor.Paths.FullPath;

                    string format = "yyyyMMddTHHmmss";
                    newTask["Schedule"] =
                        (DateTime.Now.AddHours(1).AddMinutes(-1)).ToString(format) +
                        "|" +
                        (DateTime.Now.AddHours(2)).AddMinutes(-1).ToString(format) +
                        "|127|00:60:00";

                    newTask["Last run"] =
                        DateUtil.IsoDateToDateTime(DateTime.Now.ToString(), DateTime.MinValue).ToString(format);
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

        private static string BuildPublishingTaskName(ID id)
        {
            return ItemUtil.ProposeValidItemName(string.Format("{0}_Task", id));
        }

        private IEnumerable<Item> GetItemsToPublish()
        {
            try
            {
                Item schedulesFolder = _database.GetItem(Utils.Constants.PUBLISHING_SCHEDULES_PATH);
                List<Item> itemsToPublish = new List<Item>();
                foreach (Item schedule in schedulesFolder.Children)
                {
                    if (!string.IsNullOrEmpty(schedule["Schedule"]) && !string.IsNullOrEmpty(schedule["Items"]))
                    {
                        DateTime targetDate = DateUtil.IsoDateToDateTime(schedule["Schedule"].Split('|').First());
                        if (DateTime.Compare(targetDate.AddHours(1), DateTime.Now) <= 0
                            || DateTime.Compare(targetDate.AddHours(-1), DateTime.Now) <= 0) //TODO: values here are for testing purposes only
                        {
                            Item targetItem = _database.GetItem(schedule["Items"]);
                            itemsToPublish.Add(targetItem);
                        }
                    }
                }

                return itemsToPublish;
            }
            catch (Exception e)
            {
                Log.Info(e.ToString(), this);
            }

            return Enumerable.Empty<Item>();
        }
    }
}