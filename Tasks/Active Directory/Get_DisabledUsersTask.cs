using CPService.Database;
using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace CPService.Tasks.ActiveDirectory
{
    [DisallowConcurrentExecution]
    public class Get_DisabledUsersTask : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            // Get the list of disabled users
            List<Users> users = ADActions.GetDisabledUsers();
            if (users != null)
            {               
                try
                {
                    using (SqlConnection sql = new SqlConnection(Config.ServiceSettings.SqlConnectionString))
                    {
                        sql.Open();

                        using (SqlCommand cmd = new SqlCommand("UPDATE Users SET IsEnabled=@IsEnabled WHERE UserPrincipalName=@UserPrincipalName", sql))
                        {
                            users.ForEach(x =>
                            {
                                if (!string.IsNullOrEmpty(x.UserPrincipalName))
                                {
                                    cmd.Parameters.Clear();
                                    cmd.Parameters.AddWithValue("IsEnabled", x.IsEnabled ?? true);
                                    cmd.Parameters.AddWithValue("UserPrincipalName", x.UserPrincipalName);

                                    cmd.ExecuteNonQuery();
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    CPService.LogError("Error processing disabled users: " + ex.ToString());
                }
            }
        }
    }
}
