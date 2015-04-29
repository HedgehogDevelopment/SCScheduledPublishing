using Sitecore.Data.Items;
using System.Linq;
using Sitecore.Data;
using Sitecore.Publishing;
using Sitecore.Data.Managers;
using Sitecore.Globalization;

namespace ScheduledPublishing.Models
{
    public class ScheduledPublishOptions
    {
        public Item InnerItem { get; private set; }

        public ScheduledPublishOptions(Item item)
        {
            this.InnerItem = item;
        }

        private string _schedulerEmail;
        public string SchedulerEmail
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this._schedulerEmail))
                {
                    return this._schedulerEmail;
                }

                this._schedulerEmail = this.InnerItem[ID.Parse("{0BBED214-85E7-4773-AB6A-9608CAC921FE}")];
                return this._schedulerEmail;
            }
            set
            {
                this._schedulerEmail = value;
                this.InnerItem[ID.Parse("{0BBED214-85E7-4773-AB6A-9608CAC921FE}")] = value;
            }
        }

        private Item _itemToPublish;
        public Item ItemToPublish
        {
            get
            {
                if (this._itemToPublish != null)
                {
                    return this._itemToPublish;
                }

                var itemsId = this.InnerItem[ID.Parse("{8B07571D-D616-4373-8DB0-D77672911D16}")];
                if (string.IsNullOrWhiteSpace(itemsId) 
                    || this.SourceDatabase == null)
                {
                    return this._itemToPublish;
                }

                this._itemToPublish = this.SourceDatabase.GetItem(itemsId);
                return this._itemToPublish;
            }
        }

        public string ItemToPublishPath
        {
            get { return this._itemToPublish.Paths.FullPath; }
            set { this.InnerItem[ID.Parse("{8B07571D-D616-4373-8DB0-D77672911D16}")] = value; }
        }

        public bool Unpublish
        {
            get
            {
                return "1" == this.InnerItem[ID.Parse("{0A1E6524-43BA-4F3D-B7BF-1DD696FB2953}")];
            }
            set
            {
                this.InnerItem[ID.Parse("{0A1E6524-43BA-4F3D-B7BF-1DD696FB2953}")] = value ? "1" : string.Empty;
            }
        }

        public bool PublishChildren
        {
            get
            {
                return "1" == this.InnerItem[ID.Parse("{7E70DC6C-21E1-48C2-8AEC-ACA6A4B8BCB2}")];
            }
            set
            {
                this.InnerItem[ID.Parse("{7E70DC6C-21E1-48C2-8AEC-ACA6A4B8BCB2}")] = value ? "1" : string.Empty;
            }
        }

        public string PublishDateString
        {
            get
            {
                return this.InnerItem[ID.Parse("{9691E387-E516-450E-83EA-845AF5BA7276}")];
            }
            set
            {
                this.InnerItem[ID.Parse("{9691E387-E516-450E-83EA-845AF5BA7276}")] = value;
            }
        }

        private Database _sourceDatabase;
        public Database SourceDatabase
        {
            get
            {
                if (this._sourceDatabase != null)
                {
                    return this._sourceDatabase;
                }

                var database = this.InnerItem[ID.Parse("{61632EB9-8A59-4AAB-B790-91AF3DA7B9F4}")];
                if (string.IsNullOrWhiteSpace(database))
                {
                    return this._sourceDatabase;
                }

                this._sourceDatabase = Database.GetDatabase(database);
                return this._sourceDatabase;
            }
        }

        public string SourceDatabaseString
        {
            get
            {
                return this.InnerItem[ID.Parse("{61632EB9-8A59-4AAB-B790-91AF3DA7B9F4}")];
            }
            set
            {
                this.InnerItem[ID.Parse("{61632EB9-8A59-4AAB-B790-91AF3DA7B9F4}")] = value;
            }
        }

        private Database[] _targetDatabases;
        public Database[] TargetDatabases
        {
            get
            {
                if (this._targetDatabases != null && this._targetDatabases.Any())
                {
                    return this._targetDatabases;
                }

                var databases = this.InnerItem[ID.Parse("{193B7E69-8C83-422F-80B2-F7B48C42775E}")];
                if (string.IsNullOrWhiteSpace(databases))
                {
                    return this._targetDatabases;
                }

                this._targetDatabases =
                    databases.Split('|').Select(Database.GetDatabase)
                                        .ToArray();

                return this._targetDatabases;
            }
        }

        public string TargetDatabasesString
        {
            get
            {
                return this.InnerItem[ID.Parse("{193B7E69-8C83-422F-80B2-F7B48C42775E}")];
            }
            set
            {
                this.InnerItem[ID.Parse("{193B7E69-8C83-422F-80B2-F7B48C42775E}")] = value;
            }
        }

        private Language[] _languages;
        public Language[] Languages
        {
            get
            {
                if (this._languages != null && this._languages.Any())
                {
                    return this._languages;
                }

                var languages = this.InnerItem[ID.Parse("{65C16118-BD34-4E45-9AAD-45C7AD0AE69A}")];
                if (string.IsNullOrWhiteSpace(languages)) // TODO: why?
                {
                    return this._languages;
                }

                this._languages = 
                    languages.Split('|')
                    .Select(LanguageManager.GetLanguage)
                    .Where(l => l != null)
                    .ToArray();

                return this._languages;
            }
            set { this.InnerItem[ID.Parse("{65C16118-BD34-4E45-9AAD-45C7AD0AE69A}")] = string.Join("|", (object[]) value); }
        }

        public string LanguagesString
        {
            get
            {
                return string.Join("|", this._languages.Select(x => x.Name));
            }
            set
            {
                this.InnerItem[ID.Parse("{65C16118-BD34-4E45-9AAD-45C7AD0AE69A}")] = value;
            }
        }

        private PublishMode _publishMode = PublishMode.Unknown;
        public PublishMode PublishMode
        {
            get
            {
                if (this._publishMode != PublishMode.Unknown)
                {
                    return this._publishMode;
                }

                var mode = this.InnerItem[ID.Parse("{F313EF5C-AC40-46DB-9AA1-52C70D590338}")];
                this._publishMode = ParseMode(mode);
                return this._publishMode;
            }
            set { this._publishMode = value; }
        }

        public string PublishModeString
        {
            get
            {
                if (this._publishMode != PublishMode.Unknown)
                {
                    return ParseMode(this._publishMode);
                }

                return this.InnerItem[ID.Parse("{F313EF5C-AC40-46DB-9AA1-52C70D590338}")];
            }
            set
            {
                this.InnerItem[ID.Parse("{F313EF5C-AC40-46DB-9AA1-52C70D590338}")] = value;
            }
        }

        private static PublishMode ParseMode(string mode)
        {
            switch (mode.ToLowerInvariant())
            {
                case "smart":
                    return PublishMode.Smart;
                case "full":
                    return PublishMode.Full;
                case "incremental":
                    return PublishMode.Incremental;
                default:
                    return PublishMode.Unknown;
            }
        }

        private static string ParseMode(PublishMode mode)
        {
            switch (mode)
            {
                case PublishMode.Smart:
                    return "smart";
                case PublishMode.Full:
                    return "full";
                case PublishMode.Incremental:
                    return "incremental";
                default:
                    return string.Empty;
            }
        }
    }
}