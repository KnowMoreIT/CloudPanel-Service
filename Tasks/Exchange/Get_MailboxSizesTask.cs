using CPService.Database;
using log4net;
using Quartz;
using System;
using System.Linq;

namespace CPService.Tasks.Exchange
{
    public class Get_MailboxSizesTask : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger("Get_MailboxSizesTask");

        public void Execute(IJobExecutionContext context)
        {
            int processedCount = 0, failedCount = 0;

            try
            {
                using (var db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                {
                    // Get a list of all users with mailboxes
                    var mailboxes = db.Users.Where(x => x.MailboxPlan > 0).ToList();
                    if (mailboxes != null)
                    {
                        using (var powershell = new ExchActions())
                        {

                            mailboxes.ForEach(x =>
                            {
                                try
                                {
                                    logger.DebugFormat("Processing mailbox {0} for size report", x.UserPrincipalName);

                                    StatMailboxSizes size = powershell.Get_MailboxSize(x.UserGuid, false);
                                    size.UserGuid = x.UserGuid;
                                    size.UserPrincipalName = x.UserPrincipalName;

                                    db.StatMailboxSizes.InsertOnSubmit(size);

                                    processedCount += 1;
                                }
                                catch (Exception ex)
                                {
                                    logger.ErrorFormat("Error getting mailbox size for {0}: {1}", x.UserPrincipalName, ex.ToString());
                                    failedCount += 1;
                                }
                            });
                            db.SubmitChanges();
                            mailboxes = null;

                            // Get archive mailbox sizes now
                            var archiveMailboxes = mailboxes.Where(x => x.ArchivePlan > 0).ToList();
                            archiveMailboxes.ForEach(x =>
                            {
                                try
                                {
                                    logger.DebugFormat("Processing archive mailbox {0} for size report", x.UserPrincipalName);

                                    StatMailboxSizes size = powershell.Get_MailboxSize(x.UserGuid, true);
                                    size.UserGuid = x.UserGuid;
                                    size.UserPrincipalName = x.UserPrincipalName;

                                    db.StatMailboxArchiveSizes.InsertOnSubmit(new StatMailboxArchiveSizes()
                                    {
                                        UserGuid = size.UserGuid,
                                        UserPrincipalName = size.UserPrincipalName,
                                        MailboxDatabase = size.MailboxDatabase,
                                        TotalItemSize = size.TotalItemSize,
                                        TotalItemSizeInBytes = size.TotalItemSizeInBytes,
                                        TotalDeletedItemSize = size.TotalDeletedItemSize,
                                        TotalDeletedItemSizeInBytes = size.TotalDeletedItemSizeInBytes,
                                        ItemCount = size.ItemCount,
                                        DeletedItemCount = size.DeletedItemCount,
                                        Retrieved = size.Retrieved
                                    });

                                    processedCount += 1;
                                }
                                catch (Exception ex)
                                {
                                    logger.ErrorFormat("Error getting archive mailbox size for {0}: {1}", x.UserPrincipalName, ex.ToString());
                                    failedCount += 1;
                                }
                            });
                            db.SubmitChanges();
                            archiveMailboxes = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Failed to retrieve mailbox and archive sizes: {0}", ex.ToString());
            }

            logger.InfoFormat("Processed a total of {0} mailbox and archive sizes with {1} failed", processedCount, failedCount);
        }
    }
}
