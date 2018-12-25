/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-11-19
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
    /// A resource handler for a concept
    /// </summary>
    public class ConceptResourceHandler : ResourceHandlerBase<Concept>
	{
		/// <summary>
		/// Create the specified object in the database
		/// </summary>
		[Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
		public override Object Create(Object data, bool updateIfExists)
		{
            return base.Create(data, updateIfExists);
		}

        /// <summary>
        /// Get the specified instance
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override Object Get(object id, object versionId)
		{
            return base.Get(id, versionId);
		}

		/// <summary>
		/// Obsolete the specified concept
		/// </summary>
		[Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
		public override Object Obsolete(object  key)
		{
            return base.Obsolete(key);
		}

        /// <summary>
        /// Query the specified data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
		{
            int tr = 0;
			return this.Query(queryParameters, 0, 100, out tr);
		}

        /// <summary>
        /// Query with offsets
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out Int32 totalCount)
		{
            return base.Query(queryParameters, offset, count, out totalCount);
		}

		/// <summary>
		/// Update the specified data
		/// </summary>
		[Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
		public override Object Update(Object  data)
		{
            return base.Update(data);
		}
	}
}