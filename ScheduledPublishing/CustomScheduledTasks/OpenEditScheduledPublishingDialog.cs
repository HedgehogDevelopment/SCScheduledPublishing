using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System.Collections.Specialized;

namespace ScheduledPublishing.CustomScheduledTasks
{
    public class OpenEditScheduledPublishingDialog : Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull((object)context, "context");
            Context.ClientPage.Start((object)this, "Run", new NameValueCollection());
        }

        protected void Run(ClientPipelineArgs args)
        {
            if (!SheerResponse.CheckModified())
            {
                return;
            }
                
            if (args.IsPostBack)
            {
                return;
            }

            UrlString urlString = new UrlString(UIUtil.GetUri("control:EditScheduledPublishing"));
            SheerResponse.ShowModalDialog(urlString.ToString(), "700", "400", string.Empty, true);
            args.WaitForPostBack();
        }
    }
}