using System;
using System.Net.Mail;
using System.Text;
using System.Web.UI;
using Sitecore.Data.Items;
using ScheduledPublish.Smtp;

namespace ScheduledPublish.sitecore.admin.custom
{
    public partial class MailTester : Page
    {
        protected override void OnInit(EventArgs e)
        {
            Sitecore.Context.Database = Sitecore.Configuration.Factory.GetDatabase("master");
            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                return;
            }

            Item item = Sitecore.Context.Database.GetItem(PublishedItem.Text);
            MailMessage message = MailManager.ComposeEmail("", item, @"extranet\Anonymous");

            var sb = new StringBuilder();
            sb.AppendFormat("To: {0}", message.To);
            sb.AppendLine();
            sb.AppendFormat("Bcc: {0}", message.Bcc);
            sb.AppendLine();
            sb.AppendFormat("Body: {0}", message.Subject);

            EmailList.Text = sb.ToString();
        }
    }
}