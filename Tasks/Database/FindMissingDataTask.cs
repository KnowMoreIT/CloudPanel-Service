using CPService.Database;
using CPService.Tasks.Exchange;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPService.Tasks.Database
{
    [DisallowConcurrentExecution]
    public class FindMissingDataTask : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            if (Config.ServiceSettings.ExchangeVersion > 2010)
            {
                try
                {
                    using (CloudPanelDbContext db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                    {
                        // Find users with missing Exchange Guid that are Exchange enabled
                        IEnumerable<Users> users = db.Users.Where(x => x.MailboxPlan > 0).Where(x => x.ExchangeGuid == Guid.Empty);

                        if (users != null)
                        {
                            using (ExchActions exchTasks = new ExchActions())
                            {
                                foreach (Users user in users)
                                {
                                    try
                                    {
                                        Guid exchangeGuid = exchTasks.Get_ExchangeGuid(user.UserPrincipalName);
                                        user.ExchangeGuid = exchangeGuid;
                                    }
                                    catch (Exception ex)
                                    {
                                        CPService.LogError("Error retrieving Exchange GUID for " + user.UserPrincipalName + ": " + ex.ToString());
                                    }
                                }

                                db.SubmitChanges();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CPService.LogError("Error finding missing ExchangeGuid values: " + ex.ToString());
                }
            }
        }
    }
}
