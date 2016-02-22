using System;
using ScheduledPublish.Recurrence.Implementation;

namespace ScheduledPublish.Recurrence.Abstraction
{
    public interface IRecurrentPublish
    {
        DateTime PublishDate { get; set; }

        RecurrenceType RecurrenceType { get; set; }

        int HoursToNextPublish { get; set; }
    }
}
