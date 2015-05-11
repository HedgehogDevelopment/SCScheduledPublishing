using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Publishing;

namespace ScheduledPublish.Models
{
    public class PublishSchedule
    {
        public static readonly ID SchedulerEmailId = ID.Parse("{0BBED214-85E7-4773-AB6A-9608CAC921FE}");
        public static readonly ID ItemToPublishId = ID.Parse("{8B07571D-D616-4373-8DB0-D77672911D16}");
        public static readonly ID SourceDatabaseId = ID.Parse("{61632EB9-8A59-4AAB-B790-91AF3DA7B9F4}");
        public static readonly ID TargetDatabasesId = ID.Parse("{193B7E69-8C83-422F-80B2-F7B48C42775E}");
        public static readonly ID PublishDateId = ID.Parse("{9691E387-E516-450E-83EA-845AF5BA7276}");
        public static readonly ID TargetLanguagesId = ID.Parse("{65C16118-BD34-4E45-9AAD-45C7AD0AE69A}");
        public static readonly ID UnpublishId = ID.Parse("{0A1E6524-43BA-4F3D-B7BF-1DD696FB2953}");
        public static readonly ID PublishChildrenId = ID.Parse("{7E70DC6C-21E1-48C2-8AEC-ACA6A4B8BCB2}");
        public static readonly ID PublishModeId = ID.Parse("{F313EF5C-AC40-46DB-9AA1-52C70D590338}");
        public static readonly ID IsPublishedId = ID.Parse("{EEAC5DF6-19B2-425B-84F4-466D44213108}");
        public static readonly ID PublishRelatedItemsId = ID.Parse("{D1DE06E3-0A92-4DCE-8787-FA6DF424E3F5}");

        public PublishSchedule()
        {
        }

        public PublishSchedule(Item item)
        {
            InnerItem = item;
            SchedulerEmail = item[SchedulerEmailId];
            Unpublish = "1" == item[UnpublishId];
            PublishChildren = "1" == item[PublishChildrenId];
            PublishRelatedItems = "1" == item[PublishRelatedItemsId];
            PublishMode = ParseMode(item[PublishModeId]);
            IsPublished = "1" == item[IsPublishedId];

            string sourceDatabaseName = item[SourceDatabaseId];
            if (!string.IsNullOrWhiteSpace(sourceDatabaseName))
            {
                SourceDatabase = Database.GetDatabase(sourceDatabaseName);
            }

            string targetDatabaseNames = item[TargetDatabasesId];
            if (!string.IsNullOrWhiteSpace(targetDatabaseNames))
            {
                TargetDatabases = targetDatabaseNames.Split('|').Select(Database.GetDatabase);
            }

            string itemToPublishPath = item[ItemToPublishId];
            if (!string.IsNullOrWhiteSpace(itemToPublishPath) && SourceDatabase != null)
            {
                ItemToPublish = SourceDatabase.GetItem(itemToPublishPath);
            }

            string dateString = item[PublishDateId];
            if (!string.IsNullOrWhiteSpace(dateString))
            {
                PublishDate = DateUtil.IsoDateToDateTime(dateString, DateTime.MinValue);
            }

            string languages = item[TargetLanguagesId];
            if (!string.IsNullOrWhiteSpace(languages))
            {
                TargetLanguages = languages.Split('|').Select(LanguageManager.GetLanguage).Where(l => l != null);
            }
        }

        public Item InnerItem { get; private set; } 

        public string SchedulerEmail { get; set; }

        public Item ItemToPublish { get; set; }

        public bool Unpublish { get; set; }

        public bool PublishChildren { get; set; }

        public bool PublishRelatedItems { get; set; }

        public DateTime PublishDate { get; set; }

        public Database SourceDatabase { get; set; }

        public IEnumerable<Database> TargetDatabases { get; set; }

        public IEnumerable<Language> TargetLanguages { get; set; }

        public PublishMode PublishMode { get; set; }

        public bool IsPublished { get; set; }

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
    }
}