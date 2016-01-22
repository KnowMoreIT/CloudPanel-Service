using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CPService.Config
{
    public class ServiceTaskIntervals
    {
        /// <summary>
        /// Interval to get a list of disabled users from Active Directory
        /// </summary>
        public static int ad_GetDisabledUsers { get; set; }

        /// <summary>
        /// Interval to get a list of locked out users in Active Directory
        /// </summary>
        public static int ad_GetLockedUsers { get; set; }

        /// <summary>
        /// Interval to get a list of mailbox sizes in Exchange
        /// </summary>
        public static int exch_GetMailboxSizes { get; set; }

        /// <summary>
        /// Interval to get a list of mailbox database sizes in Exchange
        /// </summary>
        public static int exch_GetMailboxDatabaseSizes { get; set; }

        /// <summary>
        /// Interval to get a list of message tracking logs (sent/received messages) in Exchange
        /// </summary>
        public static int exch_GetMessageTrackingLogs { get; set; }

        /// <summary>
        /// Interval to find missing data in the database
        /// </summary>
        public static int db_FindMissingData { get; set; }

        /// <summary>
        /// Interval to update the database statistics at current point in time.
        /// </summary>
        public static int db_UpdateDatabaseHistory { get; set; }

        /// <summary>
        /// Gets a list of intervals and sets the values in memory from the xml file
        /// </summary>
        public static void GetIntervalsFromConfig()
        {
            try
            {
                var _assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
                var path = string.Format("{0}\\{1}", System.IO.Path.GetDirectoryName(_assembly), "config\\settings.xml");
                
                var xDoc = XDocument.Load(path);
                var x = from s in xDoc.Elements("cloudpanel") select s;

                // Active Directory
                ad_GetDisabledUsers = ConvertStringIntervalToInteger(ref x, "ActiveDirectory", "GetDisabledUsers");
                ad_GetLockedUsers = ConvertStringIntervalToInteger(ref x, "ActiveDirectory", "GetLockedUsers");

                // Exchange
                exch_GetMailboxSizes = ConvertStringIntervalToInteger(ref x, "Exchange", "GetMailboxSizes");
                exch_GetMailboxDatabaseSizes = ConvertStringIntervalToInteger(ref x, "Exchange", "GetMailboxDatabaseSizes");
                exch_GetMessageTrackingLogs = ConvertStringIntervalToInteger(ref x, "Exchange", "GetMessageTrackingLogs");

                // Database
                db_FindMissingData = ConvertStringIntervalToInteger(ref x, "Database", "FindMissingData");
                db_UpdateDatabaseHistory = ConvertStringIntervalToInteger(ref x, "Database", "UpdateDatabaseHistory");

            }
            catch (Exception ex)
            {
                CPService.LogError("Error retrieving intervals from config file: " + ex.ToString());
            }
        }

        /// <summary>
        /// Reads the element and converts it to a integer
        /// </summary>
        /// <param name="x"></param>
        /// <param name="parent"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private static int ConvertStringIntervalToInteger(ref IEnumerable<XElement> x, string parent, string element)
        {
            return int.Parse(x.Descendants(parent)
                              .Elements(element)
                              .FirstOrDefault()
                              .Value);
        }
    }
}
