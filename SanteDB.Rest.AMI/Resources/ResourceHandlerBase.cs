/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: Justin Fyfe
 * Date: 2019-8-8
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using System;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to HDSI
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    public class ResourceHandlerBase<TData> : SanteDB.Rest.Common.ResourceHandlerBase<TData> where TData : IdentifiedData
    {

        /// <summary>
        /// Gets the scope
        /// </summary>
        override public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the resource capabilities for the object
        /// </summary>
        public override ResourceCapabilityType Capabilities
        {
            get
            {
                return ResourceCapabilityType.Create | ResourceCapabilityType.Update | ResourceCapabilityType.Get | ResourceCapabilityType.Search;
            }
        }

    }
}
