using ScheduledPublish.Recurrence.Abstraction;

namespace ScheduledPublish.Recurrence.Implementation
{
    public class RecurrenceScheduler : IRecurrenceScheduler
    {
        public void ScheduleNextRecurrence(IRecurrentPublish recurrentPublish)
        {
            if (recurrentPublish == null)
            {
                return;
            }

            switch (recurrentPublish.RecurrenceType)
            {
                case RecurrenceType.Hourly:
                    {
                        if (recurrentPublish.HoursToNextPublish > 0)
                        {
                            recurrentPublish.PublishDate = recurrentPublish.PublishDate.AddHours(recurrentPublish.HoursToNextPublish);
                        }
                        break;
                    }
                case RecurrenceType.Daily:
                    {
                        recurrentPublish.PublishDate = recurrentPublish.PublishDate.AddDays(1);
                        break;
                    }

                case RecurrenceType.Weekly:
                    {
                        recurrentPublish.PublishDate = recurrentPublish.PublishDate.AddDays(7);
                        break;
                    }

                case RecurrenceType.Monthly:
                    {
                        recurrentPublish.PublishDate = recurrentPublish.PublishDate.AddMonths(1);
                        break;
                    }
            }
        }
    }
}