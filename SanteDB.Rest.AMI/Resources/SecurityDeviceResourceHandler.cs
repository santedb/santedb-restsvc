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
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that handles security device operations
    /// </summary>
    public class SecurityDeviceResourceHandler : SecurityEntityResourceHandler<SecurityDevice>
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
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Update the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Update(object data)
        {
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
    }
}
