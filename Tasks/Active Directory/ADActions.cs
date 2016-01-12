using log4net;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;

namespace CPService.Tasks.ActiveDirectory
{
    public class ADActions
    {
        private static readonly ILog logger = LogManager.GetLogger("ADActions");

        /// <summary>
        /// Gets a list of disabled users in Active Directory
        /// </summary>
        /// <returns></returns>
        public static List<Users> GetDisabledUsers()
        {
            List<Users> disabledUsers = new List<Users>();

            PrincipalContext pc = null;
            PrincipalSearcher ps = null;
            UserPrincipal up = null;
            try
            {
                pc = new PrincipalContext(ContextType.Domain, Config.ServiceSettings.PrimaryDC, Config.ServiceSettings.Username, Config.ServiceSettings.Password);
                up = new UserPrincipal(pc);
                up.Enabled = false;

                ps = new PrincipalSearcher(up);
                var results = ps.FindAll();
                foreach (var r in results)
                {
                    logger.DebugFormat("Found disabled user {0}", r.UserPrincipalName);
                    disabledUsers.Add(new Users()
                    {
                        UserGuid = (Guid)r.Guid,
                        DisplayName = r.DisplayName,
                        UserPrincipalName = r.UserPrincipalName,
                        SamAccountName = r.SamAccountName,
                        DistinguishedName = r.DistinguishedName,
                        IsEnabled = false
                    });
                }
                results = null;
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Error getting a list of disabled users: {0}", ex.ToString());
            }
            finally
            {
                if (ps != null)
                    ps.Dispose();

                if (up != null)
                    up.Dispose();

                if (pc != null)
                    pc.Dispose();
            }

            return disabledUsers;
        }

        /// <summary>
        /// Gets a list of enabled users in Active Directory
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static List<Users> GetEnabledUsers()
        {
            List<Users> enabledUsers = new List<Users>();

            PrincipalContext pc = null;
            PrincipalSearcher ps = null;
            UserPrincipal up = null;
            try
            {
                pc = new PrincipalContext(ContextType.Domain, Config.ServiceSettings.PrimaryDC, Config.ServiceSettings.Username, Config.ServiceSettings.Password);
                up = new UserPrincipal(pc);
                up.Enabled = true;

                ps = new PrincipalSearcher(up);
                var results = ps.FindAll();
                foreach (var r in results)
                {
                    logger.DebugFormat("Found enabled user {0}", r.UserPrincipalName);
                    enabledUsers.Add(new Users()
                    {
                        UserGuid = (Guid)r.Guid,
                        DisplayName = r.DisplayName,
                        UserPrincipalName = r.UserPrincipalName,
                        SamAccountName = r.SamAccountName,
                        DistinguishedName = r.DistinguishedName,
                        IsEnabled = false
                    });
                }
                results = null;
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Error getting a list of enabled users in Active Directory: {0}", ex.ToString());
            }
            finally
            {
                if (ps != null)
                    ps.Dispose();

                if (up != null)
                    up.Dispose();

                if (pc != null)
                    pc.Dispose();
            }

            return enabledUsers;
        }

        /// <summary>
        /// Gets a list of locked out users in Active directory
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static List<Users> GetLockedUsers()
        {
            List<Users> lockedUsers = new List<Users>();

            PrincipalContext pc = null;
            PrincipalSearcher ps = null;
            UserPrincipal up = null;
            try
            {
                pc = new PrincipalContext(ContextType.Domain, Config.ServiceSettings.PrimaryDC, Config.ServiceSettings.Username, Config.ServiceSettings.Password);
                up = new UserPrincipal(pc);

                ps = new PrincipalSearcher(up);
                var results = ps.FindAll();
                foreach (UserPrincipal r in results)
                {
                    if (r.IsAccountLockedOut())
                    {
                        logger.DebugFormat("Found locked out user {0}", r.UserPrincipalName);
                        lockedUsers.Add(new Users()
                        {
                            UserGuid = (Guid)r.Guid,
                            DisplayName = r.DisplayName,
                            UserPrincipalName = r.UserPrincipalName,
                            SamAccountName = r.SamAccountName,
                            DistinguishedName = r.DistinguishedName,
                            IsEnabled = false
                        });
                    }
                }
                results = null;
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("Error getting a list of locked out users in Active Directory: {0}", ex.ToString());
            }
            finally
            {
                if (ps != null)
                    ps.Dispose();

                if (up != null)
                    up.Dispose();

                if (pc != null)
                    pc.Dispose();
            }

            return lockedUsers;
        }
    }
}
