using Sitecore.Data.Items;
using System.Linq;
using Sitecore.Data;
using Sitecore.Publishing;
using ScheduledPublishing.Utils;
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

        private Item[] _publishItems;
        public Item[] PublishItems
        {
            get
            {
                if (this._publishItems != null && this._publishItems.Any())
                {
                    return this._publishItems;
                }

                var itemsIds = this.InnerItem[Constants.PUBLISH_OPTIONS_PUBLISH_ITEMS];
                if (string.IsNullOrWhiteSpace(itemsIds))
                {
                    return this._publishItems;
                }

                this._publishItems = 
                    itemsIds.Split('|')
                            .Select(Database.GetDatabase("master").GetItem)
                            .ToArray();

                return this._publishItems;
            }
        }

        public bool Unpublish
        {
            get
            {
                return "1" == this.InnerItem[Constants.PUBLISH_OPTIONS_UNPUBLISH];

            }
        }

        public bool PublishChildren
        {
            get
            {
                return "1" == this.InnerItem[Constants.PUBLISH_OPTIONS_PUBLISH_CHILDREN];
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

                var databases = this.InnerItem[Constants.PUBLISH_OPTIONS_TARGET_DATABASES];
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

        private Language[] _languages;
        public Language[] Languages
        {
            get
            {
                if (this._languages != null && this._languages.Any())
                {
                    return this._languages;
                }

                var languages = this.InnerItem[Constants.PUBLISH_OPTIONS_TARGET_LANGUAGES];
                if (string.IsNullOrWhiteSpace(languages))
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

                var mode = this.InnerItem[Constants.PUBLISH_OPTIONS_PUBLISH_MODE];
                this._publishMode = ParseMode(mode);
                return this._publishMode;
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
                    return PublishMode.Smart;
            }
        }
    }
}