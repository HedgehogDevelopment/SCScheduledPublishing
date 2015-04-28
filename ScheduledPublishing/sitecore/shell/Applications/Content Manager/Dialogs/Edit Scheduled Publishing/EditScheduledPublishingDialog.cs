using ScheduledPublishing.Models;
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
using Constants = ScheduledPublishing.Utils.Constants;

namespace ScheduledPublishing.sitecore.shell.Applications.ContentManager.Dialogs
{
    public class EditScheduledPublishingDialog : DialogForm
    {
        protected Scrollbox AllSchedules;
        protected Sitecore.Web.UI.HtmlControls.Literal ServerTime;
        
        protected override void OnLoad(EventArgs e)
        {
            if (!Context.ClientPage.IsEvent)
            {
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
            Item schedulesFolder = Context.ContentDatabase.GetItem(Constants.PUBLISH_OPTIONS_FOLDER_ID);
            List<ScheduledPublishOptions> allSchedules = schedulesFolder.Axes.GetDescendants()
                .Where(x => x.TemplateID == Constants.PUBLISH_OPTIONS_TEMPLATE_ID)
                .Select(x => new ScheduledPublishOptions(x)).ToList();
            allSchedules.Sort((a, b) => DateUtil.IsoDateToDateTime(a.PublishDateString).CompareTo(DateUtil.IsoDateToDateTime(b.PublishDateString)));

            StringBuilder sbHeader = new StringBuilder();
            sbHeader.Append("<table width=\"100%\" cellpadding=\"4\" cellspacing=\"0\">");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<tr style=\"background:#e9e9e9\">");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Item" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Action" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Date" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Delete" + "</b></td>");
            sbHeader.Append("</tr>");
            this.AllSchedules.Controls.Add(new LiteralControl(sbHeader.ToString()));
            
            foreach (var schedule in allSchedules)
            {
                if (schedule.InnerItem != null)
                {
                    StringBuilder sbItem = new StringBuilder();
                    // Item name and path
                    sbItem.Append("<tr style='background:#cedff2;border-bottom:1px solid #F0F1F2;'>");
                    Item scheduledItem = schedule.ItemToPublish;
                    sbItem.Append("<td><b>" + scheduledItem.DisplayName + "</b><br />" + scheduledItem.Paths.FullPath + "</td>");

                    // Is publishing/unpublishing
                    sbItem.Append("<td style='border-left:1px solid #F0F1F2;'>");
                    string isUnpublishing = schedule.Unpublish ? "Unpublish" : "Publish";
                    sbItem.Append(isUnpublishing);
                    sbItem.Append("</td><td style='border-left:1px solid #F0F1F2;'>");

                    // Current scheudled publish date and time
                    this.AllSchedules.Controls.Add(new LiteralControl(sbItem.ToString()));
                    DateTime pbDate = DateUtil.IsoDateToDateTime(schedule.PublishDateString);
                    this.AllSchedules.Controls.Add(new LiteralControl(pbDate.ToString()));

                    // Pick new date and time
                    DateTimePicker dtPicker = new DateTimePicker();
                    dtPicker.ID = "dt_" + schedule.InnerItem.ID;
                    dtPicker.Width = new Unit(100.0, UnitType.Percentage);
                    dtPicker.Value = schedule.PublishDateString;
                    this.AllSchedules.Controls.Add(dtPicker);
                    this.AllSchedules.Controls.Add(new LiteralControl("</td>"));

                    // Delete schedule
                    this.AllSchedules.Controls.Add(new LiteralControl("<td style='border-left:1px solid #F0F1F2;'>"));
                    Checkbox deleteCheckbox = new Checkbox();
                    deleteCheckbox.ID = "del_" + schedule.InnerItem.ID;
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

                    ScheduledPublishOptions publishOption = new ScheduledPublishOptions(Context.ContentDatabase.GetItem(new ID(id)));

                    //Scheudled time has changed
                    if (publishOption.PublishDateString != DateUtil.ToIsoDate(dateTime))
                    {
                        using (new SecurityDisabler())
                        {
                            publishOption.InnerItem.Editing.BeginEdit();
                            string startDateTime = DateUtil.ToIsoDate(dateTime);
                            publishOption.PublishDateString = startDateTime;
                            publishOption.InnerItem.Editing.AcceptChanges();
                            publishOption.InnerItem.Editing.EndEdit();

                            Item newFolder = Utils.Utils.GetOrCreateFolder(dateTime, Context.ContentDatabase);
                            publishOption.InnerItem.MoveTo(newFolder);
                        }
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
                        Item publishOption = Context.ContentDatabase.GetItem(new ID(id));

                        using (new SecurityDisabler())
                        {
                            if (Sitecore.Configuration.Settings.RecycleBinActive)
                            {
                                publishOption.Recycle();
                            }
                            else
                            {
                                publishOption.Delete();
                            }
                        }
                    }
                }2
            }

            base.OnOK(sender, args);
        }
    }
}