/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
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
 * DatERROR: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// A resource handler which handles security roles
    /// </summary> 
    public class SecurityRoleResourceHandler : SecurityEntityResourceHandler<SecurityRole>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="localizationService"></param>
        public SecurityRoleResourceHandler(ILocalizationService localizationService) : base(localizationService)
        {

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
        public override object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            var scope = this.GetRepository().Get(Guid.Parse(scopingEntityKey.ToString()));
            if (scope == null)
            {
               this.m_tracer.TraceError($"Could not find SecurityRole with identifier {scopingEntityKey}");
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.rest.ami.securityRoleNotFound", new
                {
                    param = scopingEntityKey
                }));
            }

            switch (propertyName)
            {
                case "policy":


                case "user":
                    var user = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>().Get(Guid.Parse(subItemKey.ToString()));
                    if (user == null)
                    {
                        this.m_tracer.TraceError($"User {subItemKey} not found");
                        throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.userMessage"));
                    }
                    try
                    {
                        ApplicationServiceContext.Current.GetService<IRoleProviderService>().RemoveUsersFromRoles(new string[] { user.UserName }, new string[] { scope.Name }, AuthenticationContext.Current.Principal);
                        this.FireSecurityAttributesChanged(scope, true, $"del user={user.UserName}");
                        return user;
                    }
                    catch
                    {
                        this.FireSecurityAttributesChanged(scope, false, $"del user={subItemKey}");
                        throw;
                    }
                default:
                    {
                       this.m_tracer.TraceError($"Property with {propertyName} not valid");
                        throw new ArgumentException(this.m_localizationService.FormatString("error.rest.ami.invalidProperty", new
                        {
                            param = propertyName
                        }));
                    }
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
