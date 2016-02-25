using System;
using System.Collections.Generic;
using Sitecore.Data;

namespace ScheduledPublish.Repos.Abstraction
{
    interface IReadOnlySchedulesRepo<out T>
    {
        IEnumerable<T> GetSchedules();

        IEnumerable<T> GetSchedules(ID itemId);

        IEnumerable<T> GetUnexecutedSchedules();

        IEnumerable<T> GetUnexecutedSchedules(DateTime from, DateTime to);

        IEnumerable<T> GetRecurringSchedules();

        IEnumerable<T> GetRecurringSchedules(DateTime from, DateTime to);

        void CleanRepo();
    }
}
