/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: justi
 * Date: 2019-1-12
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
    public class SecurityUserResourceHandler : SecurityEntityResourceHandler<SecurityUser>, ILockableResourceHandler
    {

        /// <summary>
        /// Gets the type of object that is expected
        /// </summary>
        public override Type Type => typeof(SecurityUserInfo);


        /// <summary>
        /// Creates the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateIdentity)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityUser)
                data = new SecurityUserInfo(data as SecurityUser);
            var td = data as SecurityUserInfo;

            // Insert the user
            var retVal = base.Create(data, updateIfExists) as SecurityUserInfo;

            // User information to roles
            if (td.Roles.Count > 0)
                ApplicationServiceContext.Current.GetService<IRoleProviderService>().AddUsersToRoles(new string[] { retVal.Entity.UserName }, td.Roles.ToArray(), AuthenticationContext.Current.Principal);

            return new SecurityUserInfo(retVal.Entity)
            {
                Roles = td.Roles
            };
        }

        /// <summary>
        /// Lock the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterIdentity)]
        public object Lock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().LockUser((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = true");
            return retVal;
        }

        /// <summary>
        /// Unlock user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterIdentity)]
        public object Unlock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().UnlockUser((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = false");
            return retVal;
        }

        /// <summary>
        /// Override the update function
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterIdentity)]
        public override object Update(object data)
        {
            if (data is SecurityUser)
                data = new SecurityUserInfo(data as SecurityUser);

            var td = data as SecurityUserInfo;

            // Update the user
            if (td.PasswordOnly)
            {
                // Validate that the user name matches the SID
                var user = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>().Get(td.Entity.Key.Value);
                if (user.UserName?.ToLowerInvariant() != td.Entity.UserName.ToLowerInvariant())
                    throw new FaultException(403, $"Username mismatch expect {user.UserName.ToLowerInvariant()} but got {td.Entity.UserName.ToLowerInvariant()}");

                ApplicationServiceContext.Current.GetService<IIdentityProviderService>().ChangePassword(td.Entity.UserName, td.Entity.Password, AuthenticationContext.Current.Principal);
                this.FireSecurityAttributesChanged(user, true, "Password");
                return null;
            }
            else
            {
                td.Entity.Password = null;

                //td.Entity.Roles = td.Roles.Select(o => new SecurityRole() { Name = o }).ToList();
                var retVal = base.Update(data) as SecurityUserInfo;

                // Roles? We want to update
                if (td.Roles.Count > 0)
                {
                    var irps= ApplicationServiceContext.Current.GetService<IRoleProviderService>();
                    irps.RemoveUsersFromRoles(new String[] { td.Entity.UserName }, irps.GetAllRoles().Where(o => !td.Roles.Contains(o)).ToArray(), AuthenticationContext.Current.Principal);
                    irps.AddUsersToRoles(new string[] { td.Entity.UserName }, td.Roles.ToArray(), AuthenticationContext.Current.Principal);
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
