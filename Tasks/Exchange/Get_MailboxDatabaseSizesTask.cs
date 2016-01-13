using CPService.Database;
using log4net;
using Quartz;
using System;

namespace CPService.Tasks.Exchange
{
    public class Get_MailboxDatabaseSizesTask : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger("Get_MailboxDatabaseSizesTask");

        public void Execute(IJobExecutionContext context)
        {
            int processedCount = 0;

            try
            {
                using (var db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                {

                    using (var powershell = new ExchActions())
                    {
                        var mailboxDatabases = powershell.Get_MailboxDatabaseSizes();
                        db.StatMailboxDatabaseSizes.InsertAllOnSubmit(mailboxDatabases);
                        db.SubmitChanges();

                        processedCount = mailboxDatabases.Count;
                        mailboxDatabases = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Failed to retrieve mailbox database sizes: {0}", ex.ToString());
            }

            logger.InfoFormat("Processed a total of {0} mailbox databases", processedCount);
        }
    }
}
