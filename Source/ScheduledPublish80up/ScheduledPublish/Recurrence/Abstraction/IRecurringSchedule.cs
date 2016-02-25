using System;
using ScheduledPublish.Recurrence.Implementation;

namespace ScheduledPublish.Recurrence.Abstraction
{
    public interface IRecurringSchedule
    {
        DateTime ScheduledDate { get; set; }

        RecurrenceType RecurrenceType { get; set; }

        int HoursToNextSchedule { get; set; }
    }
}
