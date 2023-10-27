﻿using System;
using System.Linq;
using ScheduledPublish.Models;
using ScheduledPublish.Recurrence.Implementation;
using Sitecore.Data.Items;
using Sitecore.Publishing;

namespace ScheduledPublish.Validation
{
    /// <summary>
    /// Validator for the user's input while creating schedules
    /// </summary>
    public static class ScheduledPublishValidator
    {
        public static ValidationResult Validate(PublishSchedule publishSchedule)
        {
            ValidationResult result = new ValidationResult
            {
                IsValid = true
            };

            if (publishSchedule == null)
            {
                result.ValidationErrors.Add("Null reference");
                result.IsValid = false;

                return result;
            }

            if (!IsFutureDate(publishSchedule.PublishDate))
            {
                result.ValidationErrors.Add("Please, select future date.");
                result.IsValid = false;
            }

            if (publishSchedule.TargetDatabases == null || !publishSchedule.TargetDatabases.Any())
            {
                result.ValidationErrors.Add("Please, select at least one publish target.");
                result.IsValid = false;
            }

            if (publishSchedule.Unpublish)
            {
                return result;
            }

            if (publishSchedule.TargetLanguages == null || !publishSchedule.TargetLanguages.Any())
            {
                result.ValidationErrors.Add("Please, select at least one publish language.");
                result.IsValid = false;
            }

            if (publishSchedule.PublishMode == PublishMode.Unknown)
            {
                result.ValidationErrors.Add("Unknown publish mode.");
                result.IsValid = false;
            }

            if (!IsPublishableItem(publishSchedule.ItemToPublish, publishSchedule.PublishDate))
            {
                result.ValidationErrors.Add("Item is not publishable at that time.");
                result.IsValid = false;
            }

            if (publishSchedule.RecurrenceType == RecurrenceType.Hourly && publishSchedule.HoursToNextPublish == 0)
            {
                result.ValidationErrors.Add("Please, enter a valid, whole number value in 'Hour(s)' field.");
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Is schedules date in the future
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static bool IsFutureDate(DateTime date)
        {
            return DateTime.Compare(date, DateTime.Now) > 0;
        }

        /// <summary>
        /// Is passed item valid for publish in passed DateTime
        /// </summary>
        /// <param name="item">Item for check</param>
        /// <param name="date">DateTime</param>
        /// <returns></returns>
        private static bool IsPublishableItem(Item item, DateTime date)
        {
            //We should also check ancestors because if any ancestor is marked for unpublish
            //our item will be also unpublished instead of published
            //IsValid added for workflow state and DateTime range (Valid From/Valid To)
            if (item != null && item.Publishing.IsPublishable(date, true))
            {
                return true;
            }

            return false;
        }
    }
}