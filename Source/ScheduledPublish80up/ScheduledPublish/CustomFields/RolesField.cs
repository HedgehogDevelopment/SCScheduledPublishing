using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Sitecore.Text;

namespace ScheduledPublish.CustomFields
{
    public class RolesField : IRolesField
    {
        private static readonly string DomainParameterName = Settings.GetSetting("RolesField.DomainParameterName");

        private ListString _SelectedRoles;
        private ListString SelectedRoles
        {
            get
            {
                if (_SelectedRoles == null)
                {
                    _SelectedRoles = new ListString(Value);
                }

                return _SelectedRoles;
            }
        }

        private IEnumerable<Role> _RolesInDomain;
        private IEnumerable<Role> RolesInDomain
        {
            get
            {
                if (_RolesInDomain == null)
                {
                    _RolesInDomain = GetRolesInDomain();
                }

                return _RolesInDomain;
            }
        }

        private IEnumerable<Role> _Roles;
        private IEnumerable<Role> Roles
        {
            get
            {
                if (_Roles == null)
                {
                    _Roles = GetRoles();
                }

                return _Roles;
            }
        }

        private string _Domain;
        private string Domain
        {
            get
            {
                if (string.IsNullOrEmpty(_Domain))
                {
                    _Domain = FieldSettings[DomainParameterName];
                }

                return _Domain;
            }
        }

        private UrlString _FieldSettings;
        private UrlString FieldSettings
        {
            get
            {
                if (_FieldSettings == null)
                {
                    _FieldSettings = GetFieldSettings();
                }

                return _FieldSettings;
            }
        }

        private string Source { get; set; }
        private string Value { get; set; }

        private RolesField(string source, string value)
        {
            SetSource(source);
            SetValue(value);
        }

        private void SetSource(string source)
        {
            Source = source;
        }

        private void SetValue(string value)
        {
            Value = value;
        }

        private IEnumerable<Role> GetRolesInDomain()
        {
            if (!string.IsNullOrEmpty(Domain))
            {
                return Roles.Where(x => IsRoleInDomain(x, Domain));
            }

            return Roles;
        }

        private static IEnumerable<Role> GetRoles()
        {
            IEnumerable<Role> roles = RolesInRolesManager.GetAllRoles();

            if (roles != null)
            {
                return roles;
            }

            return new List<Role>();
        }

        private static bool IsRoleInDomain(Role role, string domain)
        {
            Assert.ArgumentNotNull(role, "role");
            Assert.ArgumentNotNullOrEmpty(domain, "domain");

            string roleNameLowerCase = role.Name.ToLower();
            string domainLowerCase = domain.ToLower();

            return roleNameLowerCase.StartsWith(domainLowerCase);
        }

        private UrlString GetFieldSettings()
        {
            try
            {
                if (!string.IsNullOrEmpty(Source))
                {
                    return new UrlString(Source);
                }
            }
            catch (Exception ex)
            {
                Log.Error(this.ToString(), ex, this);
            }

            return new UrlString();
        }

        public IEnumerable<Role> GetSelectedRoles()
        {
            IList<Role> selectedRoles = new List<Role>();

            foreach (string providerRoleKey in SelectedRoles)
            {
                Role selectedRole = RolesInDomain.FirstOrDefault(x => GetProviderRoleKey(x) == providerRoleKey);
                if (selectedRole != null)
                {
                    selectedRoles.Add(selectedRole);
                }
            }

            return selectedRoles;
        }

        public IEnumerable<Role> GetUnselectedRoles()
        {
            IList<Role> unselectedRoles = new List<Role>();

            foreach (Role role in RolesInDomain)
            {
                if (!IsRoleSelected(role))
                {
                    unselectedRoles.Add(role);
                }
            }

            return unselectedRoles;
        }

        private bool IsRoleSelected(Role role)
        {
            string providerRoleKey = GetProviderRoleKey(role);
            return IsRoleSelected(providerRoleKey);
        }

        private bool IsRoleSelected(string providerRoleKey)
        {
            return SelectedRoles.IndexOf(providerRoleKey) > -1;
        }

        public string GetProviderRoleKey(Role role)
        {
            Assert.ArgumentNotNull(role, "role");
            return role.Name;
        }

        public static IRolesField CreateNewRolesField(string source, string value)
        {
            return new RolesField(source, value);
        }
    }
}