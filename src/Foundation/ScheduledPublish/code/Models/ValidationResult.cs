using System.Collections.Generic;

namespace ScheduledPublish.Models
{
    /// <summary>
    /// Handles validation status and errors of schedules
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; }

        public ValidationResult()
        {
            ValidationErrors = new List<string>();
        }
    }
}