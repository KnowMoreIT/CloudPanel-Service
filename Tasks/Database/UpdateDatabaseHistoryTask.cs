using CPService.Database;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPService.Tasks.Database
{
    [DisallowConcurrentExecution]
    public class UpdateDatabaseHistoryTask : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                using (CloudPanelDbContext db = new CloudPanelDbContext(Config.ServiceSettings.SqlConnectionString))
                {
                    // Get a list of ALL companies
                    IEnumerable<Companies> companies = db.Companies.Where(x => x.IsReseller != true);

                    // Set our date and time when we started this task
                    DateTime now = DateTime.Now;

                    // Go through all companies getting the latest values
                    foreach (Companies company in companies)
                    {
                        IEnumerable<Users> users = db.Users.Where(x => x.CompanyCode == company.CompanyCode);
                        IEnumerable<int> userIds = users.Select(x => x.ID);

                        int citrixUsers = db.CitrixUserToDesktopGroup.Where(x => userIds.Contains(x.UserRefDesktopGroupId))
                                                                     .Select(x => x.UserRefDesktopGroupId)
                                                                     .Distinct()
                                                                     .Count();

                        Statistics newStatistic = new Statistics();
                        newStatistic.UserCount = users.Count();
                        newStatistic.MailboxCount = users.Where(a => a.MailboxPlan > 0).Count();
                        newStatistic.CitrixCount = citrixUsers;
                        newStatistic.ResellerCode = company.ResellerCode;
                        newStatistic.CompanyCode = company.CompanyCode;
                        newStatistic.Retrieved = now;

                        db.Statistics.InsertOnSubmit(newStatistic);
                    }

                    // Save changes to the database
                    db.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                CPService.LogError("Error getting history statistics: " + ex.ToString());
            }
        }
    }
}
