/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
    /// Represents a security application resource handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SecurityApplicationResourceHandler : SecurityEntityResourceHandler<SecurityApplication>, ILockableResourceHandler
    {
        // Security repository
        private readonly ISecurityRepositoryService m_securityRepository;

        /// <summary>
        /// Wrapper type
        /// </summary>
        protected override Type WrapperType => typeof(SecurityApplicationInfo);

        /// <summary>
        /// Create security repository
        /// </summary>
        public SecurityApplicationResourceHandler(ISecurityRepositoryService securityRepository, IPolicyInformationService policyInformationService, ILocalizationService localizationService, IAuditService auditService, IDataCachingService cachingService = null, IRepositoryService<SecurityApplication> repository = null) : base(auditService, policyInformationService, localizationService, cachingService, repository)
        {
            this.m_securityRepository = securityRepository;
        }

        /// <summary>
        /// Create application
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityApplication sa)
            {
                data = new SecurityApplicationInfo(sa, this.m_policyInformationService);
            }

            var sde = data as SecurityApplicationInfo;
            // If no policies then assign the ones from SYNCHRONIZERS
            //if (sde.Policies?.Any() != true)
            //{
            //    var role = this.m_securityRepository.GetRole("APPLICATIONS");
            //    var policies = this.m_policyInformationService?.GetPolicies(role);
            //    if (policies != null)
            //    {
            //        sde.Policies = policies.Select(o => new SecurityPolicyInfo(o)).ToList();
            //    }
            //}
            //Policies now always copy from APPLICATIONS in AdoApplicationIdentityProvider

            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Update the application.
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public override object Update(object data)
        {
            if (data is SecurityApplication app)
            {
                data = new SecurityApplicationInfo(app, this.m_policyInformationService);
            }

            //Secret changes are handled by the downstream identity provider on save. This is different to how the user service is built up.

            return base.Update(data);
        }

        /// <summary>
        /// Obolete the application
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <summary>
        /// Lock the specified application
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public object Lock(object key)
        {
            this.m_securityRepository.LockApplication((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = true");
            return retVal;
        }

        /// <summary>
        /// Unlock the application.
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateApplication)]
        public object Unlock(object key)
        {
            this.m_securityRepository.UnlockApplication((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = false");
            return retVal;
        }
    }
}