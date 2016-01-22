using CPService.Database;
using CPService.Models;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPService.Tasks.Exchange
{
    [DisallowConcurrentExecution]
    public class Get_MessageTrackingLogsTask : IJob
    {
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
                    using (CloudPanelDbContext db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                    {
                        // Find a list of ALL mailbox users
                        IQueryable<Users> allMailboxes = db.Users.Where(x => x.MailboxPlan > 0);

                        // Look through each mailbox adding the message tracking information
                        foreach (Users user in allMailboxes)
                        {
                            try
                            {
                                IEnumerable<MessageTrackingLog> totalSentLogs = sentLogs.Where(a => a.Users.Contains(user.Email, StringComparer.OrdinalIgnoreCase));
                                IEnumerable<MessageTrackingLog> totalReceivedLogs = receivedLogs.Where(a => a.Users.Contains(user.Email, StringComparer.OrdinalIgnoreCase));

                                int totalSentLogsCount = totalSentLogs.Count();
                                int totalReceivedLogsCount = totalReceivedLogs.Count();

                                StatMessageTrackingCounts newCount = new StatMessageTrackingCounts();
                                newCount.UserID = user.ID;
                                newCount.Start = startTime;
                                newCount.End = endTime;
                                newCount.TotalSent = totalSentLogsCount;
                                newCount.TotalReceived = totalReceivedLogsCount;
                                newCount.TotalBytesSent = totalSentLogsCount > 0 ? totalSentLogs.Select(a => a.TotalBytes).Sum() : 0;
                                newCount.TotalBytesReceived = totalReceivedLogsCount > 0 ? totalReceivedLogs.Select(a => a.TotalBytes).Sum() : 0;

                                db.StatMessageTrackingCounts.InsertOnSubmit(newCount);
                                processedCount++;
                            }
                            catch (Exception ex)
                            {
                                CPService.LogError("Error getting total messages for " + user.Email + ": " + ex.ToString());
                                failedCount++;
                            }
                        }

                        // Save to database
                        db.SubmitChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                CPService.LogError("Failed to retrieve message logs: {0}: " + ex.ToString());
            }
        }
    }
}
