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
 * User: Justin Fyfe
 * Date: 2019-8-8
 */
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler for concept reference terms.
    /// </summary>
    public class ReferenceTermResourceHandler : ResourceHandlerBase<ReferenceTerm>
	{
        /// <summary>
        /// Gets the scope
        /// </summary>
        public override Type Scope => typeof(IHdsiServiceContract);
        
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return base.Query(queryParameters, offset, count, out totalCount);
        }

        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Update(object data)
        {
            return base.Update(data);
        }
    }
}
