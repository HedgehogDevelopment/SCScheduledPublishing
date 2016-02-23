using ScheduledPublish.Models;
using ScheduledPublish.Repos;
using ScheduledPublish.Validation;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Security.AccessControl;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using ScheduledPublish.Recurrence.Implementation;
using ScheduledPublish.Utils;
using Constants = ScheduledPublish.Utils.Constants;
using Control = Sitecore.Web.UI.HtmlControls.Control;
using ItemList = Sitecore.Collections.ItemList;
using Action = Sitecore.Web.UI.HtmlControls.Action;

namespace ScheduledPublish.sitecore.shell.Applications.Content_Manager.Dialogs.Schedule_Publish
{
    /// <summary>
    /// Schedule Publish Dialog code-beside
    /// </summary>
    public class SchedulePublishDialog : DialogForm
    {
        protected Groupbox ScheduleSettings;
        protected Groupbox ScheduleLanguages;
        protected Groupbox ScheduleTargets;
        protected Border ExistingSchedules;
        protected GridPanel GridRecurrence;
        protected Border Languages;
        protected Border PublishModePanel;
        protected Border PublishingTargets;
        protected Literal ServerTime;
        protected Literal PublishTimeLit;
        protected Checkbox PublishChildren;
        protected Checkbox PublishRelatedItems;
        protected Radiobutton SmartPublish;
        protected Radiobutton Hourly;
        protected Radiobutton Daily;
        protected Radiobutton Weekly;
        protected Radiobutton Monthly;
        protected DateTimePicker PublishDateTimePicker;
        protected Border BorderRecurrenceSettings;
        protected Action VisibleAction;
        protected Edit HoursToNextPublish;
        protected Button RecurrenceButton;

        private readonly Database _database = Context.ContentDatabase;
        private readonly CultureInfo _culture = Context.Culture;
        private ScheduledPublishRepo _scheduledPublishRepo;

        /// <summary>
        /// Current selected item
        /// </summary>
        private Item _innerItem;
        private Item InnerItem
        {
            get
            {
                if (_innerItem != null)
                {
                    return _innerItem;
                }

                _innerItem = UIUtil.GetItemFromQueryString(_database);
                return _innerItem;
            }
        }

        /// <summary>
        /// Selected publish date
        /// </summary>
        private DateTime _selectedPublishDate;
        private DateTime SelectedPublishDate
        {
            get
            {
                if (_selectedPublishDate != DateTime.MinValue)
                {
                    return _selectedPublishDate;
                }

                _selectedPublishDate =
                    DateUtil.IsoDateToDateTime(PublishDateTimePicker.Value, DateTime.MinValue);
                return _selectedPublishDate;
            }
        }

        /// <summary>
        /// Selected publishing targets
        /// </summary>
        private IEnumerable<Database> _selectedTargets;
        private IEnumerable<Database> SelectedTargets
        {
            get
            {
                if (_selectedTargets != null && _selectedTargets.Any())
                {
                    return _selectedTargets;
                }

                List<Item> targetItems = new List<Item>();
                foreach (string str in Context.ClientPage.ClientRequest.Form.Keys)
                {
                    if (str != null && str.StartsWith("pt_", StringComparison.InvariantCulture))
                    {
                        Item target = _database.Items[ShortID.Decode(str.Substring(3))];
                        Assert.IsNotNull(target, "Publish target not found.");
                        targetItems.Add(target);
                    }
                }

                _selectedTargets =
                    targetItems.Select(x => Database.GetDatabase(x[FieldIDs.PublishingTargetDatabase]));
                return _selectedTargets;
            }
        }

        /// <summary>
        /// Selected languages to publish 
        /// </summary>
        private IEnumerable<Language> _selectedLanguages;
        private IEnumerable<Language> SelectedLanguages
        {
            get
            {
                if (_selectedLanguages != null && _selectedLanguages.Any())
                {
                    return _selectedLanguages;
                }

                List<Language> languages = new List<Language>();
                foreach (string index in Context.ClientPage.ClientRequest.Form.Keys)
                {
                    if (index != null && index.StartsWith("lang_", StringComparison.InvariantCulture))
                    {
                        Language language = LanguageManager.GetLanguage(Context.ClientPage.ClientRequest.Form[index]);
                        Assert.IsNotNull(language, "Publish language not found.");
                        languages.Add(language);
                    }
                }

                _selectedLanguages = languages;
                return languages;
            }
        }

        /// <summary>
        /// Hourly, Daily, Weekly, Monthly. 
        /// </summary>
        private RecurrenceType RecurrenceType
        {
            get
            {
                string reccurenceType = string.Empty;

                if (Hourly.Checked)
                {
                    reccurenceType = Hourly.Value;
                }
                else if (Daily.Checked)
                {
                    reccurenceType = Daily.Value;
                }
                else if (Weekly.Checked)
                {
                    reccurenceType = Weekly.Value;
                }
                else if (Monthly.Checked)
                {
                    reccurenceType = Monthly.Value;
                }

                RecurrenceType castedType;
                return Enum.TryParse<RecurrenceType>(reccurenceType, true, out castedType)
                    ? castedType
                    : RecurrenceType.None;
            }
        }

        /// <summary>
        /// Hours to the next publish if Hourly option is set as Recurrence type.
        /// </summary>
        private int HoursToNextPublishValue
        {
            get
            {
                int value;
                int.TryParse(HoursToNextPublish.Value, out value);
                return value;
            }
        }
        /// <summary>
        /// If true, the action is unpublishing from selected database(s)
        /// </summary>
        private static bool Unpublish
        {
            get
            {
                bool unpublish;
                bool.TryParse(WebUtil.GetQueryString("unpublish"), out unpublish);

                return unpublish;
            }
        }

        /// <summary>
        /// Raises the load event
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");

            _scheduledPublishRepo = new ScheduledPublishRepo();

            if (!Context.ClientPage.IsEvent)
            {
                if (Unpublish)
                {
                    BuildUnpublishTitles();

                    PublishModePanel.Visible = false;
                    ScheduleLanguages.Visible = false;
                    RecurrenceButton.Visible = false;
                }

                BuildExistingSchedules();
                BuildPublishingTargets();
                BuildLanguages();

                ServerTime.Text = Constants.CURREN_TIME_ON_SERVER_TEXT + DateTime.Now.ToString(_culture);
                SmartPublish.Checked = true;

                GridRecurrence.SetExtensibleProperty(BorderRecurrenceSettings, "Row.Style", "display:none");
                VisibleAction.Checked = false;
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Handles a click on the OK button. Creates a task for publishing the selected item.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            using (new LanguageSwitcher(LanguageManager.DefaultLanguage))
            {

                PublishSchedule publishSchedule = new PublishSchedule
                {
                    ItemToPublish = InnerItem,
                    PublishDate = SelectedPublishDate,
                    SourceDatabase = _database,
                    TargetDatabases = SelectedTargets,
                    TargetLanguages = SelectedLanguages,
                    Unpublish = Unpublish,
                    PublishMode = SmartPublish.Checked ? PublishMode.Smart : PublishMode.Full,
                    PublishChildren = PublishChildren.Checked,
                    PublishRelatedItems = PublishRelatedItems.Checked,
                    SchedulerUsername = Context.User.Name,
                    RecurrenceType = RecurrenceType,
                    HoursToNextPublish = HoursToNextPublishValue,
                    IsPublished = false
                };

                if (Unpublish)
                {
                    publishSchedule.TargetLanguages = LanguageManager.GetLanguages(_database);
                }

                ValidationResult validationResult = ScheduledPublishValidator.Validate(publishSchedule);
                if (!validationResult.IsValid)
                {
                    SheerResponse.Alert(string.Join(Environment.NewLine, validationResult.ValidationErrors));
                    return;
                }

                _scheduledPublishRepo.CreatePublishSchedule(publishSchedule);
            }

            base.OnOK(sender, args);
        }

        /// <summary>
        /// Show / hide the Recurrence section
        /// </summary>
        protected void ToggleRecurrence()
        {
            bool visible = VisibleAction.Checked;

            Context.ClientPage.ClientResponse.SetStyle("BorderRecurrenceSettingsRow", "display", visible ? "none" : "");

            // Remember to persist the current visibility status, both in the viewstate
            VisibleAction.Checked = !visible;
        }


        /// <summary>
        /// Renders available publishing targets.
        /// </summary>
        private void BuildPublishingTargets()
        {
            ItemList publishingTargets = PublishManager.GetPublishingTargets(_database);
            if (publishingTargets == null)
            {
                Log.Warn("Scheduled Publish: No publish targets found", this);
                return;
            }

            foreach (Item target in publishingTargets)
            {
                string id = string.Format("pt_{0}", ShortID.Encode(target.ID));
                Database database = Database.GetDatabase(target[FieldIDs.PublishingTargetDatabase]);
                if (database == null)
                {
                    continue;
                }

                HtmlGenericControl targetInput = new HtmlGenericControl("input");
                targetInput.ID = id;
                targetInput.Attributes["type"] = "checkbox";
                targetInput.Disabled = !target.Access.CanWrite();
                if (publishingTargets.Count == 1)
                {
                    targetInput.Attributes["checked"] = "checked";
                }
                PublishingTargets.Controls.Add(targetInput);

                HtmlGenericControl targetLabel = new HtmlGenericControl("label");
                targetLabel.Attributes["for"] = id;
                targetLabel.InnerText = string.Format("{0} ({1})", target.DisplayName, database.Name);
                PublishingTargets.Controls.Add(targetLabel);

                PublishingTargets.Controls.Add(new LiteralControl("<br/>"));
            }
        }

        /// <summary>
        /// Renders available publishing languages.
        /// </summary>
        private void BuildLanguages()
        {
            LanguageCollection languages = LanguageManager.GetLanguages(_database);
            if (languages == null)
            {
                Log.Warn("Scheduled Publish: No publish languages found", this);
                return;
            }

            foreach (var language in languages)
            {
                if (!CanWriteLanguage(language))
                {
                    continue;
                }

                string id = Control.GetUniqueID("lang_");

                HtmlGenericControl langInput = new HtmlGenericControl("input");
                langInput.ID = id;
                langInput.Attributes["type"] = "checkbox";
                langInput.Attributes["value"] = language.Name;
                if (languages.Count == 1)
                {
                    langInput.Attributes["checked"] = "checked";
                }
                Languages.Controls.Add(langInput);

                HtmlGenericControl langLabel = new HtmlGenericControl("label");
                langLabel.Attributes["for"] = id;
                langLabel.InnerText = language.CultureInfo.DisplayName;
                Languages.Controls.Add(langLabel);

                Languages.Controls.Add(new LiteralControl("<br/>"));
            }
        }

        /// <summary>
        /// Checks if the user has write permissions for the selected language.
        /// </summary>
        /// <param name="language">The language to check permissons on.</param>
        /// <returns>True if the user has write permissons for the language.</returns>
        private bool CanWriteLanguage(Language language)
        {
            if (language == null)
            {
                return false;
            }

            bool renderLanguage = true;

            if (Settings.CheckSecurityOnLanguages)
            {
                ID languageItemId = LanguageManager.GetLanguageItemId(language, _database);
                if (!ItemUtil.IsNull(languageItemId))
                {
                    Item languageItem = _database.GetItem(languageItemId);
                    if (languageItem == null)
                    {
                        renderLanguage = false;
                    }

                    bool canWriteLanguage = AuthorizationManager.IsAllowed(languageItem, AccessRight.LanguageWrite, Context.User);
                    if (!canWriteLanguage)
                    {
                        renderLanguage = false;
                    }
                }
                else
                {
                    renderLanguage = false;
                }
            }

            return renderLanguage;
        }

        /// <summary>
        /// Displays a list of all already scheduled publishes for this item, ordered from most recent to furthest in time.
        /// </summary>
        private void BuildExistingSchedules()
        {
            IEnumerable<PublishSchedule> schedules = _scheduledPublishRepo.GetSchedules(InnerItem.ID).ToArray();

            string schedulesHtml = BuildExistingSchedulesHtml(schedules);

            ExistingSchedules.InnerHtml = string.IsNullOrWhiteSpace(schedulesHtml)
                ? Constants.NO_EXISTING_SCHEDULES_TEXT
                : schedulesHtml;
        }

        /// <summary>
        /// Sets appropriate titles for the different sections in the Schedule Publish dialog.
        /// </summary>
        private void BuildUnpublishTitles()
        {
            ScheduleSettings.Header = Constants.SCHEDULE_UNPUBLISH_SETTINGS_TITLE;
            ScheduleLanguages.Header = Constants.SCHEDULE_UNPUBLISH_LANGUAGES_TITLE;
            ScheduleTargets.Header = Constants.SCHEDULE_UNPUBLISH_TARGETS_TITLE;
            PublishTimeLit.Text = Constants.SCHEDULE_DATETIMEPICKER_UNPUBLISH_TITLE;
        }

        /// <summary>
        /// Builds Existing Schedules table.
        /// </summary>
        /// <param name="schedules">Existing Schedules</param>
        /// <returns>Html table</returns>
        private string BuildExistingSchedulesHtml(IEnumerable<PublishSchedule> schedules)
        {
            if (schedules == null)
            {
                return string.Empty;
            }

            PublishSchedule[] schedulesArray = schedules.ToArray();
            if (schedulesArray.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sbTable = new StringBuilder(100);

            sbTable.Append("<table width=\"100%\">");
            sbTable.Append("<tr>");
            sbTable.Append("<td nowrap=\"nowrap\">Time</td>");
            sbTable.Append("<td nowrap=\"nowrap\">Action</td>");
            sbTable.Append("<td nowrap=\"nowrap\">Languages</td>");
            sbTable.Append("<td nowrap=\"nowrap\">Version</td>");
            sbTable.Append("<td nowrap=\"nowrap\">Recurrence</td>");
            sbTable.Append("</tr>");
            foreach (var schedule in schedulesArray)
            {
                string version;

                if (schedule.ItemToPublish != null)
                {
                    Item itemInVersion = schedule.ItemToPublish.Publishing.GetValidVersion(schedule.PublishDate, true, false);
                    if (itemInVersion != null)
                    {
                        sbTable.Append("<tr>");
                        version = itemInVersion.Version.Number.ToString();
                    }
                    else
                    {
                        sbTable.Append("<tr style='color: red'>");
                        version = Constants.NO_VALID_VERSION_TEXT;
                    }
                }
                else
                {
                    sbTable.Append("<tr>");
                    version = Constants.WEBSITE_PUBLISH_TEXT;
                }

                sbTable.AppendFormat("<td nowrap=\"nowrap\">{0}</td>", schedule.PublishDate.ToString(_culture));
                sbTable.AppendFormat("<td nowrap=\"nowrap\">{0}</td>", schedule.Unpublish ? Constants.UNPUBLISH_TEXT : Constants.PUBLISH_TEXT);
                sbTable.AppendFormat("<td nowrap=\"nowrap\">{0}</td>", string.Join(",", schedule.TargetLanguages.Select(x => x.Name)).TrimEnd(','));
                sbTable.AppendFormat("<td nowrap=\"nowrap\">{0}</td>", version);
                sbTable.AppendFormat("<td nowrap=\"nowrap\">{0}</td>", DialogsHelper.GetRecurrenceMessage(schedule.RecurrenceType, schedule.HoursToNextPublish));
                sbTable.Append("</tr>");
            }

            sbTable.Append("</table>");

            return sbTable.ToString();
        }
    }
}