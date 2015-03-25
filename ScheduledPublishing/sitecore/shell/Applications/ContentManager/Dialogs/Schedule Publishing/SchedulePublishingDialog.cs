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
        protected Border PublishingTargets;
        private readonly string ScheduleTemplateID = "{70244923-FA84-477C-8CBD-62F39642C42B}";
        private readonly string CustomScheduleTemplateID = "{62F2F77F-903B-4FEE-8A7C-F0DA51910A2B}";

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
                Error.AssertItemFound(itemFromQueryString);
                RenderTargets(itemFromQueryString);
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
                string validTarget = itemTargetsStr.IndexOf(target.ID.ToString(), StringComparison.InvariantCulture) >= 0
                    ? " checked=\"true\""
                    : string.Empty;

                sb.Append("<input id=\"pb_" + ShortID.Encode(target.ID) + "\" name=\"pb_" + ShortID.Encode(target.ID) + "\" class=\"scRibbonCheckbox\" type=\"checkbox\"" + validTarget + readOnly + " style=\"vertical-align:middle\"/>");
                sb.Append(target.DisplayName);
                sb.Append("<br/>");
            }
            this.PublishingTargets.InnerHtml = sb.ToString();
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
                    TemplateItem scheduleTaskTemplate =
                        Context.ContentDatabase.GetTemplate(new ID(ScheduleTemplateID));
                    Item schedulesFolder = Context.ContentDatabase.GetItem("/sitecore/System/Tasks/Schedules");
                    schedulesFolder.Add(itemFromQueryString.Name + "Task", scheduleTaskTemplate);
                    Item newTask =
                        Context.ContentDatabase.GetItem("/sitecore/System/Tasks/Schedules/" +
                                                        itemFromQueryString.Name +
                                                        "Task");
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
            catch
            {
                Log.Info(
                    "Failed scheduling publishing: " + itemFromQueryString.Name + " " + itemFromQueryString.ID +
                    DateTime.Now, this);
            }
            ////using (new StatisticDisabler(StatisticDisablerState.ForItemsWithoutVersionOnly))
            ////{
            //    itemFromQueryString.Editing.BeginEdit();
            //DateTime dateToPublish = DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue);
            //    itemFromQueryString.Publishing.PublishDate = DateUtil.ParseDateTime(this.PublishDateTime.Value, DateTime.MinValue);
            //    foreach (string text in Context.ClientPage.ClientRequest.Form.Keys)
            //    {
            //        if (text != null && text.StartsWith("pb_", StringComparison.InvariantCulture))
            //        {
            //            string str = ShortID.Decode(StringUtil.Mid(text, 3));
            //            pbTargets.Add(str);
            //        }
            //    }
            //    itemFromQueryString[FieldIDs.PublishingTargets] = pbTargets.ToString();
            //    itemFromQueryString.Editing.EndEdit();
            
            ////}
            //Log.Info("Set publishing for: " + itemFromQueryString.Name + " date: " + dateToPublish.ToShortTimeString(), this);
            ////Log.Audit(this, "Set publishing for: " + AuditFormatter.FormatItem(itemFromQueryString) + " date: " + dateToPublish.ToShortTimeString());
            SheerResponse.SetDialogValue("yes");
            base.OnOK(sender, args);
        }
    }
}