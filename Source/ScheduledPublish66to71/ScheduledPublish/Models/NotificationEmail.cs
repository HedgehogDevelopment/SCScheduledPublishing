using ScheduledPublish.Utils;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublish.Models
{
    public class NotificationEmail
    {
        private static readonly Database _database = Constants.SCHEDULED_TASK_CONTEXT_DATABASE;

        private Item _innerItem;
        public Item InnerItem
        {
            get { return _database.GetItem(ID.Parse("{292C5A92-A8BB-4F27-97A5-29564DF45329}")); }
        }

        public string EmailTo
        {
            get { return _innerItem[ID.Parse("{A35E5B44-1CD0-49C4-B210-DB4106685CE4}")]; }
        }

        public string EmailFrom
        {
            get { return _innerItem[ID.Parse("{4A6046AA-2B94-47B2-9A27-3B83F0601822}")]; }
        }

        public string Subject
        {
            get { return _innerItem[ID.Parse("{20DEB7CE-6AD1-459F-B1A4-F6AE88B2C62A}")]; }
        }

        public string Body
        {
            get { return _innerItem[ID.Parse("{39A13A96-8BF0-4334-B366-16C0ACCC2B60}")]; }
        }

        public NotificationEmail()
        {
            _innerItem = InnerItem;
        }
    }
}