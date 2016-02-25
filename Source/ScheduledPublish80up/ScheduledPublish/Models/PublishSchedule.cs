using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Security.Accounts;

namespace ScheduledPublish.Models
{
    /// <summary>
    /// Parses Schedule item from Sitecore into an object.
    /// </summary>
    public class PublishSchedule: CommandSchedule
    {
        public static readonly ID SchedulerUsernameId = ID.Parse("{0BBED214-85E7-4773-AB6A-9608CAC921FE}");
        public static readonly ID TargetDatabasesId = ID.Parse("{193B7E69-8C83-422F-80B2-F7B48C42775E}");
        public static readonly ID TargetLanguagesId = ID.Parse("{65C16118-BD34-4E45-9AAD-45C7AD0AE69A}");
        public static readonly ID UnpublishId = ID.Parse("{0A1E6524-43BA-4F3D-B7BF-1DD696FB2953}");
        public static readonly ID PublishChildrenId = ID.Parse("{7E70DC6C-21E1-48C2-8AEC-ACA6A4B8BCB2}");
        public static readonly ID PublishModeId = ID.Parse("{F313EF5C-AC40-46DB-9AA1-52C70D590338}");
        public static readonly ID PublishRelatedItemsId = ID.Parse("{D1DE06E3-0A92-4DCE-8787-FA6DF424E3F5}");

        public PublishSchedule()
        { }

        public PublishSchedule(Item item): base(item)
        {
            SchedulerUsername = item[SchedulerUsernameId];
            Unpublish = "1" == item[UnpublishId];
            PublishChildren = "1" == item[PublishChildrenId];
            PublishRelatedItems = "1" == item[PublishRelatedItemsId];
            PublishMode = ParseMode(item[PublishModeId]);

            if (!string.IsNullOrWhiteSpace(SchedulerUsername))
            {
                User user = User.FromName(SchedulerUsername, false);
                if (user != null && user.Profile != null)
                {
                    SchedulerEmail = user.Profile.Email;
                }
            }

            string targetDatabaseNames = item[TargetDatabasesId];
            if (!string.IsNullOrWhiteSpace(targetDatabaseNames))
            {
                TargetDatabases = targetDatabaseNames.Split('|').Select(Database.GetDatabase);
            }

            string languages = item[TargetLanguagesId];
            if (!string.IsNullOrWhiteSpace(languages))
            {
                TargetLanguages = languages.Split('|').Select(LanguageManager.GetLanguage).Where(l => l != null);
            }
        }

        /// <summary>
        /// Username of the person who scheduled the publish
        /// </summary>
        public string SchedulerUsername { get; set; }

        /// <summary>
        /// User's email who scheduled the publish
        /// </summary>
        public string SchedulerEmail { get; set; }

        /// <summary>
        /// Is scheduled unpublish
        /// </summary>
        public bool Unpublish { get; set; }

        /// <summary>
        /// Publish all children
        /// </summary>
        public bool PublishChildren { get; set; }

        /// <summary>
        /// Publish all related items
        /// </summary>
        public bool PublishRelatedItems { get; set; }

        /// <summary>
        /// Target databases for publish
        /// </summary>
        public IEnumerable<Database> TargetDatabases { get; set; }

        /// <summary>
        /// Target languages for publish
        /// </summary>
        public IEnumerable<Language> TargetLanguages { get; set; }

        /// <summary>
        /// Smart, Incremental, Full
        /// </summary>
        public PublishMode PublishMode { get; set; }

        /// <summary>
        /// Parses mode from string to enum
        /// </summary>
        /// <param name="mode">Mode string</param>
        /// <returns>Mode enum</returns>
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