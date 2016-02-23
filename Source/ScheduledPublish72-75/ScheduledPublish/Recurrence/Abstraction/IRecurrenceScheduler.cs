namespace ScheduledPublish.Recurrence.Abstraction
{
    public interface IRecurrenceScheduler
    {
        void ScheduleNextRecurrence(IRecurrentPublish recurrentPublish);
    }
}
