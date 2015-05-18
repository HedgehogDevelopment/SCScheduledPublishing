using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
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
using Constants = ScheduledPublish.Utils.Constants;
using Control = Sitecore.Web.UI.HtmlControls.Control;
using ItemList = Sitecore.Collections.ItemList;

namespace ScheduledPublish.sitecore.shell.Applications.Content_Manager.Dialogs.Schedule_Publish
{
    /// <summary>
    /// Schedule Publishing code-beside
    /// </summary>
    public class SchedulePublishDialog : DialogForm
    {
        protected Groupbox ScheduleSettings;
        protected Groupbox ScheduleLanguages;
        protected Groupbox ScheduleTargets;
        protected Border ExistingSchedulesDiv;
        protected GridPanel ExistingSchedulesTable;
        protected Border Languages;
        protected Border PublishModePanel;
        protected Border PublishingTargets;
        protected Literal ServerTime;
        protected Literal PublishTimeLit;
        protected Checkbox PublishChildren;
        protected Checkbox PublishRelatedItems;
        protected Radiobutton SmartPublish;
        protected Radiobutton Republish;
        protected DateTimePicker PublishDateTimePicker;

        private readonly Database _database = Context.ContentDatabase;
        private readonly CultureInfo _culture = Context.Culture;
        private ScheduledPublishRepo scheduledPublishRepo;

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

        private static bool Unpublish
        {
            get
            {
                bool unpublish;
                bool.TryParse(WebUtil.GetQueryString("unpublish"), out unpublish);

                return unpublish;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");

            scheduledPublishRepo = new ScheduledPublishRepo();

            if (!Context.ClientPage.IsEvent)
            {
                if (Unpublish)
                {
                    PublishModePanel.Visible = false;
                    ScheduleLanguages.Visible = false;
                    BuildUnpublishTitles();
                }

                BuildExistingSchedules();
                BuildPublishingTargets();
                BuildLanguages();

                ServerTime.Text = Constants.CURREN_TIME_ON_SERVER_TEXT + DateTime.Now.ToString(_culture);
                SmartPublish.Checked = true;
            }

            base.OnLoad(e);
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
                SchedulerEmail = Context.User.Profile.Email,
                IsPublished = false
            };

            ValidationResult validationResult = ScheduledPublishValidator.Validate(publishSchedule);
            if (!validationResult.IsValid)
            {
                SheerResponse.Alert(string.Join(Environment.NewLine, validationResult.ValidationErrors));
                return;
            }

            scheduledPublishRepo.CreatePublishSchedule(publishSchedule);

            base.OnOK(sender, args);
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
        /// Displays a list of all already scheduled publishings' date and time for this item, ordered from most recent to furthest in time
        /// </summary>
        private void BuildExistingSchedules()
        {
            IEnumerable<PublishSchedule> existingSchedules = scheduledPublishRepo.GetSchedules(InnerItem.ID).ToList();

            if (existingSchedules.Any())
            {
                ExistingSchedulesDiv.Visible = false;
                foreach (var schedule in existingSchedules)
                {
                    DateTime time = schedule.PublishDate;
                    Literal timeLit = new Literal();
                    timeLit.Text = time.ToString(_culture);
                    ExistingSchedulesTable.Controls.Add(timeLit);

                    string action = schedule.Unpublish ? Constants.UNPUBLISH_TEXT : Constants.PUBLISH_TEXT;
                    Literal actionLit = new Literal();
                    actionLit.Text = action;
                    ExistingSchedulesTable.Controls.Add(actionLit);

                    string languages = string.Join(",", schedule.TargetLanguages.Select(x => x.Name)).TrimEnd(',');
                    Literal languagesLit = new Literal();
                    languagesLit.Text = languages;
                    ExistingSchedulesTable.Controls.Add(languagesLit);

                    Literal versionLit = new Literal();
                    string version;
                    if (schedule.ItemToPublish == null)
                    {
                        version = Constants.WEBSITE_PUBLISH_TEXT;
                    }
                    else
                    {
                        Item itemInVersion = schedule.ItemToPublish.Publishing.GetValidVersion(time, true, false);
                        if (itemInVersion != null)
                        {
                            version = itemInVersion.Version.Number.ToString();
                        }
                        else
                        {
                            version = Constants.NO_VALID_VERSION_TEXT;

                            languagesLit.Style.Add("color", "red");
                            actionLit.Style.Add("color", "red");
                            timeLit.Style.Add("color", "red");
                            versionLit.Style.Add("color", "red");
                        }
                    }
                    versionLit.Text = version;
                    ExistingSchedulesTable.Controls.Add(versionLit);
                }
            }
            else
            {
                ExistingSchedulesTable.Visible = false;
                ExistingSchedulesDiv.InnerHtml = Constants.NO_EXISTING_SCHEDULES_TEXT;
            }
        }

        private void BuildUnpublishTitles()
        {
            ScheduleSettings.Header = Constants.SCHEDULE_UNPUBLISH_SETTINGS_TITLE;
            ScheduleLanguages.Header = Constants.SCHEDULE_UNPUBLISH_LANGUAGES_TITLE;
            ScheduleTargets.Header = Constants.SCHEDULE_UNPUBLISH_TARGETS_TITLE;
            PublishTimeLit.Text = Constants.SCHEDULE_DATETIMEPICKER_UNPUBLISH_TITLE;
        }
    }
}