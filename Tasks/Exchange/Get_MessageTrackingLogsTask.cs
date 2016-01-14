using CPService.Database;
using CPService.Models;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPService.Tasks.Exchange
{
    [DisallowConcurrentExecution]
    public class Get_MessageTrackingLogsTask : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger("Get_MessageTrackingLogsTask");

        public void Execute(IJobExecutionContext context)
        {
            int processedCount = 0, failedCount = 0;
            try
            {
                using (ExchActions powershell = new ExchActions())
                {

                    // Our timestamps to look for
                    DateTime startTime = DateTime.Now.AddHours(-24);
                    DateTime endTime = DateTime.Now;

                    // Get the sent and received logs for the past 24 hours from Exchange
                    List<MessageTrackingLog> sentLogs = powershell.Get_TotalSentMessages(startTime, endTime);
                    List<MessageTrackingLog> receivedLogs = powershell.Get_TotalReceivedMessages(startTime, endTime);

                    // Initialize our database
                    using (var db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                    {

                        // Find a list of ALL mailbox users
                        var allMailboxes = db.Users.Where(x => x.MailboxPlan > 0)
                                                   .Select(x => new
                                                   {
                                                       EmailAddress = x.Email,
                                                       ID = x.ID
                                                   }).ToList();

                        // Look through each mailbox adding the message tracking information
                        allMailboxes.ForEach(x =>
                        {
                            try
                            {
                                List<MessageTrackingLog> totalSentLogs = sentLogs.Where(a => a.Users.Contains(x.EmailAddress, StringComparer.OrdinalIgnoreCase)).ToList();
                                List<MessageTrackingLog> totalReceivedLogs = receivedLogs.Where(a => a.Users.Contains(x.EmailAddress, StringComparer.OrdinalIgnoreCase)).ToList();

                                StatMessageTrackingCounts newCount = new StatMessageTrackingCounts();
                                newCount.UserID = x.ID;
                                newCount.Start = startTime;
                                newCount.End = endTime;
                                newCount.TotalSent = totalSentLogs.Count;
                                newCount.TotalReceived = totalReceivedLogs.Count;
                                newCount.TotalBytesSent = totalSentLogs.Count > 0 ? totalSentLogs.Select(a => a.TotalBytes).Sum() : 0;
                                newCount.TotalBytesReceived = totalReceivedLogs.Count > 0 ? totalReceivedLogs.Select(a => a.TotalBytes).Sum() : 0;

                                db.StatMessageTrackingCounts.InsertOnSubmit(newCount);

                                processedCount++;
                            }
                            catch (Exception ex)
                            {
                                logger.ErrorFormat("Error getting total messages for {0}: {1}", x.EmailAddress, ex.ToString());
                                failedCount++;
                            }
                        });

                        // Save to database
                        db.SubmitChanges();
                        allMailboxes = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Failed to retrieve message logs: {0}", ex.ToString());
            }

            logger.InfoFormat("Processed a total of {0} mailboxes for message logs with {1} failed", processedCount, failedCount);
        }
    }
}
