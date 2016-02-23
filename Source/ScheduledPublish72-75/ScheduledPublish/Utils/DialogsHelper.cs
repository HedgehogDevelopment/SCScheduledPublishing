using ScheduledPublish.Recurrence.Implementation;

namespace ScheduledPublish.Utils
{
    /// <summary>
    /// Contains reusable messages/logic coupled to the UI Dialogs
    /// </summary>
    public static class DialogsHelper
    {
        /// <summary>
        /// Gets the text which defines the recurrence settings for the publish.
        /// </summary>
        /// <param name="type">Recurrence type</param>
        /// <param name="hours">Hours to next publish</param>
        /// <returns></returns>
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