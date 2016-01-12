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
            CloudPanelDbContext db = null;
            ExchActions powershell = null;
            int processedCount = 0;

            try
            {
                db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString);

                powershell = new ExchActions();
                var mailboxDatabases = powershell.Get_MailboxDatabaseSizes();
                db.StatMailboxDatabaseSizes.InsertAllOnSubmit(mailboxDatabases);
                db.SubmitChanges();

                processedCount = mailboxDatabases.Count;
                mailboxDatabases = null;
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Failed to retrieve mailbox database sizes: {0}", ex.ToString());
            }
            finally
            {
                if (powershell != null)
                    powershell.Dispose();

                if (db != null)
                    db.Dispose();

                logger.InfoFormat("Processed a total of {0} mailbox databases", processedCount);
            }
        }
    }
}
