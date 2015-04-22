using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ScheduledPublishing.sitecore.shell.Applications.ContentManager.Dialogs
{
    public class EditScheduledPublishingDialog : DialogForm
    {
        protected Scrollbox AllSchedules;
        protected Sitecore.Web.UI.HtmlControls.Literal ServerTime;
        private readonly string PUBLISHING_SCHEDULES_PATH = "/sitecore/system/Tasks/PublishingSchedules/";
        
        protected override void OnLoad(EventArgs e)
        {
            if (!Context.ClientPage.IsEvent)
            {
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
                Error.AssertItemFound(itemFromQueryString);

                ServerTime.Text = "Current time on server: " + DateTime.Now;

                RenderAllSchedules();
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Displays all current publishing schedules ordered by date and time
        /// </summary>
        private void RenderAllSchedules()
        {
            Item schedulesFolder = Context.ContentDatabase.GetItem(PUBLISHING_SCHEDULES_PATH);
            IEnumerable<Item> allSchedules = schedulesFolder.Children;
            allSchedules = allSchedules.OrderBy(x => DateUtil.IsoDateToDateTime(x["Schedule"].Split('|').First()));

            StringBuilder sbHeader = new StringBuilder();
            sbHeader.Append("<table width=\"100%\" cellpadding=\"4\" cellspacing=\"0\">");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<tr style=\"background:#e9e9e9\">");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Item" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Unpublish" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Date" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Delete" + "</b></td>");
            sbHeader.Append("</tr>");
            this.AllSchedules.Controls.Add(new LiteralControl(sbHeader.ToString()));
            
            foreach (var schedule in allSchedules)
            {
                if (!string.IsNullOrEmpty(schedule["Command"]) && !string.IsNullOrEmpty(schedule["Items"]) &&
                    !string.IsNullOrEmpty(schedule["Schedule"]))
                {
                    StringBuilder sbItem = new StringBuilder();
                    // Item name and path
                    sbItem.Append("<tr style='background:#cedff2;border-bottom:1px solid #F0F1F2;'>");
                    Item scheduledItem = Context.ContentDatabase.GetItem(schedule["Items"].Split('|').First());
                    sbItem.Append("<td><b>" + scheduledItem.DisplayName + "</b><br />" + scheduledItem.Paths.FullPath + "</td>");

                    // Is unpublishing
                    sbItem.Append("<td style='border-left:1px solid #F0F1F2;text-align:center;'>");
                    string isUnpublishing = string.IsNullOrEmpty(schedule["Unpublish"]) ? "No" : "Yes";
                    sbItem.Append(isUnpublishing);
                    sbItem.Append("</td><td style='border-left:1px solid #F0F1F2;'>");

                    // Current scheudled publish date and time
                    this.AllSchedules.Controls.Add(new LiteralControl(sbItem.ToString()));
                    DateTime pbDate = DateUtil.IsoDateToDateTime(schedule["Schedule"].Split('|').First());
                    this.AllSchedules.Controls.Add(new LiteralControl(pbDate.ToString()));

                    // Pick new date and time
                    DateTimePicker dtPicker = new DateTimePicker();
                    dtPicker.ID = "dt_" + schedule.ID;
                    dtPicker.Width = new Unit(100.0, UnitType.Percentage);
                    dtPicker.Value = schedule["Schedule"].Split('|').First();
                    this.AllSchedules.Controls.Add(dtPicker);
                    this.AllSchedules.Controls.Add(new LiteralControl("</td>"));

                    // Delete schedule
                    this.AllSchedules.Controls.Add(new LiteralControl("<td style='border-left:1px solid #F0F1F2;'>"));
                    Checkbox deleteCheckbox = new Checkbox();
                    deleteCheckbox.ID = "del_" + schedule.ID;
                    this.AllSchedules.Controls.Add(deleteCheckbox);

                    this.AllSchedules.Controls.Add(new LiteralControl("</td></tr>"));
                }
            }

            this.AllSchedules.Controls.Add(new LiteralControl("</table"));
        }

        /// <summary>
        /// Save the new publishing schedules
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            foreach (string key in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (key != null && key.StartsWith("dt_", StringComparison.InvariantCulture))
                {
                    string id = StringUtil.Mid(key, 3, 38);

                    DateTimePicker dtEditPicker = this.AllSchedules.FindControl("dt_" + id) as DateTimePicker;
                    Assert.IsNotNull(dtEditPicker, "dtEditPicker");
                    DateTime dateTime = DateUtil.IsoDateToDateTime(dtEditPicker.Value);

                    Item task = Context.ContentDatabase.GetItem(new ID(id));

                    using (new SecurityDisabler())
                    {
                        task.Editing.BeginEdit();
                        string startDateTime = DateUtil.ToIsoDate(dateTime);
                        string endDateTime = DateUtil.ToIsoDate(dateTime.AddHours(1).AddMinutes(1));
                        task["Schedule"] = startDateTime + "|" + endDateTime + "|127|00:60:00";
                        task.Editing.AcceptChanges();
                        task.Editing.EndEdit();
                    }
                }
                else if (key != null && key.StartsWith("del_", StringComparison.InvariantCulture))
                {
                    string id = StringUtil.Mid(key, 4, 38);
                    Checkbox deleteCheckbox = this.AllSchedules.FindControl("del_" + id) as Checkbox;
                    Assert.IsNotNull(deleteCheckbox, "deleteCheckbox");
                    bool doDelete = deleteCheckbox.Checked;

                    if (doDelete)
                    {
                        Item task = Context.ContentDatabase.GetItem(new ID(id));

                        using (new SecurityDisabler())
                        {
                            if (Sitecore.Configuration.Settings.RecycleBinActive)
                            {
                                task.Recycle();
                            }
                            else
                            {
                                task.Delete();
                            }
                        }
                    }
                }
            }

            base.OnOK(sender, args);
        }
    }
}