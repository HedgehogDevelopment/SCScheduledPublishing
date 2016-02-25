using System;
using ScheduledPublish.Models;
using ScheduledPublish.Repos.Abstraction;
using Sitecore.Data.Items;

namespace ScheduledPublish.Repos.Implementation
{
    public class ScheduledCommandRepo: SchedulesRepo<CommandSchedule>
    {
        public override void CreateSchedule(CommandSchedule schedule)
        {
            throw new NotImplementedException();
        }

        public override void UpdateSchedule(CommandSchedule schedule)
        {
            throw new NotImplementedException();
        }

        protected override CommandSchedule Map(Item item)
        {
            return item == null ? null : new CommandSchedule(item);
        }

        protected override string BuildScheduleName()
        {
            return ItemUtil.ProposeValidItemName(string.Format("{0}ScheduledCommand", Guid.NewGuid()));
        }
    }
}