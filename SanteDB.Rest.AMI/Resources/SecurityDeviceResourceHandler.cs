﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that handles security device operations
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SecurityDeviceResourceHandler : SecurityEntityResourceHandler<SecurityDevice>, ILockableResourceHandler
    {
        // Security repository
        private readonly ISecurityRepositoryService m_securityRepository;

        /// <summary>
        /// Wrapper type
        /// </summary>
        protected override Type WrapperType => typeof(SecurityDeviceInfo);

        /// <summary>
        /// Create security repository
        /// </summary>
        public SecurityDeviceResourceHandler(ISecurityRepositoryService securityRepository, IPolicyInformationService policyInformationService, ILocalizationService localizationService, IAuditService auditService, IDataCachingService cachingService = null, IRepositoryService<SecurityDevice> repository = null) : base(auditService, policyInformationService, localizationService, cachingService, repository)
        {
            this.m_securityRepository = securityRepository;
        }

        /// <summary>
        /// Create device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityDevice)
            {
                data = new SecurityDeviceInfo(data as SecurityDevice, this.m_policyInformationService);
            }

            var sde = data as SecurityDeviceInfo;
            // If no policies then assign the ones from DEVICE
            //if (sde.Policies?.Any() != true)
            //{
            //    var role = this.m_securityRepository?.GetRole("DEVICE");
            //    var policies = this.m_policyInformationService?.GetPolicies(role);
            //    if (policies != null)
            //    {
            //        sde.Policies = policies.Select(o => new SecurityPolicyInfo(o)).ToList();
            //    }
            //}
            //Policies now always copy from DEVICE automatically by the AdoDeviceIdentityProvider.

            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Update the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Update(object data)
        {
            if (data is SecurityDevice)
            {
                data = new SecurityDeviceInfo(data as SecurityDevice, this.m_policyInformationService);
            }

            return base.Update(data);
        }

        /// <summary>
        /// Obolete the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <summary>
        /// Lock the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public object Lock(object key)
        {
            this.m_securityRepository.LockDevice((Guid)key);
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
            this.m_securityRepository.UnlockDevice((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = false");
            return retVal;
        }
    }
}