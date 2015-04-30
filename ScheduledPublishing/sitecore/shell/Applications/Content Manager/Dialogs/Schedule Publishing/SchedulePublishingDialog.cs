using ScheduledPublishing.Models;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Security.AccessControl;
using Sitecore.SecurityModel;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using ScheduledPublishing.Utils;
using Sitecore.Web.UI.WebControls;
using Constants = ScheduledPublishing.Utils.Constants;
using Control = Sitecore.Web.UI.HtmlControls.Control;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace ScheduledPublishing.sitecore.shell.Applications.ContentManager.Dialogs
{
    /// <summary>
    /// Schedule Publishing code-beside
    /// </summary>
    public class SchedulePublishingDialog : DialogForm
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
        protected Radiobutton SmartPublish;
        protected Radiobutton Republish;
        protected DateTimePicker PublishDateTimePicker;

        private const string SCHEDULE_UNPUBLISH_SETTINGS_TITLE = "Scheduled Unpublish Settings";
        private const string SCHEDULE_UNPUBLISH_LANGUAGES_TITLE = "Scheduled Unpublish Languages";
        private const string SCHEDULE_UNPUBLISH_TARGETS_TITLE = "Scheduled Unpublish Targets";
        private const string SCHEDULE_DATETIMEPICKER_UNPUBLISH_TITLE = "Unpiblish Time:";
        private const string CURREN_TIME_ON_SERVER_TEXT = "Current time on server: ";
        private readonly Database _database = Context.ContentDatabase;

        private Item _innerItem;
        private Item InnerItem
        {
            get
            {
                if (_innerItem != null)
                {
                    return this._innerItem;
                }

                this._innerItem = UIUtil.GetItemFromQueryString(_database);
                return this._innerItem;
            }
        }

        private DateTime _selectedPublishDataTite;
        private DateTime SelectedPublishDateTime
        {
            get
            {
                if (this._selectedPublishDataTite != DateTime.MinValue)
                {
                    return this._selectedPublishDataTite;
                }

                this._selectedPublishDataTite =
                    DateUtil.IsoDateToDateTime(this.PublishDateTimePicker.Value, DateTime.MinValue);
                return this._selectedPublishDataTite;
            }
        }

        private IEnumerable<Database> _selectedTargets;
        private IEnumerable<Database> SelectedTargets
        {
            get
            {
                if (this._selectedTargets != null && this._selectedTargets.Any())
                {
                    return this._selectedTargets;
                }

                var targetItems = new List<Item>();

                foreach (string str in Context.ClientPage.ClientRequest.Form.Keys)
                {
                    if (str != null && str.StartsWith("pt_", StringComparison.InvariantCulture))
                    {
                        var target = _database.Items[ShortID.Decode(str.Substring(3))];
                        Assert.IsNotNull(target, "Publish target not found.");
                        targetItems.Add(target);
                    }
                }

                this._selectedTargets =
                    targetItems.Select(t => Database.GetDatabase(t[FieldIDs.PublishingTargetDatabase]));
                return this._selectedTargets;
            }
        }

        private IEnumerable<Language> _selectedLanguages;
        private IEnumerable<Language> SelectedLanguages
        {
            get
            {
                if (this._selectedLanguages != null && this._selectedLanguages.Any())
                {
                    return this._selectedLanguages;
                }

                var languages = new List<Language>();
                foreach (string index in Context.ClientPage.ClientRequest.Form.Keys)
                {
                    if (index != null && index.StartsWith("lang_", StringComparison.InvariantCulture))
                    {
                        var language = LanguageManager.GetLanguage(Context.ClientPage.ClientRequest.Form[index]);
                        Assert.IsNotNull(language, "Publish language not found.");
                        languages.Add(language);
                    }
                }

                this._selectedLanguages = languages;
                return languages;
            }
        }

        private Item _publishingSchedulesFolder;
        private Item PublishingSchedulesFolder
        {
            get
            {
                if (this._publishingSchedulesFolder != null)
                {
                    return this._publishingSchedulesFolder;
                }

                this._publishingSchedulesFolder = _database.GetItem(Constants.PUBLISH_OPTIONS_FOLDER_ID);
                if (this._publishingSchedulesFolder == null)
                {
                    Error.AssertItemFound(this._publishingSchedulesFolder);
                }

                return this._publishingSchedulesFolder;
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

            if (!Context.ClientPage.IsEvent)
            {
                if (Unpublish)
                {
                    this.BuildUnpublishTitles();
                    this.PublishModePanel.Visible = false;
                }

                this.BuildExistingSchedules();
                this.BuildPublishingTargets();
                this.BuildLanguages();

                this.ServerTime.Text = CURREN_TIME_ON_SERVER_TEXT + DateTime.Now;
                this.SmartPublish.Checked = true;
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

            if (!this.ValidateSchedule())
            {
                return;
            }

            this.CreatePublishOptionsItem();

            base.OnOK(sender, args);
        }

        /// <summary>
        /// Renders available publishing targets.
        /// </summary>
        private void BuildPublishingTargets()
        {
            var publishingTargets = PublishManager.GetPublishingTargets(_database);
            if (publishingTargets == null)
            {
                Log.Info("No publish targets found", this);
                return;
            }

            foreach (Item target in publishingTargets)
            {
                var id = string.Format("pt_{0}", ShortID.Encode(target.ID));
                var database = Database.GetDatabase(target[FieldIDs.PublishingTargetDatabase]);
                if (database == null)
                {
                    continue;
                }

                var targetInput = new HtmlGenericControl("input");
                targetInput.ID = id;
                targetInput.Attributes["type"] = "checkbox";
                targetInput.Disabled = !target.Access.CanWrite();
                this.PublishingTargets.Controls.Add(targetInput);

                var targetLabel = new HtmlGenericControl("label");
                targetLabel.Attributes["for"] = id;
                targetLabel.InnerText = string.Format("{0} ({1})", target.DisplayName, database.Name);
                this.PublishingTargets.Controls.Add(targetLabel);

                this.PublishingTargets.Controls.Add(new LiteralControl("<br>"));
            }
        }

        /// <summary>
        /// Renders available publishing languages.
        /// </summary>
        private void BuildLanguages()
        {
            var languages = LanguageManager.GetLanguages(_database);
            if (languages == null)
            {
                Log.Info("No publish languages found", this);
                return;
            }

            foreach (var language in languages)
            {
                if (!this.CanWriteLanguage(language))
                {
                    continue;
                }

                var id = Control.GetUniqueID("lang_");

                var langInput = new HtmlGenericControl("input");
                langInput.ID = id;
                langInput.Attributes["type"] = "checkbox";
                langInput.Attributes["value"] = language.Name;
                this.Languages.Controls.Add(langInput);

                var langLabel = new HtmlGenericControl("label");
                langLabel.Attributes["for"] = id;
                langLabel.InnerText = language.CultureInfo.DisplayName;
                this.Languages.Controls.Add(langLabel);

                this.Languages.Controls.Add(new LiteralControl("<br>"));
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
                var languageItemId = LanguageManager.GetLanguageItemId(language, _database);
                if (!ItemUtil.IsNull(languageItemId))
                {
                    var languageItem = _database.GetItem(languageItemId);
                    if (languageItem == null)
                    {
                        renderLanguage = false;
                    }

                    var canWriteLanguage = AuthorizationManager.IsAllowed(languageItem, AccessRight.LanguageWrite, Context.User);
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
        /// <param name="item">The item that publishing is scheduled for</param>
        private void BuildExistingSchedules()
        {
            if (!this.PublishingSchedulesFolder.Children.Any())
            {
                return;
            }

            var existingSchedules =
                ScheduledPublishOptionsManager.GetScheduledOptions(PublishingSchedulesFolder, InnerItem.ID).ToList();

            if (existingSchedules.Any())
            {
                ExistingSchedulesDiv.Visible = false;
                foreach (var schedule in existingSchedules)
                {
                    var time = DateUtil.IsoDateToDateTime(schedule.PublishDateString);
                    var timeLit = new Literal();
                    timeLit.Text = time.ToString(Context.Culture);
                    ExistingSchedulesTable.Controls.Add(timeLit);

                    var action = schedule.Unpublish ? "Unpublish" : "Publish";
                    var actionLit = new Literal();
                    actionLit.Text = action;
                    ExistingSchedulesTable.Controls.Add(actionLit);

                    var languages = string.Join(",", schedule.Languages.Select(la => la.Name)).TrimEnd(',');
                    var languagesLit = new Literal();
                    languagesLit.Text = languages;
                    ExistingSchedulesTable.Controls.Add(languagesLit);

                    var version = schedule.ItemToPublish.Publishing.GetValidVersion(time, true, false);
                    var versionLit = new Literal();
                    versionLit.Text = version.Version.Number.ToString();
                    ExistingSchedulesTable.Controls.Add(versionLit);
                }
            }
            else
            {
                ExistingSchedulesTable.Visible = false;
                ExistingSchedulesDiv.InnerHtml = "This item has not been scheduled for publishing yet.";
            }
        }

        #region Validations

        private bool ValidateSchedule()
        {
            var isValid =
                (this.ValidateSelectedDate()
                 && this.ValidateSelectedPublishTargets()
                 && this.ValidateSelectedLanguages());

            return Unpublish
                ? isValid
                : isValid && this.ValidatePublishable();
        }

        /// <summary>
        /// Checks whether the item is publishable in the selected time.
        /// </summary>
        /// <returns>True if the item meets all date and time requirements for publishing</returns>
        private bool ValidatePublishable()
        {
            //we should also check ancestors because if any ancestor is marked for unpublish
            //our item will be also unpublished instead of published
            //IsValid added for workflow state and DateTime range (Valid From/Valid To)
            if (this.InnerItem != null
                && !this.InnerItem.Publishing.IsPublishable(this.SelectedPublishDateTime, true)
                && !this.InnerItem.Publishing.IsValid(this.SelectedPublishDateTime, true))
            {
                SheerResponse.Alert("Item is not publishable at that time.");
                return false;
            }

            return true;
        }

        private bool ValidateSelectedDate()
        {
            if (DateTime.Compare(this.SelectedPublishDateTime, DateTime.Now) <= 0)
            {
                SheerResponse.Alert("The date selected for publish has passed. Please select a future date.");
                return false;
            }

            return true;
        }

        private bool ValidateSelectedPublishTargets()
        {
            if (this.SelectedTargets == null || !this.SelectedTargets.Any())
            {
                SheerResponse.Alert("Please select at least one publish target.");
                return false;
            }

            return true;
        }

        private bool ValidateSelectedLanguages()
        {
            if (this.SelectedLanguages == null || !this.SelectedLanguages.Any())
            {
                SheerResponse.Alert("Please select at least one publish language.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if item has validator errors
        /// </summary>
        /// <returns>Always true if we want to be able to publish with validator errors. isValid if we want validator errors to prevent publishing.</returns>
        //private bool ValidateItemValidators()
        //{
        //    this.InnerItem.Fields.ReadAll();
        //    ValidatorCollection validators = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.Workflow, this.InnerItem);
        //    ValidatorCollection validatorsBar = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.ValidatorBar, this.InnerItem);
        //    ValidatorCollection validatorsButton = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.ValidateButton, this.InnerItem);
        //    ValidatorCollection validatorsGutter = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.Gutter, this.InnerItem);
        //    var options = new ValidatorOptions(true);
        //    ValidatorManager.Validate(validatorsBar, options);
        //    foreach (BaseValidator validator in validatorsBar)
        //    {
        //        if (validator.Result != ValidatorResult.Valid)
        //        {
        //            SheerResponse.Alert("This item has validation errors. You may want to review them and schedule another publish.");
        //            return false;
        //        }
        //    }

        //    return true;
        //}

        #endregion

        private void CreatePublishOptionsItem()
        {
            var unpublish = Unpublish;
            var action = unpublish ? "unpublish" : "publish";

            try
            {
                using (new SecurityDisabler())
                {
                    var publishOptionsTemplate = this._database.GetTemplate(Constants.PUBLISH_OPTIONS_TEMPLATE_ID);
                    var publishOptionsName = BuildPublishOptionsName(this.InnerItem);
                    var optionsFolder = Utils.Utils.GetOrCreateFolder(this.SelectedPublishDateTime, _database);
                    ScheduledPublishOptions newPublishOptions = new ScheduledPublishOptions(optionsFolder.Add(publishOptionsName, publishOptionsTemplate));

                    newPublishOptions.InnerItem.Editing.BeginEdit();

                    newPublishOptions.SchedulerEmail = Context.User.Profile.Email;
                    newPublishOptions.Unpublish = unpublish;
                    if (this.InnerItem != null)
                    {
                        newPublishOptions.ItemToPublishPath = this.InnerItem.Paths.FullPath;
                    }
                    newPublishOptions.PublishModeString = this.SmartPublish.Checked //TODO - smart full incremental
                        ? PublishMode.Smart.ToString()
                        : PublishMode.Full.ToString();
                    newPublishOptions.PublishChildren = this.PublishChildren.Checked;
                    newPublishOptions.Languages = this.SelectedLanguages.ToArray(); //TODO: check (string.Join("|", this.SelectedLanguages.Select(x => x.Name))
                    newPublishOptions.SourceDatabaseString = this._database.Name;
                    newPublishOptions.TargetDatabasesString = string.Join("|", this.SelectedTargets.Select(x => x.Name));
                    newPublishOptions.PublishDateString = DateUtil.ToIsoDate(this.SelectedPublishDateTime);

                    newPublishOptions.InnerItem.Editing.AcceptChanges();
                    newPublishOptions.InnerItem.Editing.EndEdit();

                    Log.Info(
                        string.Format("Created Publish Options: {0}: {1} {2} {3}",
                            action,
                            this.InnerItem != null ? this.InnerItem.Name : "Website",
                            this.InnerItem != null ? this.InnerItem.ID.ToString() : "Website",
                            DateTime.Now), this);
                }
            }
            catch (Exception ex)
            {
                Log.Info(
                    string.Format("Failed creating Publish Options: {0}: {1} {2} {3}",
                        action,
                        this.InnerItem != null ? this.InnerItem.Name : "Website",
                        this.InnerItem != null ? this.InnerItem.ID.ToString() : "Website",
                        ex), this);
            }
        }

        private static string BuildPublishOptionsName(Item item)
        {
            var guid = item != null
                ? item.ID.Guid
                : Guid.NewGuid();

            return ItemUtil.ProposeValidItemName(string.Format("{0}ScheduledPublishOptions", guid));
        }

        private void BuildUnpublishTitles()
        {
            this.ScheduleSettings.Header = SCHEDULE_UNPUBLISH_SETTINGS_TITLE;
            this.ScheduleLanguages.Header = SCHEDULE_UNPUBLISH_LANGUAGES_TITLE;
            this.ScheduleTargets.Header = SCHEDULE_UNPUBLISH_TARGETS_TITLE;
            this.PublishTimeLit.Text = SCHEDULE_DATETIMEPICKER_UNPUBLISH_TITLE;
        }
    }
}