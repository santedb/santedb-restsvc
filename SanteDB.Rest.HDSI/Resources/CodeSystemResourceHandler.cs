/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-11-27
 */
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler for code systems
    /// </summary>
    public class CodeSystemResourceHandler : ResourceHandlerBase<CodeSystem>
    {
        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }
    }
}
