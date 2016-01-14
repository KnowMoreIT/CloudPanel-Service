using CPService.Database;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPService.Tasks.ActiveDirectory
{
    [DisallowConcurrentExecution]
    public class Get_LockedUsersTask : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger("Get_LockedUsersTask");

        public void Execute(IJobExecutionContext context)
        {
            logger.InfoFormat("Processing locked users");

            // Get a list of all locked out users
            List<Users> users = ADActions.GetLockedUsers();
            if (users != null)
            {
                logger.DebugFormat("Found {0} locked user(s): {1}", users.Count, String.Join(", ", users.Select(x => x.UserPrincipalName).ToList()));
                try
                {
                    using (CloudPanelDbContext db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                    {
                        List<string> upns = users.Select(x => x.UserPrincipalName).ToList();
                        List<Users> lockedUsers = db.Users.Where(x => upns.Contains(x.UserPrincipalName)).ToList();
                        lockedUsers.ForEach(x => x.IsLockedOut = true);

                        List<Users> unlockedUsers = db.Users.Where(x => !upns.Contains(x.UserPrincipalName)).ToList();
                        unlockedUsers.ForEach(x => x.IsLockedOut = false);

                        db.SubmitChanges();
                        logger.InfoFormat("Found a total of {0} locked out users and {1} unlocked users", lockedUsers.Count, unlockedUsers.Count);
                    }
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat("Error processing locked users: {0}", ex.ToString());
                }

                users = null;
            }
        }
    }
}
