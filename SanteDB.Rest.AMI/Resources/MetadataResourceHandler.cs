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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System.Collections.Generic;
using SanteDB.Core.Services;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler base class which has appropriate permissions for metadata
    /// </summary>
    public class MetadataResourceHandler<TMetadata> : ResourceHandlerBase<TMetadata>
        where TMetadata : IdentifiedData, new()
    {
        /// <summary>
        /// Initializes the metadata resource handler
        /// </summary>
        /// <param name="localizationService"></param>
        public MetadataResourceHandler(ILocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>
        /// Create a resource 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Get the resource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Query for metadata
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return base.Query(queryParameters, offset, count, out totalCount);
        }

        /// <summary>
        /// Obsolete metadata
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Update metadata
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="localizationService"></param>
        public MetadataResourceHandler(ILocalizationService localizationService) : base(localizationService)
        {
        }
    }
}
