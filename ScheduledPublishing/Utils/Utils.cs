using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace ScheduledPublishing.Utils
{
    public static class Utils
    {
        /// <summary>
        /// Get appropriate hour folder or create one if not present using the year/month/day/hour structure
        /// </summary>
        /// <param name="date">Date chosen for publishing</param>
        /// <returns>The hour folder as an item</returns>
        public static Item GetOrCreateFolder(DateTime date, Database database)
        {
            Item publishOptionsFolder = database.GetItem(Constants.PUBLISH_OPTIONS_FOLDER_ID);
            string yearName = date.Year.ToString();
            string monthName = date.Month.ToString();
            string dayName = date.Day.ToString();
            string hourName = date.AddHours(1).Hour.ToString();

            TemplateItem folderTemplate = database.GetTemplate(Constants.FOLDER_TEMPLATE_ID);
            Item yearFolder = publishOptionsFolder.Children.FirstOrDefault(x => x.Name == yearName) ??
                              publishOptionsFolder.Add(yearName, folderTemplate);


            Item monthFolder = yearFolder.Children.FirstOrDefault(x => x.Name == monthName) ??
                               yearFolder.Add(monthName, folderTemplate);

            Item dayFolder = monthFolder.Children.FirstOrDefault(x => x.Name == dayName) ??
                             monthFolder.Add(dayName, folderTemplate);

            Item hourFolder = dayFolder.Children.FirstOrDefault(x => x.Name == hourName) ??
                              dayFolder.Add(hourName, folderTemplate);

            return hourFolder;
        }
    }
}