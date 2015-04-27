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
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
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
        private readonly Database _database = Context.ContentDatabase;

        protected DateTimePicker PublishDateTimePicker;
        protected Border ExistingSchedules;
        protected Literal ServerTime;
        protected Literal PublishTimeLit;
        protected Border PublishingTargets;
        protected Border Languages;
        protected Checkbox PublishChildren;
        protected Radiobutton SmartPublish;
        protected Radiobutton Republish;

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

                this._selectedPublishDataTite = DateUtil.IsoDateToDateTime(this.PublishDateTimePicker.Value,
                    DateTime.MinValue);
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

                this._publishingSchedulesFolder = _database.GetItem(Utils.Constants.PUBLISH_OPTIONS_FOLDER_ID);
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
                this.SmartPublish.Checked = true;

                this.ServerTime.Text = "Current time on server: " + DateTime.Now;

                this.PublishTimeLit.Text = Unpublish ? "Unpiblish Time: " : "Publish Time: ";

                this.BuildExistingSchedules();
                this.BuildPublishingTargets();
                this.BuildLanguages();
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

            var isValidSchedule = this.ValidateSchedule();
            if (isValidSchedule)
            {
                this.CreatePublishOptionsItem();
            }

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
                if (Settings.CheckSecurityOnLanguages)
                {
                    var languageItemId = LanguageManager.GetLanguageItemId(language, _database);
                    if (!ItemUtil.IsNull(languageItemId))
                    {
                        var languageItem = _database.GetItem(languageItemId);
                        if (languageItem == null || !CanWriteLanguage(languageItem))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
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

        private static bool CanWriteLanguage(ISecurable item)
        {
            return AuthorizationManager.IsAllowed(item, AccessRight.LanguageWrite, Context.User);
        }

        /// <summary>
        /// Displays a list of all already scheduled publishings' date and time for this item, ordered from most recent to furthest in time
        /// </summary>
        /// <param name="item">The item that publishing is scheduled for</param>
        private void BuildExistingSchedules()
        {
            if (this.PublishingSchedulesFolder.Children == null)
            {
                return;
            }

            var publishingTaskName = BuildPublishOptionsName(this.InnerItem);
            var existingSchedules =
                this.PublishingSchedulesFolder.Axes.GetDescendants()
                    .Where(x => x.Name == publishingTaskName)
                    .Select(x => x[Constants.PUBLISH_OPTIONS_SCHEDULED_DATE].ToString());

            existingSchedules = existingSchedules.OrderBy(DateTime.Parse);

            var sbExistingSchedules = new StringBuilder();
            if (existingSchedules.Any())
            {
                foreach (var existingSchedule in existingSchedules)
                {
                    sbExistingSchedules.Append("<div style=\"padding:0px 0px 2px 0px; width=100%;\">" + existingSchedule +
                                               "</div>");
                    sbExistingSchedules.Append("<br />");
                }
            }
            else
            {
                sbExistingSchedules.Append("<div style=\"padding:0px 0px 2px 0px; width=100%;\">" +
                                           "This item has not been scheduled for publishing yet." + "</div>");
                sbExistingSchedules.Append("<br />");
            }

            this.ExistingSchedules.InnerHtml = sbExistingSchedules.ToString();
        }

        #region Validations

        private bool ValidateSchedule()
        {
            var isValid =
                (this.ValidatePublishable()
                 && this.ValidateSelectedDate()
                 && this.ValidateSelectedPublishTargets()
                 && this.ValidateSelectedLanguages());

            return isValid;
        }

        /// <summary>
        /// Checks whether the item is publishable in the selected time.
        /// </summary>
        /// <returns>True if the item meets all date and time requirements for publishing</returns>
        private bool ValidatePublishable()
        {
            if (this.InnerItem != null 
                && !this.InnerItem.Publishing.IsPublishable(this.SelectedPublishDateTime, false))
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
                    var optionsFolder = this.GetOrCreateFolder(this.SelectedPublishDateTime);
                    var newPublishOptions = optionsFolder.Add(publishOptionsName, publishOptionsTemplate);

                    newPublishOptions.Editing.BeginEdit();

                    newPublishOptions[Constants.PUBLISH_OPTIONS_CREATED_BY_EMAIL] = Context.User.Profile.Email;
                    newPublishOptions[Constants.PUBLISH_OPTIONS_UNPUBLISH] = unpublish ? "1" : string.Empty;
                    if (this.InnerItem != null)
                    {
                        newPublishOptions[Constants.PUBLISH_OPTIONS_PUBLISH_ITEM] = this.InnerItem.Paths.FullPath;
                    }
                    newPublishOptions[Constants.PUBLISH_OPTIONS_PUBLISH_MODE] = this.SmartPublish.Checked
                        ? "smart"
                        : "republish";
                    newPublishOptions[Constants.PUBLISH_OPTIONS_PUBLISH_CHILDREN] = this.PublishChildren.Checked
                        ? "1"
                        : string.Empty;
                    newPublishOptions[Constants.PUBLISH_OPTIONS_TARGET_LANGUAGES] = string.Join("|",
                        this.SelectedLanguages.Select(x => x.Name));
                    newPublishOptions[Constants.PUBLISH_OPTIONS_SOURCE_DATABASE] = this._database.Name;
                    newPublishOptions[Constants.PUBLISH_OPTIONS_TARGET_DATABASES] = string.Join("|",
                        this.SelectedTargets.Select(x => x.Name));

                    newPublishOptions.Editing.AcceptChanges();
                    newPublishOptions.Editing.EndEdit();

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

        /// <summary>
        /// Get appropriate hour folder or create one if not present using the year/month/day/hour structure
        /// </summary>
        /// <param name="date">Date chosen for publishing</param>
        /// <returns>The hour folder as an item</returns>
        private Item GetOrCreateFolder(DateTime date)
        {
            Item publishOptionsFolder = _database.GetItem(Utils.Constants.PUBLISH_OPTIONS_FOLDER_ID);
            string yearName = date.Year.ToString();
            string monthName = date.Month.ToString();
            string dayName = date.Day.ToString();
            string hourName = date.AddHours(1).Hour.ToString();

            TemplateItem folderTemplate = _database.GetTemplate(Utils.Constants.FOLDER_TEMPLATE_ID);
            Item yearFolder = publishOptionsFolder.Children.FirstOrDefault(x => x.Name == yearName) ??
                              publishOptionsFolder.Add(yearName, folderTemplate);


            Item monthFolder = yearFolder.Children.FirstOrDefault(x => x.Name == monthName) ??
                               yearFolder.Add(monthName, folderTemplate);

            Item dayFolder = monthFolder.Children.FirstOrDefault(x => x.Name == dayName) ??
                             monthFolder.Add(dayName, folderTemplate);

            Item hourFolder = dayFolder.Children.FirstOrDefault(x => x.Name == hourName) ??
                              dayFolder.Add(hourName, folderTemplate);

            return hourFolder;
        }

        private static string BuildPublishOptionsName(Item item)
        {
            var guid = item != null
                ? item.ID.Guid
                : Guid.NewGuid();

            return ItemUtil.ProposeValidItemName(string.Format("{0}ScheduledPublishOptions", guid));
        }
    }
}