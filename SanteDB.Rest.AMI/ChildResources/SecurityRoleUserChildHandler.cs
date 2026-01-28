/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

#pragma warning disable CS0612

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Security role user child handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SecurityRoleUserChildHandler : IApiChildResourceHandler
    {
        /// <summary>
        /// Parent types to which this child applies
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(SecurityRole) };

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

        readonly IAuditService _AuditService;

        /// <summary>
        /// Security challenge child handler
        /// </summary>
        public SecurityRoleUserChildHandler(IRepositoryService<SecurityRole> roleRepository,
            IRepositoryService<SecurityUser> userRepository,
            ISecurityRepositoryService securityRepository,
            IRoleProviderService roleProvider,
            IPolicyEnforcementService pepService,
            IAuditService auditService)
        {
            this.m_userRepository = userRepository;
            this.m_pep = pepService;
            this.m_roleRepository = roleRepository;
            this.m_securityRepository = securityRepository;
            this.m_roleProvider = roleProvider;
            _AuditService = auditService;
        }

        /// <summary>
        /// Add a new user to the role
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            var scope = this.m_roleRepository.Get((Guid)scopingKey);
            if (scope == null)
            {
                throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingKey}");
            }

            try
            {
                this.m_pep.Demand(PermissionPolicyIdentifiers.AlterRoles);
                this.m_pep.Demand(PermissionPolicyIdentifiers.AlterIdentity);

                var userNames = new List<string>();
                // Get user entity
                switch (item)
                {
                    case SecurityUser su:
                        userNames.Add(su.UserName);
                        break;
                    case SecurityUserInfo sui:
                        userNames.Add(sui.Entity.UserName);
                        break;
                    case AmiCollection amic:
                        userNames.AddRange(amic.CollectionItem.OfType<SecurityUser>().Select(r => r.UserName).Union(amic.CollectionItem.OfType<SecurityUserInfo>().Select(r => r.Entity.UserName)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(item), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(SecurityUser), item.GetType()));
                }

                this.m_roleProvider.AddUsersToRoles(userNames.ToArray(), new string[] { scope.Name }, AuthenticationContext.Current.Principal);
                _AuditService.Audit().ForSecurityAttributeAction(new object[] { scope }, true, $"add user={String.Join(",", userNames)}").Send();
                return scope;
            }
            catch
            {
                _AuditService.Audit().ForSecurityAttributeAction(new object[] { scope }, false).Send();
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
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            filter.Add("roles.id", scopingKey.ToString());
            var expr = QueryExpressionParser.BuildLinqExpression<SecurityUser>(filter);
            // Could redirect but faster just to query and return
            return this.m_userRepository.Find(expr);
        }

        /// <summary>
        /// Remove user from role
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            var scope = this.m_roleRepository.Get((Guid)scopingKey);
            if (scope == null)
            {
                throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingKey}");
            }


            try
            {
                this.m_pep.Demand(PermissionPolicyIdentifiers.AlterRoles);


                if (key is Guid keyGuid)
                {
                    var user = this.m_userRepository.Get(keyGuid);
                    if (user == null)
                    {
                        throw new KeyNotFoundException($"User {key} not found");
                    }
                    key = user.UserName;
                }

                this.m_roleProvider.RemoveUsersFromRoles(new string[] { key.ToString() }, new string[] { scope.Name }, AuthenticationContext.Current.Principal);
                _AuditService.Audit().ForSecurityAttributeAction(new object[] { scope }, true, $"del user={key}").Send();
                return scope;
            }
            catch
            {
                _AuditService.Audit().ForSecurityAttributeAction(new object[] { scope }, false, $"del user={key}").Send();
                throw;
            }
        }
    }
}
#pragma warning restore