using System.Collections.Generic;
using Sitecore.Security.Accounts;

namespace ScheduledPublish.CustomFields
{
    public interface IRolesField
    {
        IEnumerable<Role> GetSelectedRoles();

        IEnumerable<Role> GetUnselectedRoles();

        string GetProviderRoleKey(Role role);
    }
}