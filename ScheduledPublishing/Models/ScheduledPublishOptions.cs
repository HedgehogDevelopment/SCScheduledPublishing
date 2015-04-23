using Sitecore.Data.Items;
using System.Linq;
using Sitecore.Data;
using Sitecore.Globalization;
using Sitecore.Publishing;
using ScheduledPublishing.Utils;

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

                //TODO
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

                //TODO
                return null;
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

                //TODO
                return null;
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

                //TODO
                this._publishMode = ParseMode("smart");
                return this._publishMode;
            }
        }

        private static PublishMode ParseMode(string mode)
        {
            switch (mode.ToLowerInvariant())
            {
                case "full":
                    return PublishMode.Full;
                case "incremental":
                    return PublishMode.Full;
                case "smart":
                    return PublishMode.Smart;
                default:
                    return PublishMode.Unknown;
            }
        }
    }
}