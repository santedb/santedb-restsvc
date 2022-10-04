/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 * User: fyfej
 * Date: 2022-5-30
 */
using RestSrvr.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that can handle security users
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SecurityUserResourceHandler : SecurityEntityResourceHandler<SecurityUser>, ILockableResourceHandler
    {
        // Security repository
        private ISecurityRepositoryService m_securityRepository;

        // Role provider
        private IRoleProviderService m_roleProvider;

        // Identity provider
        private IIdentityProviderService m_identityProvider;

        /// <summary>
        /// Create security repository
        /// </summary>
        public SecurityUserResourceHandler(IIdentityProviderService identityProvider, ILocalizationService localizationService, IRoleProviderService roleProvider, ISecurityRepositoryService securityRepository, IPolicyInformationService policyInformationService, IAuditService auditService, IDataCachingService cachingService = null, IRepositoryService<SecurityUser> repository = null) : base(auditService, policyInformationService, localizationService, cachingService, repository)
        {
            this.m_securityRepository = securityRepository;
            this.m_roleProvider = roleProvider;
            this.m_identityProvider = identityProvider;
        }

        /// <summary>
        /// Gets the type of object that is expected
        /// </summary>
        public override Type Type => typeof(SecurityUserInfo);

        /// <summary>
        /// Creates the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateLocalIdentity)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityUser)
            {
                data = new SecurityUserInfo(data as SecurityUser);
            }

            var td = data as SecurityUserInfo;

            // Don't allow callers to overwrite expiration
            td.Entity.PasswordExpiration = null;
            // Insert the user
            var retVal = base.Create(data, updateIfExists) as SecurityUserInfo;

            // User information to roles
            if (td.Roles.Count > 0)
            {
                this.m_roleProvider.AddUsersToRoles(new string[] { retVal.Entity.UserName }, td.Roles.ToArray(), AuthenticationContext.Current.Principal);
            }

            return new SecurityUserInfo(retVal.Entity)
            {
                Roles = td.Roles
            };
        }

        /// <summary>
        /// Lock the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterLocalIdentity)]
        public object Lock(object key)
        {
            this.m_securityRepository.LockUser((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = true");
            return retVal;
        }

        /// <summary>
        /// Unlock user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterLocalIdentity)]
        public object Unlock(object key)
        {
            this.m_securityRepository.UnlockUser((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = false");
            return retVal;
        }

        /// <summary>
        /// Override the update function
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginPasswordOnly, true)]
        public override object Update(object data)
        {
            if (data is SecurityUser)
            {
                data = new SecurityUserInfo(data as SecurityUser);
            }

            var td = data as SecurityUserInfo;
            // Don't allow callers to overwrite expiration
            td.Entity.PasswordExpiration = null;

            // Update the user
            if (td.PasswordOnly)
            {
                // Validate that the user name matches the SID
                var user = this.GetRepository().Get(td.Entity.Key.Value);
                // Check upstream?
                if (user != null && user.UserName?.ToLowerInvariant() != td.Entity.UserName.ToLowerInvariant())
                {
                    this.m_tracer.TraceError($"Username mismatch expect {user.UserName.ToLowerInvariant()} but got {td.Entity.UserName.ToLowerInvariant()}", 403);
                    throw new FaultException(System.Net.HttpStatusCode.Forbidden, this.m_localizationService.GetString("error.rest.ami.mismatchUsername", new
                    {
                        param = user.UserName.ToLowerInvariant(),
                        param2 = td.Entity.UserName.ToLowerInvariant()
                    }));
                }

                this.m_identityProvider.ChangePassword(td.Entity.UserName, td.Entity.Password, AuthenticationContext.Current.Principal);

                if (user != null)
                {
                    this.FireSecurityAttributesChanged(user, true, "Password");
                }
                else
                {
                    this.FireSecurityAttributesChanged(td.Entity, true, "Password");
                }

                return null;
            }
            else
            {
                // We're doing a general update, so we have to demand access
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(PermissionPolicyIdentifiers.LoginAsService);
                td.Entity.Password = null;

                //td.Entity.Roles = td.Roles.Select(o => new SecurityRole() { Name = o }).ToList();
                var retVal = base.Update(data) as SecurityUserInfo;

                // Roles? We want to update
                if (td.Roles != null && td.Roles.Count > 0)
                {
                    this.m_roleProvider.RemoveUsersFromRoles(new String[] { td.Entity.UserName }, this.m_roleProvider.GetAllRoles().Where(o => !td.Roles.Contains(o)).ToArray(), AuthenticationContext.Current.Principal);
                    this.m_roleProvider.AddUsersToRoles(new string[] { td.Entity.UserName }, td.Roles.ToArray(), AuthenticationContext.Current.Principal);
                    this.FireSecurityAttributesChanged(retVal.Entity, true, $"Roles = {String.Join(",", td.Roles)}");
                }

                return new SecurityUserInfo(retVal.Entity)
                {
                    Roles = td.Roles
                };
            }
        }
    }
}