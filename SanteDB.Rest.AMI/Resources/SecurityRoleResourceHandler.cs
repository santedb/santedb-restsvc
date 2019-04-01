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
using SanteDB.Rest.Common.Attributes;
using System;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// A resource handler which handles security roles
    /// </summary> 
    public class SecurityRoleResourceHandler : SecurityEntityResourceHandler<SecurityRole>
    {

        /// <summary>
        /// Get the type
        /// </summary>
        public override Type Type => typeof(SecurityRoleInfo);

        /// <summary>
        /// Create the specified security role
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateRoles)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityRole)
                data = new SecurityRoleInfo(data as SecurityRole);


            var retVal = base.Create(data, updateIfExists) as SecurityRoleInfo;
            var td = data as SecurityRoleInfo;
            
            if(td.Users.Count > 0)
            {
                ApplicationServiceContext.Current.GetService<IRoleProviderService>().AddUsersToRoles(td.Users.ToArray(), new string[] { td.Entity.Name } , AuthenticationContext.Current.Principal);
            }
            return new SecurityRoleInfo(retVal.Entity);
        }

        /// <summary>
        /// Obsolete roles
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterRoles)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Update roles
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.AlterRoles)]
        public override object Update(object data)
        {
            if (data is SecurityRole)
                data = new SecurityRoleInfo(data as SecurityRole);

            return base.Update(data);
        }
    }
}
