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
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a policy resource handler
    /// </summary>
    public class SecurityPolicyResourceHandler : ResourceHandlerBase<SecurityPolicy>
    {

        /// <summary>
        /// Get the capabilities of this resource
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Update | ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.GetVersion;

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
            return base.Update(data);
        }

        /// <summary>
        /// Obsolete the policy
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override object Obsolete(object key)
        {
            throw new NotSupportedException();
        }
    }
}
