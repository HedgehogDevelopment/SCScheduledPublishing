using ScheduledPublish.Recurrence.Implementation;

namespace ScheduledPublish.Utils
{
    public static class DialogsHelper
    {
        public static string GetRecurrenceMessage(RecurrenceType type, int hours)
        {
            string message = string.Empty;

            switch (type)
            {
                case RecurrenceType.Hourly:
                    {
                        message = string.Format("Every {0} hour(s)", hours);
                        break;
                    }

                case RecurrenceType.Daily:
                case RecurrenceType.Weekly:
                case RecurrenceType.Monthly:
                    {
                        message = type.ToString();
                        break;
                    }
            }

            return message;
        }
    }
}