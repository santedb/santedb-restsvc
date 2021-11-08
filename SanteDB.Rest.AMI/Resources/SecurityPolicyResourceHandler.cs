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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Security;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a policy resource handler
    /// </summary>
    public class SecurityPolicyResourceHandler : ResourceHandlerBase<SecurityPolicy>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="localizationService"></param>
        public SecurityPolicyResourceHandler(ILocalizationService localizationService) : base(localizationService)
        {

        }
        /// <summary>
        /// Get the capabilities of this resource
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Update | ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.GetVersion | ResourceCapabilityType.Delete;

        /// <summary>
        /// Create the policy
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterPolicy)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Update the policy
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterPolicy)]
        public override object Update(object data)
        {
            var key = (data as IIdentifiedEntity).Key.Value;
            var policy = this.Get(key, Guid.Empty);
            if (policy == null || (policy as SecurityPolicy).IsPublic)
                return base.Update(data);
            else if (policy == null)
            {
                this.m_tracer.TraceError($"Policy {key} not found");
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.rest.ami.policyNotFound", new
                {
                    param = key
                }));

            }
            else
            {
                this.m_tracer.TraceError($"Policy {(policy as SecurityPolicy).Oid} is a system policy and cannot be edited");
                throw new SecurityException(this.m_localizationService.FormatString("error.rest.ami.editSystemPolicy", new 
                { 
                    param = (policy as SecurityPolicy).Oid
                }));
            }
        }



        /// <summary>
        /// Obsolete the policy
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override object Obsolete(object key)
        {
            var policy = this.Get(key, Guid.Empty);
            if (policy == null || (policy as SecurityPolicy).IsPublic)
                return base.Obsolete(key);
            else if (policy == null)
            {
                this.m_tracer.TraceError($"Policy {key} not found");
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.rest.ami.policyNotFound", new
                {
                    param = key
                }));
            }
            else
            {
                this.m_tracer.TraceError($"Policy {(policy as SecurityPolicy).Oid} is a system policy and cannot be disabled");
                throw new SecurityException(this.m_localizationService.FormatString("error.rest.ami.disableSystemPolicy", new
                {
                    param = (policy as SecurityPolicy).Oid
                }));
            }
        }
    }
}
