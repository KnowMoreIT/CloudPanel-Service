using CPService.Database;
using Quartz;
using System;
using System.Collections.Generic;

namespace CPService.Tasks.Exchange
{
    [DisallowConcurrentExecution]
    public class Get_MailboxDatabaseSizesTask : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                using (CloudPanelDbContext db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                {
                    using (ExchActions powershell = new ExchActions())
                    {
                        List<StatMailboxDatabaseSizes> mailboxDatabases = powershell.Get_MailboxDatabaseSizes();
                        db.StatMailboxDatabaseSizes.InsertAllOnSubmit(mailboxDatabases);
                        db.SubmitChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                CPService.LogError("Failed to retrieve mailbox database sizes: " + ex.ToString());
            }
        }
    }
}
