using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace ScheduledPublishing.sitecore.shell.Applications.ContentManager.Dialogs
{
    /// <summary>
    /// Schedule Publishing code-beside
    /// </summary>
    public class SchedulePublishingDialog : DialogForm
    {
        protected DateTimePicker PublishDateTime;
        protected Border ExistingSchedules;
        protected Scrollbox AllSchedules;
        private readonly string ScheduleTemplateID = "{70244923-FA84-477C-8CBD-62F39642C42B}";
        private readonly string SchedulesFolderPath = "/sitecore/System/Tasks/Schedules/";

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");

           
            if (!Context.ClientPage.IsEvent)
            {
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
                Error.AssertItemFound(itemFromQueryString);
                RenderExistingSchedules(itemFromQueryString);
                //RenderTargets(itemFromQueryString);
                RenderAllSchedules();
            }
            //else
            //{
            //    if (Context.ClientPage.ClientRequest.Source.Contains("edit"))
            //    {
            //        UrlString urlString = new UrlString(UIUtil.GetUri("control:EditScheduledPublishing"));
            //        //urlString.Append("id", obj.ID.ToString());
            //        SheerResponse.ShowModalDialog(urlString.ToString(), "500", "300", string.Empty, true);
                    
            //    }
            //}
            if (Context.ClientPage.IsPostBack)
            {
                if (Context.ClientPage.ClientRequest.Source.Contains("edit"))
                {
                    string source = Context.ClientPage.ClientRequest.Source;
                    string id = source.Substring(source.IndexOf('_') + 1);
                    OpenEditScheduleDialog(Context.ContentDatabase.GetItem(new ID(id)));
                }

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

        private void RenderAllSchedules()
        {
            Item schedulesFolder = Context.ContentDatabase.GetItem(SchedulesFolderPath);
            IEnumerable<Item> allSchedules = schedulesFolder.Children;
            StringBuilder sbHeader = new StringBuilder();
            sbHeader.Append("<table width=\"100%\" cellpadding=\"4\" cellspacing=\"0\">");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<tr style=\"background:#e9e9e9\">");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Item" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Date" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Edit" + "</b></td>");
            sbHeader.Append("</tr>");
            this.AllSchedules.Controls.Add(new LiteralControl(sbHeader.ToString()));

            
            foreach (var schedule in allSchedules)
            {
                if (!string.IsNullOrEmpty(schedule["Command"]) && !string.IsNullOrEmpty(schedule["Items"]) &&
                    !string.IsNullOrEmpty(schedule["Schedule"]))
                {
                    StringBuilder sbItem = new StringBuilder();
                    sbItem.Append("<tr style=\"background:#cedff2\">");
                    Item scheduledItem = Context.ContentDatabase.GetItem(schedule["Items"].Split('|').First());
                    sbItem.Append("<td><b>" + scheduledItem.DisplayName + "</b></td>");
                    sbItem.Append("<td>");
                    this.AllSchedules.Controls.Add(new LiteralControl(sbItem.ToString()));
                    DateTime pbDate = DateUtil.IsoDateToDateTime(schedule["Schedule"].Split('|').First());
                    this.AllSchedules.Controls.Add(new LiteralControl(pbDate.ToString()));
                    //DateTimePicker dtPicker = new DateTimePicker();
                    //dtPicker.ID = "dt_" + schedule.ID;
                    //dtPicker.Width = new Unit(100.0, UnitType.Percentage);
                    //dtPicker.Value = schedule["Schedule"].Split('|').First();
                    //this.AllSchedules.Controls.Add(dtPicker);
                    this.AllSchedules.Controls.Add(new LiteralControl("</td><td>"));

                    //Sitecore.Web.UI.HtmlControls.Button editButton = new Sitecore.Web.UI.HtmlControls.Button();
                    //editButton.Value = "Edit";
                    //editButton.ID = "edit_" + schedule.ID;
                    //editButton.Click += new EventHandler(EditClick);
                    //this.AllSchedules.Controls.Add(editButton);

                    this.AllSchedules.Controls.Add(new LiteralControl("<Button Header=\"Edit\" Click=\"EditClick\" />"));
                    this.AllSchedules.Controls.Add(new LiteralControl("</td></tr>"));
                }
            }
            this.AllSchedules.Controls.Add(new LiteralControl("</table"));
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

        private void EditClick()
        {
           throw new Exception("edit click");
        }

        private void OpenEditScheduleDialog(Item item)
        {
            Assert.ArgumentNotNull((object)item, "item");
            NameValueCollection parameters = new NameValueCollection();
            parameters["id"] = item.ID.ToString();
            parameters["language"] = item.Language.ToString();
            parameters["version"] = item.Version.ToString();
            parameters["databasename"] = item.Database.Name;
            Context.ClientPage.Start((object)this, "RunEdit", parameters);
        }

        protected void RunEdit(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            string dbName = args.Parameters["databasename"];
            string id = args.Parameters["id"];
            string lang = args.Parameters["language"];
            string ver = args.Parameters["version"];
            Database database = Factory.GetDatabase(dbName);
            Assert.IsNotNull((object)database, dbName);
            Item obj = database.Items[id, Language.Parse(lang), Sitecore.Data.Version.Parse(ver)];
            if (obj == null)
            {
                SheerResponse.Alert("Item not found.");
            }
            else
            {
                if (!SheerResponse.CheckModified())
                    return;
                if (args.IsPostBack)
                {
                    return;
                }
                UrlString urlString = new UrlString(UIUtil.GetUri("control:editSchedulePublishing"));
                urlString.Append("id", obj.ID.ToString());
                SheerResponse.ShowModalDialog(urlString.ToString(), "500", "300", string.Empty, true);
                args.WaitForPostBack();
            }
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

            if (!string.IsNullOrEmpty(this.PublishDateTime.Value))
            {
                SchedulePublishing(itemFromQueryString);
            }

            base.OnOK(sender, args);
        }

        private void SchedulePublishing(Item itemFromQueryString)
        {
            if (DateTime.Compare(DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue), DateTime.Now) <= 0)
            {
                SheerResponse.Alert("The date selected for publish has passed. Please select a future date.");
                return;
            }
            try
            {
                using (new SecurityDisabler())
                {
                    TemplateItem scheduleTaskTemplate = Context.ContentDatabase.GetTemplate(new ID(ScheduleTemplateID));
                    string validItemName = itemFromQueryString.ID.ToString().Replace("{", string.Empty).Replace("}", string.Empty);
                    Item schedulesFolder = Context.ContentDatabase.GetItem(SchedulesFolderPath);
                    Item newTask = schedulesFolder.Add(validItemName + "Task", scheduleTaskTemplate);
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
        }
    }
}