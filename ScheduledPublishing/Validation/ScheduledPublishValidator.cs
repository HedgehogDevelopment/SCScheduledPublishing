using System;
using System.Linq;
using ScheduledPublishing.Models;
using Sitecore.Data.Items;
using Sitecore.Publishing;

namespace ScheduledPublishing.Validation
{
    public static class ScheduledPublishValidator
    {
        public static ValidationResult Validate(PublishSchedule publishSchedule)
        {
            ValidationResult result = new ValidationResult()
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
                result.ValidationErrors.Add("Please select future date.");
                result.IsValid = false;
            }

            if (publishSchedule.TargetDatabases == null || !publishSchedule.TargetDatabases.Any())
            {
                result.ValidationErrors.Add("Please select at least one publish target.");
                result.IsValid = false;
            }

            if (publishSchedule.TargetLanguages == null || !publishSchedule.TargetLanguages.Any())
            {
                result.ValidationErrors.Add("Please select at least one publish language.");
                result.IsValid = false;
            }

            if (publishSchedule.PublishMode == PublishMode.Unknown)
            {
                result.ValidationErrors.Add("Unknow publish mode.");
                result.IsValid = false;
            }

            if (publishSchedule.Unpublish)
            {
                return result;
            }

            if (!IsPublishableItem(publishSchedule.ItemToPublish, publishSchedule.PublishDate))
            {
                result.ValidationErrors.Add("Item is not publishable at that time.");
                result.IsValid = false;
            }

            return result;
        }

        private static bool IsFutureDate(DateTime date)
        {
            return DateTime.Compare(date, DateTime.Now) > 0;
        }

        private static bool IsPublishableItem(Item item, DateTime date)
        {
            //We should also check ancestors because if any ancestor is marked for unpublish
            //our item will be also unpublished instead of published
            //IsValid added for workflow state and DateTime range (Valid From/Valid To)
            if (item != null 
                && item.Publishing.IsPublishable(date, true)
                && item.Versions.GetVersions().Any(x => x.Publishing.IsValid(date, true)))
            {
                return true;
            }

            return false;
        }
    }
}