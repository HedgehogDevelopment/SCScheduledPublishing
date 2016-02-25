using System;
using System.Collections.Generic;
using Sitecore.Data;

namespace ScheduledPublish.Repos.Abstraction
{
    interface ISchedulesRepo<T>: IReadOnlySchedulesRepo<T>
    {
        void CreateSchedule(T schedule);

        void DeleteSchedule(T schedule);

        void UpdateSchedule(T schedule);
    }
}
