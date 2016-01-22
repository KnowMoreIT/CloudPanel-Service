using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;

namespace CPService.Tasks.ActiveDirectory
{
    public class ADActions
    {
        /// <summary>
        /// Gets a list of disabled users in Active Directory
        /// </summary>
        /// <returns></returns>
        public static List<Users> GetDisabledUsers()
        {
            List<Users> disabledUsers = new List<Users>();

            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, Config.ServiceSettings.PrimaryDC, Config.ServiceSettings.Username, Config.ServiceSettings.Password))
            {
                using (UserPrincipal up = new UserPrincipal(pc))
                {
                    up.Enabled = false;

                    using (PrincipalSearcher ps = new PrincipalSearcher(up))
                    {
                        PrincipalSearchResult<Principal> results = ps.FindAll();
                        foreach (Principal r in results)
                        {
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
                    }
                }
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
            List<Users> enabledUsers = new List<Users>(6000);

            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, Config.ServiceSettings.PrimaryDC, Config.ServiceSettings.Username, Config.ServiceSettings.Password))
            {
                using (UserPrincipal up = new UserPrincipal(pc))
                {
                    up.Enabled = false;

                    using (PrincipalSearcher ps = new PrincipalSearcher(up))
                    {
                        PrincipalSearchResult<Principal> results = ps.FindAll();
                        foreach (Principal r in results)
                        {
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
                    }
                }
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

            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, Config.ServiceSettings.PrimaryDC, Config.ServiceSettings.Username, Config.ServiceSettings.Password))
            {
                using (UserPrincipal up = new UserPrincipal(pc))
                {
                    using (PrincipalSearcher ps = new PrincipalSearcher(up))
                    {
                        PrincipalSearchResult<Principal> results = ps.FindAll();
                        foreach (UserPrincipal r in results)
                        {
                            if (r.IsAccountLockedOut())
                            {
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
                    }
                }
            }

            return lockedUsers;
        }
    }
}
