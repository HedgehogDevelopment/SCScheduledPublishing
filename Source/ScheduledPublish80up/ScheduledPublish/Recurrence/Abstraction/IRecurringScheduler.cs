namespace ScheduledPublish.Recurrence.Abstraction
{
    public interface IRecurringScheduler
    {
        void ScheduleNextRecurrence(IRecurringSchedule recurringSchedule);
    }
}
