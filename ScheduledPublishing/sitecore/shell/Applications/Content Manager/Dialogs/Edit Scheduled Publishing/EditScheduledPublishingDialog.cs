using System;
using Sitecore.Web.UI.Pages;

namespace ScheduledPublishing.sitecore.shell.Applications.ContentManager.Dialogs
{
    public class EditScheduledPublishingDialog : DialogForm
    {
        protected override void OnPreRender(EventArgs e)
        {
            throw new Exception("EDIT DIALOG PRERENDER");
            base.OnPreRender(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            throw new Exception("EDIT DIALOG");
            base.OnLoad(e);
        }
    }
}