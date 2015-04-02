using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScheduledPublishing.sitecore.shell.Applications.ContentManager.Dialogs
{
    /// <summary>
    /// Schedule Publishing code-beside
    /// </summary>
    public class SchedulePublishingDialog : DialogForm
    {
        protected DateTimePicker PublishDateTime;
        protected Border ExistingSchedules;
        protected Literal ServerTime;
        private readonly string ScheduleTemplateID = "{70244923-FA84-477C-8CBD-62F39642C42B}";
        private readonly string SchedulesFolderPath = "/sitecore/System/Tasks/Schedules/";

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            
            if (!Context.ClientPage.IsEvent)
            {
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
                Error.AssertItemFound(itemFromQueryString);
                ServerTime.Text = "Current time on server: " + DateTime.Now;
                RenderExistingSchedules(itemFromQueryString);
                //RenderTargets(itemFromQueryString);
            }

            base.OnLoad(e);
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
            existingSchedules = existingSchedules.OrderBy(DateTime.Parse);
            
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
            bool isUnpublishing= bool.Parse(Context.Request.QueryString["unpublish"]);
            Error.AssertItemFound(itemFromQueryString);

            if (!string.IsNullOrEmpty(this.PublishDateTime.Value))
            {
                SchedulePublishing(itemFromQueryString, isUnpublishing);
            }

            base.OnOK(sender, args);
        }

        /// <summary>
        /// Create a task to invoke publishing command at specific time
        /// </summary>
        /// <param name="itemFromQueryString">Item to be published</param>
        /// <param name="isUnpublishing">If the item is to be unpublished instead of published</param>
        private void SchedulePublishing(Item itemFromQueryString, bool isUnpublishing)
        {
            bool doPublish = true;
            // Validate date chosen
            doPublish = ValidateDateChosen();

            // Validate item to be published
            doPublish = ValidateItemValidators(itemFromQueryString);

            //Validate if item is publishable
            doPublish = ValidatePublishable(itemFromQueryString);

            // Create publishing task
            if (doPublish)
            {
                CreatePublishingTask(itemFromQueryString, isUnpublishing);
            }
        }

        private bool ValidatePublishable(Item itemFromQueryString)
        {
            if (!itemFromQueryString.Publishing.IsPublishable(
                DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue), false))
            {
                SheerResponse.Alert("Item is not publishable at that time.");
                return false;
            }
            return true;
        }

        private bool ValidateItemValidators(Item itemFromQueryString)
        {
            bool isValid = CheckValidation(itemFromQueryString);
            if (!isValid)
            {
                SheerResponse.Alert("This item has validation errors. You may want to review them and schedule another publish.");
            }
            return true;
        }

        private bool ValidateDateChosen()
        {
            if (DateTime.Compare(DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue), DateTime.Now) <= 0)
            {
                SheerResponse.Alert("The date selected for publish has passed. Please select a future date.");
                return false;
            }
            return true;
        }

        private void CreatePublishingTask(Item itemFromQueryString, bool isUnpublishing)
        {
            try
            {
                using (new SecurityDisabler())
                {
                    TemplateItem scheduleTaskTemplate = Context.ContentDatabase.GetTemplate(new ID(ScheduleTemplateID));
                    string validItemName = itemFromQueryString.ID.ToString()
                        .Replace("{", string.Empty)
                        .Replace("}", string.Empty);
                    Item schedulesFolder = Context.ContentDatabase.GetItem(SchedulesFolderPath);
                    Item newTask = schedulesFolder.Add(validItemName + "Task", scheduleTaskTemplate);
                    newTask.Editing.BeginEdit();
                    newTask["Command"] = "{EF235C25-AE83-4678-9E2C-C22175925893}";
                    newTask["Items"] = itemFromQueryString.Paths.FullPath;
                    newTask["CreatedByEmail"] = Context.User.Profile.Email;
                    newTask["Unpublish"] = isUnpublishing ? 1.ToString() : string.Empty;

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

                    string action = isUnpublishing ? "unpublishing" : "publishing";
                    Log.Info(
                        "Task scheduling " + action + ": " + itemFromQueryString.Name + " " + itemFromQueryString.ID +
                        DateTime.Now, this);
                }
            }
            catch (Exception e)
            {
                string action = isUnpublishing ? "unpublishing" : "publishing";
                Log.Info(
                    "Failed scheduling " + action + ": " + itemFromQueryString.Name + " " + itemFromQueryString.ID +
                    DateTime.Now + " " + e.ToString(), this);
            }
        }

        private bool CheckValidation(Item item)
        {
            item.Fields.ReadAll();
            ValidatorCollection validators = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.Workflow, item);
            ValidatorCollection validatorsBar = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.ValidatorBar, item);
            ValidatorCollection validatorsButton = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.ValidateButton, item);
            ValidatorCollection validatorsGutter = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.Gutter, item);
            var options = new ValidatorOptions(true);
            ValidatorManager.Validate(validators, options);
            foreach (BaseValidator validator in validators)
            {
                if (validator.Result != ValidatorResult.Valid)
                {
                    return false;
                }
            }
            //return !validators.Any(x => x.Result != ValidatorResult.Valid);
            return true;
        }
    }
}