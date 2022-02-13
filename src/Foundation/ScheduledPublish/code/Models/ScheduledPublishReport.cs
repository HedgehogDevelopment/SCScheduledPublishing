namespace ScheduledPublish.Models
{
    /// <summary>
    /// Handles publish report status and message
    /// </summary>
    public class ScheduledPublishReport
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
    }
}