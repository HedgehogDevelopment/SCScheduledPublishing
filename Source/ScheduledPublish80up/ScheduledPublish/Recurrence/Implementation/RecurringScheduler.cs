using ScheduledPublish.Recurrence.Abstraction;

namespace ScheduledPublish.Recurrence.Implementation
{
    public class RecurringScheduler : IRecurringScheduler
    {
        public void ScheduleNextRecurrence(IRecurringSchedule recurringSchedule)
        {
            if (recurringSchedule == null)
            {
                return;
            }

            switch (recurringSchedule.RecurrenceType)
            {
                case RecurrenceType.Hourly:
                    {
                        if (recurringSchedule.HoursToNextSchedule > 0)
                        {
                            recurringSchedule.ScheduledDate = recurringSchedule.ScheduledDate.AddHours(recurringSchedule.HoursToNextSchedule);
                        }
                        break;
                    }
                case RecurrenceType.Daily:
                    {
                        recurringSchedule.ScheduledDate = recurringSchedule.ScheduledDate.AddDays(1);
                        break;
                    }

                case RecurrenceType.Weekly:
                    {
                        recurringSchedule.ScheduledDate = recurringSchedule.ScheduledDate.AddDays(7);
                        break;
                    }

                case RecurrenceType.Monthly:
                    {
                        recurringSchedule.ScheduledDate = recurringSchedule.ScheduledDate.AddMonths(1);
                        break;
                    }
            }
        }
    }
}