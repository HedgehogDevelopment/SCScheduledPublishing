using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Text;

namespace ScheduledPublishing.sitecore.shell.Applications.ContentManager.Dialogs
{
    /// <summary>
    /// Schedule Publishing code-beside
    /// </summary>
    public class SchedulePublishingDialog : DialogForm
    {
        protected DateTimePicker PublishDateTime;
        //protected Border PublishingTargets;
        protected Border ExistingSchedules;
        private readonly string ScheduleTemplateID = "{70244923-FA84-477C-8CBD-62F39642C42B}";
        private readonly string SchedulesFolderPath = "/sitecore/System/Tasks/Schedules/";
        private List<string> CorrespondingPublishingTargets = new List<string>();

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
                Error.AssertItemFound(itemFromQueryString);
                RenderExistingSchedules(itemFromQueryString);
                //RenderTargets(itemFromQueryString);
            }
        }

        /// <summary>
        /// Render available publishing targets
        /// </summary>
        /// <param name="item">The item that publishing is scheduled for</param>
        private void RenderTargets(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            Field itemTargets = item.Fields[FieldIDs.PublishingTargets];
            if (itemTargets == null) return;
            Item publishingTargets = Context.ContentDatabase.Items["/sitecore/system/publishing targets"];
            if (publishingTargets == null) return;
            StringBuilder sb = new StringBuilder();
            string itemTargetsStr = itemTargets.Value;
            foreach (Item target in publishingTargets.Children)
            {
                string validTarget = itemTargetsStr.IndexOf(target.ID.ToString(), StringComparison.InvariantCulture) >= 0 ? 
                    " checked=\"true\"" : string.Empty;
                sb.Append("<input id=\"pb_" + ShortID.Encode(target.ID) + "\" name=\"pb_" + ShortID.Encode(target.ID) + "\" class=\"scRibbonCheckbox\" type=\"checkbox\"" + validTarget + " style=\"vertical-align:middle\"/>");
                sb.Append(target.DisplayName);
                sb.Append("<br/>");
            }
            //this.PublishingTargets.InnerHtml = sb.ToString();
        }


        /// <summary>
        /// Displays a list of all already scheduled publishings' date and time for this item, ordered from most recent to furthest in time
        /// </summary>
        /// <param name="item">The item that publishing is scheduled for</param>
        private void RenderExistingSchedules(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            string correspondingTaskName = item.ID.ToString().Replace("{", string.Empty).Replace("}", string.Empty) + "Task";
            Item schedulesFolder = Context.ContentDatabase.GetItem(SchedulesFolderPath);
            IEnumerable<string> existingSchedules = schedulesFolder.Children.Where(x => x.Name == correspondingTaskName)
                    .Select(x => DateUtil.IsoDateToDateTime(x["Schedule"].Substring(0, x["Schedule"].IndexOf('|'))).ToString());
            existingSchedules = existingSchedules.OrderBy(x => x);
            
            StringBuilder sb = new StringBuilder();
            if (existingSchedules.Any())
            {
                foreach (var existingSchedule in existingSchedules)
                {
                    sb.Append("<div style=\"padding:0px 0px 2px 0px; width=100%;\">" + existingSchedule + "</div>");
                    sb.Append("<br />");
                }
            }
            else
            {
                sb.Append("<div style=\"padding:0px 0px 2px 0px; width=100%;\">" + "This item has not been scheduled for publishing yet." + "</div>");
                sb.Append("<br />");
            }
            this.ExistingSchedules.InnerHtml = sb.ToString();
        }

        /// <summary>
        /// Create a task for publishing the selected item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            Error.AssertItemFound(itemFromQueryString);

            if (DateTime.Compare(DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue), DateTime.Now) <= 0)
            {
                SheerResponse.Alert("The date selected for publish has passed. Please select a future date.");
                return;
            }
            try
            {
                using (new SecurityDisabler())
                {
                    //CorrespondingPublishingTargets.Add(target.ID.ToString());
                    //itemFromQueryString.Editing.BeginEdit();
                    //itemFromQueryString[FieldIDs.PublishingTargets] = string.Join("|", CorrespondingPublishingTargets.ToArray());
                    //Log.Info("Custom pub targets to item " + itemFromQueryString.Name + "set to " + string.Join("|", CorrespondingPublishingTargets.ToArray()), this);
                    //itemFromQueryString.Editing.AcceptChanges();
                    //itemFromQueryString.Editing.EndEdit();

                    TemplateItem scheduleTaskTemplate = Context.ContentDatabase.GetTemplate(new ID(ScheduleTemplateID));
                    string validItemName =
                        itemFromQueryString.ID.ToString().Replace("{", string.Empty).Replace("}", string.Empty);
                    Item schedulesFolder = Context.ContentDatabase.GetItem(SchedulesFolderPath);
                    //schedulesFolder.Add(validItemName + "Task", scheduleTaskTemplate);
                    Item newTask = schedulesFolder.Add(validItemName + "Task", scheduleTaskTemplate);
                        //Context.ContentDatabase.GetItem(SchedulesFolderPath + validItemName + "Task");
                    newTask.Editing.BeginEdit();
                    newTask["Command"] = "{EF235C25-AE83-4678-9E2C-C22175925893}";
                    newTask["Items"] = itemFromQueryString.Paths.FullPath;
                    newTask["CreatedByEmail"] = Context.User.Profile.Email;

                    string format = "yyyyMMddTHHmmss";
                    newTask["Schedule"] =
                        (DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue)).ToString(format) +
                        "|" +
                        (DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue)
                            .AddHours(1)
                            .AddMinutes(1)).ToString(format) +
                        "|127|00:60:00";

                    newTask["Last run"] =
                        DateUtil.IsoDateToDateTime(DateTime.Now.ToString(), DateTime.MinValue).ToString(format);
                    newTask["Auto remove"] = 1.ToString();
                    newTask.Editing.AcceptChanges();
                    newTask.Editing.EndEdit();

                    Log.Info(
                        "Task scheduling publishing: " + itemFromQueryString.Name + " " + itemFromQueryString.ID +
                        DateTime.Now, this);
                }
            }
            catch (Exception e)
            {
                Log.Info(
                    "Failed scheduling publishing: " + itemFromQueryString.Name + " " + itemFromQueryString.ID +
                    DateTime.Now + " " + e.ToString(), this);
            }
            
            base.OnOK(sender, args);
        }
    }
}