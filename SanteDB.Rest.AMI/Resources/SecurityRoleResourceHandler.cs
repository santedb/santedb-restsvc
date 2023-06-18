/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-5-19
 */
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// A resource handler which handles security roles
    /// </summary> 
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SecurityRoleResourceHandler : SecurityEntityResourceHandler<SecurityRole>
    {
        // Security repository
        private IRoleProviderService m_roleProvider;

        /// <summary>
        /// Wrapper type
        /// </summary>
        protected override Type WrapperType => typeof(SecurityRoleInfo);

        /// <summary>
        /// Create security repository
        /// </summary>
        public SecurityRoleResourceHandler(IRoleProviderService roleProvider, IPolicyInformationService policyInformationService, ILocalizationService localizationService, IAuditService auditService, IDataCachingService cachingService = null, IRepositoryService<SecurityRole> repository = null) : base(auditService, policyInformationService, localizationService, cachingService, repository)
        {
            this.m_roleProvider = roleProvider;
        }

        /// <summary>
        /// Create the specified security role
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateRoles)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityRole)
            {
                data = new SecurityRoleInfo(data as SecurityRole, this.m_policyInformationService);
            }

            var retVal = base.Create(data, updateIfExists) as SecurityRoleInfo;
            var td = data as SecurityRoleInfo;

            return new SecurityRoleInfo(retVal.Entity, this.m_policyInformationService);
        }

        /// <summary>
        /// Obsolete roles
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterRoles)]
        public override object Delete(object key)
        {
            return base.Delete(key);
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
            {
                data = new SecurityRoleInfo(data as SecurityRole, this.m_policyInformationService);
            }

            var td = data as SecurityRoleInfo;

            var retVal = base.Update(data) as SecurityRoleInfo;

            return new SecurityRoleInfo(td.Entity, this.m_policyInformationService);
        }
    }
}