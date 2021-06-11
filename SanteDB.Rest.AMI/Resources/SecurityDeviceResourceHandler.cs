/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
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
    /// Represents a resource handler that handles security device operations
    /// </summary>
    public class SecurityDeviceResourceHandler : SecurityEntityResourceHandler<SecurityDevice>, ILockableResourceHandler
    {
        /// <summary>
        /// Type of security device
        /// </summary>
        public override Type Type => typeof(SecurityDeviceInfo);


        /// <summary>
        /// Create device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Create(object data, bool updateIfExists)
        {

            if (data is SecurityDevice)
                data = new SecurityDeviceInfo(data as SecurityDevice);

            var sde = data as SecurityDeviceInfo;
            // If no policies then assign the ones from DEVICE
            if (sde.Policies == null || sde.Policies.Count == 0 && sde.Entity?.Policies == null || sde.Entity.Policies.Count == 0)
            {
                var role = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>()?.GetRole("DEVICE");
                var policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>()?.GetPolicies(role);
                if (policies != null)
                    sde.Policies = policies.Select(o => new SecurityPolicyInfo(o)).ToList();
            }

            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Update the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Update(object data)
        {
            if (data is SecurityDevice)
                data = new SecurityDeviceInfo(data as SecurityDevice);
            return base.Update(data);
        }

        /// <summary>
        /// Obolete the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Lock the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public object Lock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().LockDevice((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = true");
            return retVal;
        }

        /// <summary>
        /// Unlock user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public object Unlock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().UnlockDevice((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = false");
            return retVal;
        }

    }
}
