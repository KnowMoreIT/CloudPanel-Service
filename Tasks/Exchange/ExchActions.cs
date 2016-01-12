using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;

namespace CPService.Tasks.Exchange
{
    public class ExchActions : ExchPowershell
    {
        private static readonly ILog logger = LogManager.GetLogger("ExchActions");

        /// <summary>
        /// Empty constructor not used
        /// </summary>
        public ExchActions() : base() { }

        public Guid Get_ExchangeGuid(string identity)
        {
            PSCommand cmd = new PSCommand();
            cmd.AddCommand("Get-Mailbox");
            cmd.AddParameter("Identity", identity);
            cmd.AddParameter("DomainController", Config.ServiceSettings.PrimaryDC);
            _powershell.Commands = cmd;

            var psObjects = _powershell.Invoke();
            if (_powershell.HadErrors)
                throw _powershell.Streams.Error[0].Exception;
            else
            {
                var foundUser = psObjects[0];
                return Guid.Parse(foundUser.Properties["ExchangeGuid"].Value.ToString());
            }
        }

        /// <summary>
        /// Gets a specific users mailbox size
        /// </summary>
        /// <param name="userGuid"></param>
        /// <returns></returns>
        public StatMailboxSizes Get_MailboxSize(Guid userGuid, bool isArchive = false)
        {
            logger.DebugFormat("Getting mailbox size for {0}", userGuid);

            PSCommand cmd = new PSCommand();
            cmd.AddCommand("Get-MailboxStatistics");
            cmd.AddParameter("Identity", userGuid.ToString());
            cmd.AddParameter("DomainController", Config.ServiceSettings.PrimaryDC);
            if (isArchive)
                cmd.AddParameter("Archive");
            _powershell.Commands = cmd;

            var psObjects = _powershell.Invoke();
            if (psObjects.Count > 0)
            {
                var returnSize = new StatMailboxSizes();
                foreach (PSObject obj in psObjects)
                {
                    returnSize.UserGuid = userGuid;
                    returnSize.MailboxDatabase = obj.Members["Database"].Value.ToString();
                    returnSize.TotalItemSize = obj.Members["TotalItemSize"].Value.ToString();
                    returnSize.TotalItemSizeInBytes = GetExchangeBytes(returnSize.TotalItemSize);
                    returnSize.TotalDeletedItemSize = obj.Members["TotalDeletedItemSize"].Value.ToString();
                    returnSize.TotalDeletedItemSizeInBytes = GetExchangeBytes(returnSize.TotalDeletedItemSize);

                    int itemCount = 0;
                    int.TryParse(obj.Members["ItemCount"].Value.ToString(), out itemCount);
                    returnSize.ItemCount = itemCount;

                    int deletedItemCount = 0;
                    int.TryParse(obj.Members["DeletedItemCount"].Value.ToString(), out deletedItemCount);
                    returnSize.DeletedItemCount = deletedItemCount;

                    returnSize.Retrieved = DateTime.Now;
                    break;
                }

                logger.DebugFormat("Successfully retrieves mailbox statistics for {0}: {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                    userGuid, returnSize.MailboxDatabase, returnSize.TotalItemSize, returnSize.TotalItemSizeInBytes,
                    returnSize.TotalDeletedItemSize, returnSize.TotalDeletedItemSizeInBytes, returnSize.ItemCount, returnSize.DeletedItemCount);

                return returnSize;
            }
            else
            {
                if (_powershell.Streams.Error.Count > 0)
                    throw _powershell.Streams.Error[0].Exception;

                if (_powershell.Streams.Warning.Count > 0)
                    throw new Exception(_powershell.Streams.Warning[0].Message);

                throw new Exception("No data was returned");
            }
        }

        /// <summary>
        /// Get a list of mailbox databases and their sizes
        /// </summary>
        /// <returns></returns>
        public List<StatMailboxDatabaseSizes> Get_MailboxDatabaseSizes()
        {
            var returnList = new List<StatMailboxDatabaseSizes>();

            PSCommand cmd = new PSCommand();
            cmd.AddCommand("Get-MailboxDatabase");
            cmd.AddParameter("Status");
            if (Config.ServiceSettings.ExchangeVersion > 2010) { cmd.AddParameter("IncludePreExchange2013"); }
            cmd.AddParameter("DomainController", Config.ServiceSettings.PrimaryDC);
            _powershell.Commands = cmd;

            var psObjects = _powershell.Invoke();
            if (_powershell.HadErrors)
                throw _powershell.Streams.Error[0].Exception;

            var retrieved = DateTime.Now;
            foreach (var ps in psObjects)
            {
                var newEntry = new StatMailboxDatabaseSizes();
                newEntry.DatabaseName = ps.Members["Identity"].Value.ToString();
                newEntry.Server = ps.Members["Server"].Value.ToString();
                newEntry.Retrieved = retrieved;

                if (ps.Members["DatabaseSize"].Value != null)
                {
                    newEntry.DatabaseSize = ps.Members["DatabaseSize"].Value.ToString();
                    newEntry.DatabaseSizeInBytes = GetExchangeBytes(newEntry.DatabaseSize);
                }
                else
                {
                    newEntry.DatabaseSize = "0 MB (0 bytes)";
                    newEntry.DatabaseSizeInBytes = 0;
                }

                returnList.Add(newEntry);
                logger.DebugFormat("Found Exchange database {0} with size of {1}", newEntry.DatabaseName, newEntry.DatabaseSize);
            }

            return returnList;
        }

        /// <summary>
        /// Gets a list of message tracking logs between a time period for sent messages
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<Models.MessageTrackingLog> Get_TotalSentMessages(DateTime start, DateTime end)
        {
            logger.InfoFormat("Querying total sent messages from message logs beginning {0} and ending {1}", start.ToString(), end.ToString());
            var totalSentMessages = new List<Models.MessageTrackingLog>();

            PSCommand cmd = new PSCommand();
            if (Config.ServiceSettings.ExchangeVersion >= 2013)
                cmd.AddCommand("Get-TransportService");
            else
                cmd.AddCommand("Get-TransportServer");

            cmd.AddCommand("Get-MessageTrackingLog");
            cmd.AddParameter("EventId", "RECEIVE");
            cmd.AddParameter("Start", start.ToString("MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture));
            cmd.AddParameter("End", end.ToString("MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture));
            cmd.AddParameter("ResultSize", "Unlimited");
            cmd.AddParameter("DomainController", Config.ServiceSettings.PrimaryDC);
            _powershell.Commands = cmd;

            var psObjects = _powershell.Invoke();
            if (_powershell.HadErrors)
                throw _powershell.Streams.Error[0].Exception;
            else
            {
                logger.DebugFormat("Found a total of {0} sent messages from {1} to {2}... filtering...", psObjects.Count, start.ToString(), end.ToString());

                foreach (PSObject ps in psObjects)
                {
                    var newLog = new Models.MessageTrackingLog();
                    newLog.Timestamp = DateTime.Parse(ps.Members["Timestamp"].Value.ToString());
                    newLog.ServerHostname = ps.Members["ServerHostname"].Value.ToString();
                    newLog.Source = ps.Members["Source"].Value.ToString();
                    newLog.EventId = ps.Members["EventId"].Value.ToString();
                    newLog.TotalBytes = long.Parse(ps.Members["TotalBytes"].Value.ToString());

                    newLog.Users = new List<string>();
                    newLog.Users.Add(ps.Members["Sender"].Value.ToString());

                    if (newLog.Source.Equals("STOREDRIVER"))
                        totalSentMessages.Add(newLog);
                }

                logger.DebugFormat("Finished filtering sent messages from {0} to {1}", start.ToString(), end.ToString());
            }

            return totalSentMessages;
        }

        /// <summary>
        /// Gets a list of message tracking logs between a time period for received messages
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<Models.MessageTrackingLog> Get_TotalReceivedMessages(DateTime start, DateTime end)
        {
            logger.InfoFormat("Querying total received messages from message logs beginning {0} and ending {1}", start.ToString(), end.ToString());
            var totalReceivedMessages = new List<Models.MessageTrackingLog>();

            PSCommand cmd = new PSCommand();
            if (Config.ServiceSettings.ExchangeVersion >= 2013)
                cmd.AddCommand("Get-TransportService");
            else
                cmd.AddCommand("Get-TransportServer");
            cmd.AddCommand("Get-MessageTrackingLog");
            cmd.AddParameter("EventId", "DELIVER");
            cmd.AddParameter("Start", start.ToString("MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture));
            cmd.AddParameter("End", end.ToString("MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture));
            cmd.AddParameter("ResultSize", "Unlimited");
            cmd.AddParameter("DomainController", Config.ServiceSettings.PrimaryDC);
            _powershell.Commands = cmd;

            var psObjects = _powershell.Invoke();
            if (_powershell.HadErrors)
                throw _powershell.Streams.Error[0].Exception;
            else
            {
                logger.DebugFormat("Found a total of {0} received messages from {1} to {2}... filtering...", psObjects.Count, start.ToString(), end.ToString());

                foreach (PSObject ps in psObjects)
                {
                    var newLog = new Models.MessageTrackingLog();
                    newLog.Timestamp = DateTime.Parse(ps.Members["Timestamp"].Value.ToString());
                    newLog.ServerHostname = ps.Members["ServerHostname"].Value.ToString();
                    newLog.Source = ps.Members["Source"].Value.ToString();
                    newLog.EventId = ps.Members["EventId"].Value.ToString();
                    newLog.TotalBytes = long.Parse(ps.Members["TotalBytes"].Value.ToString());

                    var multiValue = ps.Members["Recipients"].Value as PSObject;
                    var users = multiValue.BaseObject as ArrayList;
                    var array = users.ToArray(typeof(string)) as string[];
                    newLog.Users = array.ToList();

                    totalReceivedMessages.Add(newLog);
                }

                logger.DebugFormat("Finished filtering received messages from {0} to {1}", start.ToString(), end.ToString());
            }

            return totalReceivedMessages;
        }
    }
}
