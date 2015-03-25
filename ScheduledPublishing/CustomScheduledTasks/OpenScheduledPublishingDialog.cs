using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using System;

namespace ScheduledPublishing.CustomScheduledTasks
{
    /// <summary>
    /// Opens the Scheduled Publishing Dialog
    /// </summary>
    public class OpenScheduledPublishingDialog : Command
    {
        public override void Execute(CommandContext context)
        {
            Log.Info("Schedule Publishing Command: " + DateTime.Now, this);
            Error.AssertObject((object)context, "context");
            if (context.Items.Length != 1 || context.Items[0] == null)
                return;
            Item obj = context.Items[0];
            UrlString urlString = new UrlString(UIUtil.GetUri("control:SchedulePublishing"));
            urlString.Append("id", obj.ID.ToString());
            Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), "500px", "250px", string.Empty, true);
        }
    }
}