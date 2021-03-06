﻿/*
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
    /// Represents a security application resource handler
    /// </summary>
    public class SecurityApplicationResourceHandler : SecurityEntityResourceHandler<SecurityApplication>, ILockableResourceHandler
    {

        /// <summary>
        /// Get the type of results
        /// </summary>
        public override Type Type => typeof(SecurityApplicationInfo);

        /// <summary>
        /// Create device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityApplication)
                data = new SecurityApplicationInfo(data as SecurityApplication);

            var sde = data as SecurityApplicationInfo;
            // If no policies then assign the ones from SYNCHRONIZERS
            if (sde.Policies == null || sde.Policies.Count == 0 && sde.Entity?.Policies == null || sde.Entity.Policies.Count == 0)

            {
                var role = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>()?.GetRole("SYNCHRONIZERS");
                var policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>()?.GetPolicies(role);
                if (policies != null)
                    sde.Policies = policies.Select(o => new SecurityPolicyInfo(o)).ToList();
            }

            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Update the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public override object Update(object data)
        {
            if (data is SecurityApplication)
                data = new SecurityApplicationInfo(data as SecurityApplication);
            return base.Update(data);
        }

        /// <summary>
        /// Obolete the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Lock the specified application
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public object Lock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().LockApplication((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = true");
            return retVal;
        }

        /// <summary>
        /// Unlock user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public object Unlock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().UnlockApplication((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = false");
            return retVal;
        }

    }
}
