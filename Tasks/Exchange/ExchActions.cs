using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;

namespace CPService.Tasks.Exchange
{
    public class ExchActions : ExchPowershell
    {
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

            Collection<PSObject> psObjects = _powershell.Invoke();
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
            PSCommand cmd = new PSCommand();
            cmd.AddCommand("Get-MailboxStatistics");
            cmd.AddParameter("Identity", userGuid.ToString());
            cmd.AddParameter("DomainController", Config.ServiceSettings.PrimaryDC);
            if (isArchive)
                cmd.AddParameter("Archive");
            _powershell.Commands = cmd;

            Collection<PSObject> psObjects = _powershell.Invoke();
            if (psObjects.Count > 0)
            {
                StatMailboxSizes returnSize = new StatMailboxSizes();
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
            List<StatMailboxDatabaseSizes> returnList = new List<StatMailboxDatabaseSizes>(100);

            PSCommand cmd = new PSCommand();
            cmd.AddCommand("Get-MailboxDatabase");
            cmd.AddParameter("Status");
            if (Config.ServiceSettings.ExchangeVersion > 2010) { cmd.AddParameter("IncludePreExchange2013"); }
            cmd.AddParameter("DomainController", Config.ServiceSettings.PrimaryDC);
            _powershell.Commands = cmd;

            Collection<PSObject> psObjects = _powershell.Invoke();
            if (_powershell.HadErrors)
                throw _powershell.Streams.Error[0].Exception;

            DateTime retrieved = DateTime.Now;
            foreach (var ps in psObjects)
            {
                StatMailboxDatabaseSizes newEntry = new StatMailboxDatabaseSizes();
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
            List<Models.MessageTrackingLog> totalSentMessages = new List<Models.MessageTrackingLog>();

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

            Collection<PSObject> psObjects = _powershell.Invoke();
            if (_powershell.HadErrors)
                throw _powershell.Streams.Error[0].Exception;
            else
            {
                foreach (PSObject ps in psObjects)
                {
                    Models.MessageTrackingLog newLog = new Models.MessageTrackingLog();
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
            List<Models.MessageTrackingLog> totalReceivedMessages = new List<Models.MessageTrackingLog>();

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

            Collection<PSObject> psObjects = _powershell.Invoke();
            if (_powershell.HadErrors)
                throw _powershell.Streams.Error[0].Exception;
            else
            {
                foreach (PSObject ps in psObjects)
                {
                    Models.MessageTrackingLog newLog = new Models.MessageTrackingLog();
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
            }

            return totalReceivedMessages;
        }
    }
}
