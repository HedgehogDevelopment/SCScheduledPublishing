using ScheduledPublish.Models;
using ScheduledPublish.Utils;
using ScheduledPublish.Validation;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScheduledPublish.Repos.Abstraction;
using ScheduledPublish.Repos.Implementation;
using Constants = ScheduledPublish.Utils.Constants;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace ScheduledPublish.sitecore.shell.Applications.Content_Manager.Dialogs.Edit_Scheduled_Publish
{
    /// <summary>
    /// Edit Schedule Publish Dialog code-beside
    /// </summary>
    public class EditScheduledPublishDialog : DialogForm
    {
        protected Scrollbox AllSchedules;
        protected Literal ServerTime;

        private readonly Database _database = Context.ContentDatabase;
        private readonly CultureInfo _culture = Context.Culture;
        private ISchedulesRepo<PublishSchedule> _schedulesRepo;


        /// <summary>
        /// Raises the load event
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            _schedulesRepo = new ScheduledPublishRepo();

            if (!Context.ClientPage.IsEvent)
            {
                ServerTime.Text = Constants.CURREN_TIME_ON_SERVER_TEXT + DateTime.Now.ToString(_culture);
                RenderAllSchedules();
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Displays all current publishing schedules ordered by date and time
        /// </summary>
        private void RenderAllSchedules()
        {
            StringBuilder sbHeader = new StringBuilder();
            sbHeader.Append("<table width=\"100%\" cellpadding=\"4\" cellspacing=\"0\">");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<tr style=\"background:#e9e9e9\">");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Item" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Action" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Subitems" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Mode" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Date" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Delete" + "</b></td>");
            sbHeader.Append("</tr>");
            AllSchedules.Controls.Add(new LiteralControl(sbHeader.ToString()));

            IEnumerable<PublishSchedule> allSchedules;
            using (new LanguageSwitcher(LanguageManager.DefaultLanguage))
            {
                allSchedules = _schedulesRepo.GetUnexecutedSchedules();
            }
            
            foreach (var schedule in allSchedules)
            {
                if (schedule.InnerItem != null)
                {
                    StringBuilder sbItem = new StringBuilder();
                    // Item name, path, recurrence 
                    sbItem.Append("<tr style='background:#cedff2;border-bottom:1px solid #F0F1F2;'>");
                    Item scheduledItem = schedule.Items.FirstOrDefault();
                    sbItem.Append("<td>");
                    sbItem.AppendFormat("<b>{0}</b>", scheduledItem == null ? Constants.WEBSITE_PUBLISH_TEXT : scheduledItem.DisplayName);
                    sbItem.AppendFormat("<br />{0}", scheduledItem == null ? string.Empty : scheduledItem.Paths.FullPath);
                    string recurrenceMessage = 
                        DialogsHelper.GetRecurrenceMessage(schedule.RecurrenceType,
                        schedule.HoursToNextSchedule);
                    if (!string.IsNullOrWhiteSpace(recurrenceMessage))
                    {
                        sbItem.Append("<br /> Recurrence: " + recurrenceMessage);
                    }
                    sbItem.Append("</td>");

                    // Is publishing/unpublishing
                    sbItem.Append("<td style='border-left:1px solid #F0F1F2;'>");
                    string isUnpublishing = schedule.Unpublish ? "Unpublish" : "Publish";
                    sbItem.Append(isUnpublishing);
                    sbItem.Append("</td><td style='border-left:1px solid #F0F1F2; text-align: center;'>");

                    // Subitems
                    string includeSubitems = schedule.PublishChildren ? "Yes" : "No";
                    sbItem.Append(includeSubitems);
                    sbItem.Append("</td><td style='border-left:1px solid #F0F1F2;'>");

                    // Publish mode
                    string publishMode = schedule.PublishMode.ToString();
                    sbItem.Append(publishMode);
                    sbItem.Append("</td><td style='border-left:1px solid #F0F1F2;'>");

                    // Current scheudled publish date and time
                    AllSchedules.Controls.Add(new LiteralControl(sbItem.ToString()));
                    DateTime pbDate = schedule.ScheduledDate;
                    AllSchedules.Controls.Add(new LiteralControl(pbDate.ToString(_culture)));

                    // Pick new date and time
                    DateTimePicker dtPicker = new DateTimePicker();
                    dtPicker.ID = "dt_" + schedule.InnerItem.ID;
                    dtPicker.Width = new Unit(100.0, UnitType.Percentage);
                    dtPicker.Value = DateUtil.ToIsoDate(schedule.ScheduledDate);
                    AllSchedules.Controls.Add(dtPicker);
                    AllSchedules.Controls.Add(new LiteralControl("</td>"));

                    // Delete schedule
                    AllSchedules.Controls.Add(new LiteralControl("<td style='border-left:1px solid #F0F1F2;'>"));
                    Checkbox deleteCheckbox = new Checkbox();
                    deleteCheckbox.ID = "del_" + schedule.InnerItem.ID;
                    AllSchedules.Controls.Add(deleteCheckbox);

                    AllSchedules.Controls.Add(new LiteralControl("</td></tr>"));
                }
            }

            AllSchedules.Controls.Add(new LiteralControl("</table"));
        }

        /// <summary>
        /// Handles a click on the OK button. Save the new publishing schedules
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            using (new LanguageSwitcher(LanguageManager.DefaultLanguage))
            {
                foreach (string key in Context.ClientPage.ClientRequest.Form.Keys)
                {
                    if (key != null && key.StartsWith("dt_", StringComparison.InvariantCulture))
                    {
                        string id = StringUtil.Mid(key, 3, 38);

                        DateTimePicker dtEditPicker = AllSchedules.FindControl("dt_" + id) as DateTimePicker;

                        Assert.IsNotNull(dtEditPicker, "dtEditPicker");

                        DateTime dateTime = DateUtil.IsoDateToDateTime(dtEditPicker.Value);
                        PublishSchedule publishSchedule = new PublishSchedule(_database.GetItem(new ID(id)));

                        //Scheudled time has changed
                        if (publishSchedule.ScheduledDate != dateTime)
                        {
                            publishSchedule.ScheduledDate = dateTime;

                            ValidationResult validationResult = ScheduledPublishValidator.Validate(publishSchedule);
                            if (!validationResult.IsValid)
                            {
                                SheerResponse.Alert(string.Join(Environment.NewLine, validationResult.ValidationErrors));
                                return;
                            }

                            _schedulesRepo.UpdateSchedule(publishSchedule);
                        }
                    }
                    else if (key != null && key.StartsWith("del_", StringComparison.InvariantCulture))
                    {
                        string id = StringUtil.Mid(key, 4, 38);
                        Checkbox deleteCheckbox = AllSchedules.FindControl("del_" + id) as Checkbox;

                        Assert.IsNotNull(deleteCheckbox, "deleteCheckbox");

                        bool doDelete = deleteCheckbox.Checked;
                        if (doDelete)
                        {
                            Item publishOption = _database.GetItem(new ID(id));
                            PublishSchedule schedule = new PublishSchedule(publishOption);
                            _schedulesRepo.DeleteSchedule(schedule);
                        }
                    }
                }
            }

            base.OnOK(sender, args);
        }
    }
}