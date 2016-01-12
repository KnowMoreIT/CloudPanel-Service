using CPService.Database;
using CPService.Tasks.Exchange;
using log4net;
using Quartz;
using System;
using System.Linq;

namespace CPService.Tasks.Database
{
    public class FindMissingDataTask : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger("FindMissingDataTask");

        public void Execute(IJobExecutionContext context)
        {
            if (Config.ServiceSettings.ExchangeVersion > 2010)
            {
                CloudPanelDbContext db = null;
                ExchActions exchTasks = null;
                int processedCount = 0, failedCount = 0;

                try
                {
                    db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString);

                    // Find users with missing Exchange Guid that are Exchange enabled
                    var users = db.Users.Where(x => x.MailboxPlan > 0)
                                        .Where(x => x.ExchangeGuid == Guid.Empty)
                                        .ToList();

                    if (users != null)
                    {
                        exchTasks = new ExchActions();
                        users.ForEach(x =>
                        {
                            try
                            {
                                logger.DebugFormat("Retrieving ExchangeGuid for {0}", x.UserPrincipalName);
                                var exchangeGuid = exchTasks.Get_ExchangeGuid(x.UserPrincipalName);
                                x.ExchangeGuid = exchangeGuid;

                                processedCount += 1;
                            }
                            catch (Exception ex)
                            {
                                logger.ErrorFormat("Error retrieving Exchange GUID for {0}: {1}", x.UserPrincipalName, ex.ToString());
                                failedCount += 1;
                            }
                        });

                        db.SubmitChanges();
                        users = null;
                    }
                    else
                        logger.InfoFormat("No users are missing the ExchangeGuid value!");
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat("Error finding missing ExchangeGuid values: {0}", ex.ToString());
                }
                finally
                {
                    if (exchTasks != null)
                        exchTasks.Dispose();

                    if (db != null)
                        db.Dispose();

                    logger.InfoFormat("Processed a total of {0} users with {1} that have failed", processedCount, failedCount);
                }
            }
        }
    }
}
