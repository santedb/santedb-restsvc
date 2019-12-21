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
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a user entity resource handler.
    /// </summary>
    public class UserEntityResourceHandler : ResourceHandlerBase<UserEntity>
	{

        /// <summary>
        /// Create the specified user entity
        /// </summary>
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets the specified user 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override Object Get(object id, object versionId)
        {
            return base.Get((Guid)id, (Guid)versionId);
        }

        /// <summary>
        /// Obsolete
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override Object Obsolete(object key)
        {
            return base.Obsolete((Guid)key);
        }

        /// <summary>
        /// Query the specified data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Query specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return base.Query(queryParameters, offset, count, out totalCount);
        }

        /// <summary>
        /// Update specified data
        /// </summary>
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}