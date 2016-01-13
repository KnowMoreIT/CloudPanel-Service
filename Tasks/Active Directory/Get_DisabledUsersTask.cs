using CPService.Database;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPService.Tasks.ActiveDirectory
{
    public class Get_DisabledUsersTask : IJob
    {
        private static readonly ILog logger = LogManager.GetLogger("Get_DisabledUsersTask");

        public void Execute(IJobExecutionContext context)
        {
            logger.InfoFormat("Processing disabled users");

            // Get the list of disabled users
            List<Users> users = ADActions.GetDisabledUsers();
            if (users != null)
            {
                logger.DebugFormat("Found {0} disabled user(s): {1}", users.Count, String.Join(", ", users.Select(x => x.UserPrincipalName).ToList()));
               
                try
                {
                    using (var db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                    {
                        var upns = users.Select(x => x.UserPrincipalName).ToList();
                        var disabledUsers = db.Users.Where(x => upns.Any(a => a == x.UserPrincipalName)).ToList();
                        disabledUsers.ForEach(x => x.IsEnabled = false);

                        var enabledUsers = db.Users.Where(x => !upns.Any(a => a == x.UserPrincipalName)).ToList();
                        enabledUsers.ForEach(x => x.IsEnabled = true);

                        db.SubmitChanges();
                        logger.InfoFormat("Found a total of {0} disabled users and {1} enabled users", disabledUsers.Count, enabledUsers.Count);
                    }
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat("Error processing disabled users: {0}", ex.ToString());
                }

                users = null;
            }
        }
    }
}
