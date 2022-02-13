﻿using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System.Collections.Specialized;
using Version = Sitecore.Data.Version;

namespace ScheduledPublish.Commands
{
    /// <summary>
    /// Opens the Scheduled Publishing Dialog
    /// </summary>
    [Serializable]
    public class OpenScheduledPublishDialog : Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");

            if (context.Items.Length != 1)
            {
                return;
            }

            bool isUnpublish = context.Parameters["unpublish"] != null && bool.Parse(context.Parameters["unpublish"]);
            Execute(context.Items[0], isUnpublish);
        }

        public void Execute(Item item, bool isUnpublish)
        {
            Assert.ArgumentNotNull(item, "item");

            NameValueCollection parameters = new NameValueCollection();
            parameters["id"] = item.ID.ToString();
            parameters["language"] = item.Language.ToString();
            parameters["version"] = item.Version.ToString();
            parameters["databasename"] = item.Database.Name;
            parameters["unpublish"] = isUnpublish.ToString();

            Context.ClientPage.Start(this, "Run", parameters);
        }

        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            string dbName = args.Parameters["databasename"];
            string id = args.Parameters["id"];
            string lang = args.Parameters["language"];
            string ver = args.Parameters["version"];
            Database database = Factory.GetDatabase(dbName);

            Assert.IsNotNull(database, dbName);

            Item obj = database.Items[id, Language.Parse(lang), Version.Parse(ver)];
            if (obj == null)
            {
                SheerResponse.Alert("Item not found.");
            }
            else
            {
                if (!SheerResponse.CheckModified())
                    return;
                if (args.IsPostBack)
                {
                    return;
                }

                UrlString urlString = new UrlString(UIUtil.GetUri("control:SchedulePublish"));
                urlString.Append("id", obj.ID.ToString());
                urlString.Append("unpublish", args.Parameters["unpublish"]);
                SheerResponse.ShowModalDialog(urlString.ToString(), "600", "600", string.Empty, true);
                args.WaitForPostBack();
            }
        }
    }
}