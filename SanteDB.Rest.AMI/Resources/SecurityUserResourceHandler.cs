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
using SanteDB.Core;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;

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
        public override object Create(object data, bool updateIfExists)
        {
            var td = data as SecurityUserInfo;

            // Insert the user
            var retVal = base.Create(data, updateIfExists) as SecurityUserInfo;

            // User information to roles
            if (td.Roles.Count > 0)
                ApplicationServiceContext.Current.GetService<IRoleProviderService>().AddUsersToRoles(new string[] { retVal.Entity.UserName }, td.Roles.ToArray(),  AuthenticationContext.Current.Principal);

            return new SecurityUserInfo(retVal.Entity)
            {
                Roles = td.Roles
            };
        }

        /// <summary>
        /// Lock the specified user
        /// </summary>
        public object Lock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().LockUser((Guid)key);
            return this.Get(key, Guid.Empty);
        }

        /// <summary>
        /// Unlock user
        /// </summary>
        public object Unlock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().UnlockUser((Guid)key);
            return this.Get(key, Guid.Empty);
        }

        /// <summary>
        /// Override the update function
        /// </summary>
        public override object Update(object data)
        {
            var td = data as SecurityUserInfo;

            // Update the user
            if (td.PasswordOnly)
            {
                ApplicationServiceContext.Current.GetService<IIdentityProviderService>().ChangePassword(td.Entity.UserName, td.Entity.Password, AuthenticationContext.Current.Principal);
                return null;
            }
            else
            {
                td.Entity.Password = null;
                var retVal = base.Update(data) as SecurityUserInfo;

                // Roles? We want to update
                if (td.Roles.Count > 0)
                {
                    var irps = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
                    // Remove the user from all roles
                    irps.RemoveUsersFromRoles(new string[] { retVal.Entity.UserName }, irps.GetAllRoles(), AuthenticationContext.Current.Principal);
                    irps.AddUsersToRoles(new string[] { retVal.Entity.UserName }, td.Roles.ToArray(),  AuthenticationContext.Current.Principal);
                }

                return new SecurityUserInfo(retVal.Entity)
                {
                    Roles = td.Roles
                };

            }

        }
    }
}
