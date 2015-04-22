using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.SecurityModel;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Sitecore.Security.AccessControl;
using Sitecore.Web;
using BaseValidator = Sitecore.Data.Validators.BaseValidator;
using Control = Sitecore.Web.UI.HtmlControls.Control;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;
using ValidatorCollection = Sitecore.Data.Validators.ValidatorCollection;

namespace ScheduledPublishing.sitecore.shell.Applications.ContentManager.Dialogs
{
    /// <summary>
    /// Schedule Publishing code-beside
    /// </summary>
    public class SchedulePublishingDialog : DialogForm
    {
        private const string SCHEDULE_PUBLISH_COMMAND_ID = "{EF235C25-AE83-4678-9E2C-C22175925893}";
        private readonly ID _publishingSchedulesRootId = ID.Parse("{60911399-8FF1-4978-8243-0CD7A60AC3D2}");
        private readonly ID _publishingScheduleTemplateId = ID.Parse("{9F110258-0139-4FC9-AED8-5610C13DADF3}");
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

        private DateTime _publishDataTite;
        private DateTime PublishDateTime
        {
            get
            {
                if (this._publishDataTite != DateTime.MinValue)
                {
                    return this._publishDataTite;
                }

                this._publishDataTite = DateUtil.IsoDateToDateTime(this.PublishDateTimePicker.Value, DateTime.MinValue);
                return this._publishDataTite;
            }
        }

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
                if (this._innerItem == null)
                {
                    Error.AssertItemFound(this._innerItem);
                }

                return this._innerItem;
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

                this._publishingSchedulesFolder = _database.GetItem(_publishingSchedulesRootId);
                if (this._publishingSchedulesFolder == null)
                {
                    Error.AssertItemFound(this._publishingSchedulesFolder);
                }

                return this._publishingSchedulesFolder;
            }
        }

        private bool IsPublishing
        {
            get
            {
                bool isPublishing;
                bool.TryParse(WebUtil.GetQueryString("unpunlidh"), out isPublishing);

                return isPublishing;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");

            if (!Context.ClientPage.IsEvent)
            {
                this.SmartPublish.Checked = true;

                this.ServerTime.Text = "Current time on server: " + DateTime.Now;

                this.PublishTimeLit.Text = this.IsPublishing ? "Unpiblish Time: " : "Publish Time: ";

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
                this.CreatePublishingTask();
            }

            base.OnOK(sender, args);
        }

        protected string JobHandle
        {
            get
            {
                return StringUtil.GetString(this.ServerProperties["JobHandle"]);
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                this.ServerProperties["JobHandle"] = (object)value;
            }
        }

        protected void StartPublisher()
        {
            Language[] languages = GetLanguages().ToArray();
            Database[] publishingTargetDatabases = GetPublishingTargetDatabases().ToArray();
            bool @checked = this.PublishChildren.Checked;
            string id = this.InnerItem.ID.ToString();
            bool isIncremental = Context.ClientPage.ClientRequest.Form["PublishMode"] == "IncrementalPublish";
            bool isSmart = Context.ClientPage.ClientRequest.Form["PublishMode"] == "SmartPublish";

            this.JobHandle = (string.IsNullOrEmpty(id)
                ? (!isIncremental
                    ? (!isSmart
                        ? PublishManager.Republish(Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language)
                        : PublishManager.PublishSmart(Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language))
                    : PublishManager.PublishIncremental(Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language))
                : PublishManager.PublishItem(Client.GetItemNotNull(id), publishingTargetDatabases, languages, @checked, isSmart)).ToString();
        }

        /// <summary>
        /// Renders available publishing targets.
        /// </summary>
        private void BuildPublishingTargets()
        {
            var publishingTargets = GetPublishingTargets();
            if (publishingTargets == null)
            {
                return;
            }

            foreach (Item target in publishingTargets)
            {
                var id = Control.GetUniqueID("pt_");
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
            var languages = this.GetLanguages();
            if (languages == null)
            {
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

            var publishingTaskName = GetPublishingTaskName(this.InnerItem.ID);
            var existingSchedules =
                this.PublishingSchedulesFolder.Children
                                         .Where(x => x.Name == publishingTaskName)
                                         .Select(x => DateUtil.IsoDateToDateTime(x["Schedule"].Substring(0, x["Schedule"].IndexOf('|'))).ToString(Context.Culture))
                                         .OrderBy(DateTime.Parse);

            var sbExistingSchedules = new StringBuilder();
            if (existingSchedules.Any())
            {
                foreach (var existingSchedule in existingSchedules)
                {
                    sbExistingSchedules.Append("<div style=\"padding:0px 0px 2px 0px; width=100%;\">" + existingSchedule + "</div>");
                    sbExistingSchedules.Append("<br />");
                }
            }
            else
            {
                sbExistingSchedules.Append("<div style=\"padding:0px 0px 2px 0px; width=100%;\">" + "This item has not been scheduled for publishing yet." + "</div>");
                sbExistingSchedules.Append("<br />");
            }

            this.ExistingSchedules.InnerHtml = sbExistingSchedules.ToString();
        }

        #region Validations

        private bool ValidateSchedule()
        {
            //TODO: Finish ValidateItemValidators
            var isValid = (ValidateChosenDate() && ValidatePublishable()  /* && ValidateItemValidators()*/);
            return isValid;
        }

        private bool ValidateChosenDate()
        {
            if (DateTime.Compare(this.PublishDateTime, DateTime.Now) <= 0)
            {
                SheerResponse.Alert("The date selected for publish has passed. Please select a future date.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the item is publishable in the selected time.
        /// </summary>
        /// <param name="itemFromQueryString">Item to publish</param>
        /// <returns></returns>
        private bool ValidatePublishable()
        {
            if (!this.InnerItem.Publishing.IsPublishable(this.PublishDateTime, false))
            {
                SheerResponse.Alert("Item is not publishable at that time.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if item has validator errors
        /// </summary>
        /// <param name="itemFromQueryString"></param>
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

        private void CreatePublishingTask()
        {
            var isPublishing = this.IsPublishing;
            var action = isPublishing ? "unpublishing" : "publishing";

            using (new SecurityDisabler())
            {
                var scheduleTaskTemplate = _database.GetTemplate(_publishingScheduleTemplateId);
                if (scheduleTaskTemplate == null)
                {
                    return;
                }

                var publishingTaskName = this.GetPublishingTaskName(this.InnerItem.ID);

                var newTask = this.PublishingSchedulesFolder.Add(publishingTaskName, scheduleTaskTemplate);

                try
                {
                    newTask.Editing.BeginEdit();

                    newTask["Command"] = SCHEDULE_PUBLISH_COMMAND_ID;
                    newTask["Items"] = this.InnerItem.Paths.FullPath;
                    newTask["CreatedByEmail"] = Context.User.Profile.Email;
                    newTask["Unpublish"] = isPublishing ? "1" : string.Empty;
                    newTask["PublishMethod"] = this.SmartPublish.Checked ? "smart" : "republish";
                    newTask["PublishChildren"] = this.PublishChildren.Checked ? "1" : string.Empty;
                    newTask["PublishLanguages"] = string.Join("|", GetLanguages().Select(x => x.Name));
                    newTask["TargetDatabase"] = string.Join("|", GetPublishingTargetDatabases().Select(x => x.Name));
                    newTask["Auto remove"] = "1";
                    newTask["Schedule"] = this.FormatTaskScheduledTime();

                    newTask.Editing.EndEdit();

                    Log.Info(
                        string.Format("Task scheduling {0}: {1} {2} {3}",
                                       action,
                                       this.InnerItem.Name,
                                       this.InnerItem.ID,
                                       DateTime.Now), this);
                }
                catch (Exception ex)
                {
                    newTask.Editing.CancelEdit();
                    Log.Info(
                        string.Format("Failed scheduling {0}: {1} {2} {3} {4}",
                                       action,
                                       this.InnerItem.Name,
                                       this.InnerItem.ID,
                                       DateTime.Now,
                                       ex), this);
                }
            }
        }

        private string FormatTaskScheduledTime()
        {
            const string format = "yyyyMMddTHHmmss";

            return string.Format("{0}|{1}|127|00:60:00",
                                  this.PublishDateTime.ToString(format),
                                  this.PublishDateTime.AddHours(1).AddMinutes(1).ToString(format));
        }

        private IEnumerable<Language> GetLanguages()
        {
            var languages = LanguageManager.GetLanguages(_database);
            if (languages == null)
            {
                Log.Info("No publishing languages found", this);
                return Enumerable.Empty<Language>();
            }

            return languages;
        }

        private IEnumerable<Item> GetPublishingTargets()
        {
            var publishingTargets = PublishManager.GetPublishingTargets(_database);
            if (publishingTargets == null)
            {
                Log.Info("No publishing targets found", this);
                return Enumerable.Empty<Item>();

            }

            return publishingTargets;
        }

        private IEnumerable<Database> GetPublishingTargetDatabases()
        {
            var targets = GetPublishingTargets();
            if (targets == null)
            {
                return Enumerable.Empty<Database>();
            }

            return targets.Select(t => Database.GetDatabase(t[FieldIDs.PublishingTargetDatabase]));
        }

        private string GetPublishingTaskName(ID id)
        {
            return ItemUtil.ProposeValidItemName(string.Format("{0}_Task", id));
        }
    }
}