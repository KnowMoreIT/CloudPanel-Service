using CPService.Database;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPService.Tasks.Exchange
{
    [DisallowConcurrentExecution]
    public class Get_MailboxSizesTask : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                using (CloudPanelDbContext db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                {
                    // Get a list of all users with mailboxes
                    IEnumerable<Users> mailboxes = db.Users.Where(x => x.MailboxPlan > 0);
                    IEnumerable<Users> archives = mailboxes.Where(x => x.ArchivePlan > 0);
                    if (mailboxes != null)
                    {
                        using (ExchActions powershell = new ExchActions())
                        {
                            // Process mailbox sizes
                            foreach (Users user in mailboxes)
                            {
                                try
                                {
                                    StatMailboxSizes size = powershell.Get_MailboxSize(user.UserGuid, false);
                                    size.UserGuid = user.UserGuid;
                                    size.UserPrincipalName = user.UserPrincipalName;

                                    db.StatMailboxSizes.InsertOnSubmit(size);
                                }
                                catch (Exception ex)
                                {
                                    CPService.LogError("Error getting mailbox size for " + user.UserPrincipalName + ": " + ex.ToString());
                                }
                            }

                            // Process archive sizes
                            foreach (Users user in archives)
                            {
                                try
                                {
                                    StatMailboxSizes size = powershell.Get_MailboxSize(user.UserGuid, true);
                                    size.UserGuid = user.UserGuid;
                                    size.UserPrincipalName = user.UserPrincipalName;

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
                                }
                                catch (Exception ex)
                                {
                                    CPService.LogError("Error getting archive mailbox size for " + user.UserPrincipalName + ": " + ex.ToString());
                                }
                            }

                            // Save the database changes now
                            db.SubmitChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CPService.LogError("Failed to retrieve mailbox and archive sizes: " + ex.ToString());
            }
        }
    }
}
