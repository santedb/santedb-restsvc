/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */
using SanteDB.Core;
using SanteDB.Core.Security;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// A resource handler which handles security roles
    /// </summary> 
    public class SecurityRoleResourceHandler : SecurityEntityResourceHandler<SecurityRole>
    {


        // Security repository
        private IRoleProviderService m_roleProvider;

        /// <summary>
        /// Create security repository
        /// </summary>
        public SecurityRoleResourceHandler(IRoleProviderService roleProvider, IPolicyInformationService policyInformationService, IRepositoryServiceFactory repositoryFactory, IDataCachingService cachingService = null, IRepositoryService<SecurityRole> repository = null) : base(policyInformationService, repositoryFactory, cachingService, repository)
        {
            this.m_roleProvider = roleProvider;
        }

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
        /// Remove an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterRoles)]
        public object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            var scope = this.GetRepository().Get(Guid.Parse(scopingEntityKey.ToString()));
            if (scope == null)
                throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingEntityKey}");

            switch(propertyName)
            {
                case "policy":

                    
                case "user":
                    var user = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>().Get(Guid.Parse(subItemKey.ToString()));
                    if (user == null)
                        throw new KeyNotFoundException($"User {subItemKey} not found");
                    try
                    {
                        this.m_roleProvider.RemoveUsersFromRoles(new string[] { user.UserName }, new string[] { scope.Name }, AuthenticationContext.Current.Principal);
                        this.FireSecurityAttributesChanged(scope, true, $"del user={user.UserName}");
                        return user;
                    }
                    catch
                    {
                        this.FireSecurityAttributesChanged(scope, false, $"del user={subItemKey}");
                        throw;
                    }
                default:
                    throw new ArgumentException($"Property with {propertyName} not valid");
            }
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
            var td = data as SecurityRoleInfo;

            var retVal = base.Update(data) as SecurityRoleInfo;
            
            return new SecurityRoleInfo(td.Entity);
        }
    }
}
