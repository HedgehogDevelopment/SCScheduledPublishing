using Sitecore;
using Sitecore.Collections;
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
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
        protected DateTimePicker PublishDateTime;
        protected Border ExistingSchedules;
        protected Literal ServerTime;
        protected Literal PublishTimeLit;
        protected Border PublishingTargets;
        protected Border Languages;
        protected Checkbox PublishChildren;
        protected Radiobutton SmartPublish;
        protected Radiobutton Republish;
        private readonly string ScheduleTemplateID = "{9F110258-0139-4FC9-AED8-5610C13DADF3}";// "{70244923-FA84-477C-8CBD-62F39642C42B}";
        private readonly string SchedulesFolderPath = "/sitecore/System/Tasks/Custom Schedules/";

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            
            if (!Context.ClientPage.IsEvent)
            {
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
                Error.AssertItemFound(itemFromQueryString);
                ServerTime.Text = "Current time on server: " + DateTime.Now;
                RenderExistingSchedules(itemFromQueryString);
                bool isUnpublishing= bool.Parse(Context.Request.QueryString["unpublish"]);
                PublishTimeLit.Text = isUnpublishing ? "Unpiblish Time: " : "Publish Time: ";
                this.SmartPublish.Checked = true;
                BuildPublishingTargets();
                BuildLanguages();
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
            //StartPublisher();
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            bool isUnpublishing = bool.Parse(Context.Request.QueryString["unpublish"]);
            Error.AssertItemFound(itemFromQueryString);

            if (!string.IsNullOrEmpty(this.PublishDateTime.Value))
            {
                SchedulePublishing(itemFromQueryString, isUnpublishing);
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
            Language[] languages = GetLanguages();
            Database[] publishingTargetDatabases = GetPublishingTargetDatabases();
            bool @checked = this.PublishChildren.Checked;
            string id = UIUtil.GetItemFromQueryString(Context.ContentDatabase).ID.ToString();
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
            Item publishingTargets = Context.ContentDatabase.Items["/sitecore/system/publishing targets"];
            if (publishingTargets == null || !publishingTargets.HasChildren)
            {
                Log.Info("No publishing targets found", this);
            }
            
            foreach (Item target in publishingTargets.Children)
            {
                string id = "pb_" + ShortID.Encode(target.ID);
                HtmlGenericControl targetInput = new HtmlGenericControl("input");
                this.PublishingTargets.Controls.Add(targetInput);
                targetInput.Attributes["type"] = "checkbox";
                targetInput.ID = id;
                targetInput.Disabled = !target.Access.CanWrite();
                HtmlGenericControl targetLabel = new HtmlGenericControl("label");
                this.PublishingTargets.Controls.Add(targetLabel);
                targetLabel.Attributes["for"] = id;
                targetLabel.InnerText = target.DisplayName;
                this.PublishingTargets.Controls.Add(new LiteralControl("<br>"));
            }
        }

        /// <summary>
        /// Renders available publishing languages.
        /// </summary>
        private void BuildLanguages()
        {
            LanguageCollection languages = LanguageManager.GetLanguages(Context.ContentDatabase);
            foreach (var language in languages)
            {
                if (Settings.CheckSecurityOnLanguages)
                {
                    ID languageItemId = LanguageManager.GetLanguageItemId(language, Context.ContentDatabase);
                    Assert.IsNotNull(languageItemId, "languageItemId");
                    Item lang = Context.ContentDatabase.GetItem(languageItemId);
                    Assert.IsNotNull(lang, "lang");
                }

                string uniqueId = Control.GetUniqueID("la_");
                uniqueId += language.Name;
                HtmlGenericControl langInput = new HtmlGenericControl("input");
                this.Languages.Controls.Add(langInput);
                langInput.Attributes["type"] = "checkbox";
                langInput.ID = uniqueId;
                langInput.Attributes["value"] = language.Name;
                HtmlGenericControl langLabel = new HtmlGenericControl("label");
                this.Languages.Controls.Add(langLabel);
                langLabel.Attributes["for"] = uniqueId;
                langLabel.InnerText = language.CultureInfo.DisplayName;
                this.Languages.Controls.Add(new LiteralControl("<br>"));
            }
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
            existingSchedules = existingSchedules.OrderBy(DateTime.Parse);
            
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
      
        /// <summary>
        /// Create a task to invoke publishing command at specific time
        /// </summary>
        /// <param name="itemFromQueryString">Item to be published</param>
        /// <param name="isUnpublishing">If the item is to be unpublished instead of published</param>
        private void SchedulePublishing(Item itemFromQueryString, bool isUnpublishing)
        {
            // Validate date chosen
            bool isDateValid = ValidateDateChosen();

            // Validate item to be published
            bool isItemValid = ValidateItemValidators(itemFromQueryString);

            //Validate if item is publishable
            bool isPublishable = ValidatePublishable(itemFromQueryString);

            // Create publishing task
            if (isDateValid && isItemValid && isPublishable)
            {
                CreatePublishingTask(itemFromQueryString, isUnpublishing);
            }
        }

        /// <summary>
        /// Checks whether the item is publishable in the selected time.
        /// </summary>
        /// <param name="itemFromQueryString">Item to publish</param>
        /// <returns></returns>
        private bool ValidatePublishable(Item itemFromQueryString)
        {
            if (!itemFromQueryString.Publishing.IsPublishable(
                DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue), false))
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
        private bool ValidateItemValidators(Item itemFromQueryString)
        {
            bool isValid = CheckValidation(itemFromQueryString);
            if (!isValid)
            {
                SheerResponse.Alert("This item has validation errors. You may want to review them and schedule another publish.");
            }
            return true;
        }

        private bool ValidateDateChosen()
        {
            if (DateTime.Compare(DateUtil.IsoDateToDateTime(this.PublishDateTime.Value, DateTime.MinValue), DateTime.Now) <= 0)
            {
                SheerResponse.Alert("The date selected for publish has passed. Please select a future date.");
                return false;
            }
            return true;
        }

        private void CreatePublishingTask(Item itemFromQueryString, bool isUnpublishing)
        {
            try
            {
                using (new SecurityDisabler())
                {
                    TemplateItem scheduleTaskTemplate = Context.ContentDatabase.GetTemplate(new ID(ScheduleTemplateID));
                    string validItemName = itemFromQueryString.ID.ToString()
                        .Replace("{", string.Empty)
                        .Replace("}", string.Empty);
                    Item schedulesFolder = Context.ContentDatabase.GetItem(SchedulesFolderPath);
                    Item newTask = schedulesFolder.Add(validItemName + "Task", scheduleTaskTemplate);
                    newTask.Editing.BeginEdit();
                    newTask["Command"] = "{EF235C25-AE83-4678-9E2C-C22175925893}";
                    newTask["Items"] = itemFromQueryString.Paths.FullPath;
                    newTask["CreatedByEmail"] = Context.User.Profile.Email;
                    newTask["Unpublish"] = isUnpublishing ? 1.ToString() : string.Empty;
                    newTask["PublishMethod"] = this.SmartPublish.Checked ? "smart" : "republish";
                    newTask["PublishChildren"] = this.PublishChildren.Checked ? "1" : string.Empty;
                    newTask["PublishLanguages"] = string.Join("|", GetLanguages().Select(x => x.Name));
                    newTask["TargetDatabase"] = string.Join("|", GetPublishingTargetDatabases().Select(x => x.Name));

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

                    string action = isUnpublishing ? "unpublishing" : "publishing";
                    Log.Info(
                        "Task scheduling " + action + ": " + itemFromQueryString.Name + " " + itemFromQueryString.ID +
                        DateTime.Now, this);
                }
            }
            catch (Exception e)
            {
                string action = isUnpublishing ? "unpublishing" : "publishing";
                Log.Info(
                    "Failed scheduling " + action + ": " + itemFromQueryString.Name + " " + itemFromQueryString.ID +
                    DateTime.Now + " " + e.ToString(), this);
            }
        }

        private bool CheckValidation(Item item)
        {
            item.Fields.ReadAll();
            ValidatorCollection validators = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.Workflow, item);
            ValidatorCollection validatorsBar = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.ValidatorBar, item);
            ValidatorCollection validatorsButton = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.ValidateButton, item);
            ValidatorCollection validatorsGutter = ValidatorManager.GetGlobalValidatorsForItem(ValidatorsMode.Gutter, item);
            var options = new ValidatorOptions(true);
            ValidatorManager.Validate(validatorsBar, options);
            foreach (BaseValidator validator in validatorsBar)
            {
                if (validator.Result != ValidatorResult.Valid)
                {
                    return false;
                }
            }
            //return !validators.Any(x => x.Result != ValidatorResult.Valid);
            return true;
        }


        private static Language[] GetLanguages()
        {
            ArrayList arrayList = new ArrayList();
            foreach (string index in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (index != null && index.StartsWith("la_", StringComparison.InvariantCulture))
                    arrayList.Add(Language.Parse(Context.ClientPage.ClientRequest.Form[index]));
            }
            return arrayList.ToArray(typeof(Language)) as Language[];
        }

        private List<Item> GetPublishingTargets()
        {
            List<Item> list = new List<Item>();
            foreach (string str in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (str != null && str.StartsWith("pb_", StringComparison.InvariantCulture))
                {
                    Item obj = Context.ContentDatabase.Items[ShortID.Decode(str.Substring(3))];
                    Assert.IsNotNull((object)obj, typeof(Item), "Publishing target not found.", new object[0]);
                    list.Add(obj);
                }
            }
            return list;
        }

        private Database[] GetPublishingTargetDatabases()
        {
            ArrayList arrayList = new ArrayList();
            foreach (BaseItem baseItem in GetPublishingTargets())
            {
                string name = baseItem["Target database"];
                Database database = Factory.GetDatabase(name);
                Assert.IsNotNull((object)database, typeof(Database), Translate.Text("Database \"{0}\" not found."), (object)name);
                arrayList.Add((object)database);
            }
            return arrayList.ToArray(typeof(Database)) as Database[];
        }
    }
}