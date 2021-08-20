using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Security role user child handler
    /// </summary>
    public class SecurityRoleUserChildHandler : IApiChildResourceHandler
    {
        /// <summary>
        /// Parent types to which this child applies
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(SecurityRoleInfo) };

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string Name => "user";

        /// <summary>
        /// Property type
        /// </summary>
        public Type PropertyType => typeof(SecurityUser);

        /// <summary>
        /// Binding for this operation
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Get capabilities
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Delete | ResourceCapabilityType.Search;

        // Challenge service
        private IRepositoryService<SecurityUser> m_userRepository;

        // Security repository
        private ISecurityRepositoryService m_securityRepository;

        // Role provider
        private IRoleProviderService m_roleProvider;

        // Policy enforcement
        private IPolicyEnforcementService m_pep;

        // Repository service
        private IRepositoryService<SecurityRole> m_roleRepository;

        /// <summary>
        /// Security challenge child handler
        /// </summary>
        public SecurityRoleUserChildHandler(IRepositoryService<SecurityRole> roleRepository, 
            IRepositoryService<SecurityUser> userRepository, 
            ISecurityRepositoryService securityRepository, 
            IRoleProviderService roleProvider, 
            IPolicyEnforcementService pepService)
        {
            this.m_userRepository = userRepository;
            this.m_pep = pepService;
            this.m_roleRepository= roleRepository;
            this.m_securityRepository = securityRepository;
            this.m_roleProvider = roleProvider;
        }

        /// <summary>
        /// Add a new user to the role
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {

            var scope = this.m_roleRepository.Get((Guid)scopingKey);
            if (scope == null)
                throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingKey}");

            try
            {
                this.m_pep.Demand(PermissionPolicyIdentifiers.AlterRoles);

                // Get user entity
                if (item is SecurityUser su)
                    item = new SecurityUserInfo(su);

                var rd = item as SecurityUserInfo;
                if (!rd.Entity.Key.HasValue)
                    rd.Entity = this.m_securityRepository.GetUser(rd.Entity.UserName);
                if (rd.Entity == null)
                    throw new KeyNotFoundException($"Could not find specified user");

                this.m_roleProvider.AddUsersToRoles(new string[] { rd.Entity.UserName }, new string[] { scope.Name }, AuthenticationContext.Current.Principal);
                AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, true, $"add user={rd.Entity.UserName}");
                return rd.Entity;
            }
            catch
            {
                AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, false);
                throw;
            }
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query the specified sub-object
        /// </summary>
        public IEnumerable<object> Query(Type scopingType, object scopingKey, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            filter.Add("roles.id", scopingKey.ToString());
            var expr = QueryExpressionParser.BuildLinqExpression<SecurityUser>(filter);
            // Could redirect but faster just to query and return
            return this.m_userRepository.Find(expr, offset, count, out totalCount);
        }

        /// <summary>
        /// Remove user from role
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            var scope = this.m_roleRepository.Get((Guid)scopingKey);
            if (scope == null)
                throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingKey}");

            var user = this.m_userRepository.Get(Guid.Parse(key.ToString()));
            if (user == null)
                throw new KeyNotFoundException($"User {key} not found");

            try
            {
                this.m_pep.Demand(PermissionPolicyIdentifiers.AlterRoles);
                this.m_roleProvider.RemoveUsersFromRoles(new string[] { user.UserName }, new string[] { scope.Name }, AuthenticationContext.Current.Principal);
                AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, true, $"del user={user.UserName}");
                return user;
            }
            catch
            {
                AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, false, $"del user={key}");
                throw;
            }
        }
    }
}
