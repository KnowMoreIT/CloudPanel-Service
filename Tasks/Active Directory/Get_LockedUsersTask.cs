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
    public class Get_LockedUsersTask : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            // Get a list of all locked out users
            List<Users> users = ADActions.GetLockedUsers();
            if (users != null)
            {
                try
                {
                    using (SqlConnection sql = new SqlConnection(Config.ServiceSettings.SqlConnectionString))
                    {
                        sql.Open();

                        using (SqlCommand cmd = new SqlCommand("UPDATE Users SET IsLockedOut=@IsLockedOut WHERE UserPrincipalName=@UserPrincipalName", sql))
                        {
                            users.ForEach(x =>
                            {
                                if (!string.IsNullOrEmpty(x.UserPrincipalName))
                                {
                                    cmd.Parameters.Clear();
                                    cmd.Parameters.AddWithValue("IsLockedOut", x.IsLockedOut ?? false);
                                    cmd.Parameters.AddWithValue("UserPrincipalName", x.UserPrincipalName);

                                    cmd.ExecuteNonQuery();
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    CPService.LogError("Error processing locked users: " + ex.ToString());
                }
            }
        }
    }
}
