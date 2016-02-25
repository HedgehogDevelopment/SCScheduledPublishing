using System;
using System.Collections.Generic;
using System.Linq;
using ScheduledPublish.Recurrence.Abstraction;
using ScheduledPublish.Recurrence.Implementation;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublish.Models
{
    public class CommandSchedule: IRecurringSchedule
    {
        public static readonly ID RecurrenceTypeId = ID.Parse("{41255D0C-B40E-4218-87DC-C8EBF349A4FE}");
        public static readonly ID HoursToNextPublishId = ID.Parse("{CE68EA41-A925-494C-BE41-14C6C5BCB671}");
        public static readonly ID IsExecutedId = ID.Parse("{EEAC5DF6-19B2-425B-84F4-466D44213108}");
        public static readonly ID ItemsId = ID.Parse("{8B07571D-D616-4373-8DB0-D77672911D16}");
        public static readonly ID ScheduledDateId = ID.Parse("{9691E387-E516-450E-83EA-845AF5BA7276}");
        public static readonly ID SourceDatabaseId = ID.Parse("{61632EB9-8A59-4AAB-B790-91AF3DA7B9F4}");

        public CommandSchedule()
        { }

        public CommandSchedule(Item item)
        {
            InnerItem = item;
            IsExecuted = "1" == item[IsExecutedId];
            RecurrenceType = ParseRecurrenceType(item[RecurrenceTypeId]);

            string itemsPath = item[ItemsId];
            if (!string.IsNullOrWhiteSpace(itemsPath) && SourceDatabase != null)
            {
                Items = itemsPath.Split('|').Select(x => SourceDatabase.GetItem(x));
            }

            string dateString = item[ScheduledDateId];
            if (!string.IsNullOrWhiteSpace(dateString))
            {
                ScheduledDate = DateUtil.ToServerTime(DateUtil.IsoDateToDateTime(dateString, DateTime.MinValue));
            }

            string sourceDatabaseName = item[SourceDatabaseId];
            if (!string.IsNullOrWhiteSpace(sourceDatabaseName))
            {
                SourceDatabase = Database.GetDatabase(sourceDatabaseName);
            }

            int hoursToNextSchedule;
            if (int.TryParse(item[HoursToNextPublishId], out hoursToNextSchedule))
            {
                HoursToNextSchedule = hoursToNextSchedule;
            }
        }

        public Item InnerItem { get; protected set; }

        public IEnumerable<Item> Items { get; set; }

        public DateTime ScheduledDate { get; set; }

        public RecurrenceType RecurrenceType { get; set; }

        public int HoursToNextSchedule { get; set; }

        public Database SourceDatabase { get; set; }

        public bool IsExecuted { get; set; }

        private static RecurrenceType ParseRecurrenceType(string type)
        {
            RecurrenceType castedType;
            return Enum.TryParse(type, true, out castedType)
                ? castedType
                : RecurrenceType.None;
        }
    }
}